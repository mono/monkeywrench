

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;

using MonkeyWrench.WebServices;
using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using System.Collections.Specialized;

namespace MonkeyWrench.WebServices {
	public class EMailNotification : NotificationBase
	{
		DBEmailIdentity identity;
		string[] emails;
		ObjectCache cache = new MemoryCache ("EmailCache");

		public EMailNotification (DBNotification notification)
			: base (notification)
		{
			using (DB db = new DB ()) {
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = "SELECT * FROM EmailIdentity WHERE id = @id;";
					DB.CreateParameter (cmd, "id", notification.emailidentity_id.Value);
					using (IDataReader reader = cmd.ExecuteReader ()) {
						if (!reader.Read ())
							throw new ApplicationException (string.Format ("Could not find the email identity {0}", notification.emailidentity_id.Value));
						identity = new DBEmailIdentity (reader);
					}
				}
			}

			emails = string.IsNullOrWhiteSpace (identity.email) ? new string[0] : identity.email.Split(',');
		}

		public override void Notify (DBWork work, DBRevisionWork revision_work)
		{
			if (!(work.State == DBState.Failed || work.State == DBState.Issues || work.State == DBState.Timeout))
				return;
			base.Notify (work, revision_work);
		}

		protected override void Notify (DBWork work, DBRevisionWork revision_work, List<DBPerson> people, string message)
		{
			if (!this.emails.Any ()) {
				Logger.Log ("EmailNotification.Notify no emails");
				return;
			}

			if (this.emails.Any (x => !x.EndsWith ("@hipchat.xamarin.com", StringComparison.OrdinalIgnoreCase))) {
				Logger.Log ("EmailNotification.Notify skipping non-HipChat emails because we don't know how to send email!");
				return;
			}

			var actionableEmails = this.emails.Where (x => x.EndsWith ("@hipchat.xamarin.com")).ToList ();

			if (!actionableEmails.Any ()) {
				Logger.Log ("EmailNotification.Notify no actionable emails!");
				return;
			}

			Logger.Log ("EmailNotification.Notify (lane_id: {1} revision_id: {2} host_id: {3} State: {0}) enabled: {4}, {5} people", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id, true, people.Count);

			bool nonfatal = false;

			if (!Evaluate (work, revision_work, out nonfatal)) {
				Logger.Log ("EmailNotification.Notify (lane_id: {1} revision_id: {2} host_id: {3} State: {0}) = evaluation returned false", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id);
				return;
			}

			if (nonfatal) {
				message = "Test failure";
			} else {
				message = "Build failure";
			}

			DBRevision revision;
			DBLane lane;
			DBHost host;

			using (DB db = new DB ()) {
				revision = DBRevision_Extensions.Create (db, revision_work.revision_id);
				lane = DBLane_Extensions.Create (db, revision_work.lane_id);
				host = DBHost_Extensions.Create (db, revision_work.host_id);
			}

			message = string.Format ("{0} in revision {1} on {2}/{3} ({4}/ViewLane.aspx?lane_id={5}&host_id={6}&revision_id={7})",
				message,
				(revision.revision.Length > 8 ? revision.revision.Substring (0, 8) : revision.revision),
				lane.lane, host.host, Configuration.GetWebSiteUrl (), lane.id, host.id, revision.id);

			foreach (var person in people) {
				if (string.IsNullOrEmpty (person.irc_nicknames)) {
					using (var db = new DB ()) {
						var computed_nicks = new List<string> ();
						var email_lists = new List<IEnumerable<string>> ();
						email_lists.Add (person.GetEmails (db));
						if (person.Emails != null)
							email_lists.Add (person.Emails);

						foreach (var emails in email_lists) {
							foreach (var email in emails) {
								var at = email.IndexOf ('@');
								if (at > 0) {
									computed_nicks.Add (email.Substring (0, at));
								}
							}
						}
						if (computed_nicks.Count == 0 && !string.IsNullOrEmpty (person.fullname))
							computed_nicks.Add (person.fullname);
						person.irc_nicknames = string.Join (",", computed_nicks.ToArray ());
					}
				}

				if (string.IsNullOrEmpty (person.irc_nicknames)) {
					Logger.Log ("HipChatNotification: could not find somebody to notify for revision with id {0} on lane {1}", revision_work.revision_id, revision_work.lane_id);
					continue;
				}
			}

			var apiToken = identity.password;
			var rooms = actionableEmails.Select (email => email.Split ('@') [0]).ToList ();
			const string finalApi = "https://hipchat.xamarin.com/v1/rooms/message";

			var webClient = new WebClient ();

			foreach (var r in rooms) {
				string prefix;
				string room = r;
				if (room.StartsWith ("*")) {
					room = room.Substring (1);
					prefix = "";
				} else {
					prefix = "@";
				}

				var prefixNames = string.Join (", ", people.SelectMany (x => x.irc_nicknames.Split (',')).Distinct ().Select (x => string.Format("{1}{0}", x, prefix)));
				var finalMessage = prefixNames + ": " + message;

				if (cache [room + "|" + finalMessage] != null)
					continue;
				cache.Add (room + "|" + finalMessage, string.Empty, new DateTimeOffset (DateTime.UtcNow.AddHours (1), TimeSpan.Zero));

				var postData = new NameValueCollection ();
				postData.Add ("auth_token", apiToken);
				postData.Add ("room_id", room);
				postData.Add ("from", "Wrench");
				postData.Add ("message", finalMessage);
				postData.Add ("notify", nonfatal ? "0" : "1");
				// TODO: Maybe send HTML eventually, though HTML doesn't allow @-mentions. :(
				postData.Add ("message_format", "text");
				postData.Add ("color", nonfatal ? "yellow" : "red");
				postData.Add ("format", "json");
				var res = webClient.UploadValues (finalApi, postData);
				var resString = Encoding.UTF8.GetString (res);

				Logger.Log ("HipChatNotification: response from server: {0}", resString);
			}
		}

		public override void Stop ()
		{
			/* Nothing to do */
		}
	}
}
