
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Services;

using MonkeyWrench.WebServices;
using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using System.Collections.Specialized;

namespace MonkeyWrench.WebServices {

	public class IrcNotification : NotificationBase
	{
		readonly DBIrcIdentity identity;
		const bool enabled = true;
		readonly ObjectCache cache = new MemoryCache ("IrcCache");

		static Thread message_thread;
		static Queue<IrcMessage> message_list = new Queue<IrcMessage> ();
		static AutoResetEvent message_event = new AutoResetEvent (false);
		const bool message_pumping = true;

		class IrcMessage {
			public string Server;
			public string Password;
			public bool UseSSL;
			public string Nick;
			public string[] Channels;
			public int Port;
			public IEnumerable<string> Messages;

			public override string ToString ()
			{
				return string.Format ("IrcMessage [ Server: {0} Nick: {1} Channels: {2} Messages: {3} ]", Server, Nick, string.Join (";", Channels), string.Join (";", Messages.ToArray ()));
			}
		}

		public IrcNotification (DBNotification notification)
			: base (notification)
		{
			/* Connect to server and join channels */

			using (DB db = new DB ()) {
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = "SELECT * FROM IrcIdentity WHERE id = @id;";
					DB.CreateParameter (cmd, "id", notification.ircidentity_id.Value);
					using (IDataReader reader = cmd.ExecuteReader ()) {
						if (!reader.Read ())
							throw new ApplicationException (string.Format ("Could not find the irc identity {0}", notification.ircidentity_id.Value));
						identity = new DBIrcIdentity (reader);
					}
				}
			}
		}

		public override void Stop ()
		{
		}

		public void SendMessages (IEnumerable<string> messages)
		{
			try {
				lock (message_list) {
					if (message_thread == null) {
						message_thread = new Thread (MessagePump);
						message_thread.IsBackground = true;
						message_thread.Start ();
					}
				}
				var Server = identity.servers;
				var Port = identity.use_ssl ? 6697 : 6667;

				var colon = Server.IndexOf (':');
				if (colon > 0) {
					Port = int.Parse (Server.Substring (colon + 1));
					Server = Server.Substring (0, colon);
				}

				var message = new IrcMessage
				{
					Server = Server,
					Password = identity.password,
					UseSSL = identity.use_ssl,
					Nick = identity.nicks,
					Channels = identity.channels.Split (','),
					Port = Port,
					Messages = messages,
				};
				lock (message_list) {
					message_list.Enqueue (message);
					Logger.Log ("IrcNotification.SendMessages: added new message. There are now {0} messages in the queue", message_list.Count);
				}
				message_event.Set ();
			} catch (Exception ex) {
				Logger.Log ("IrcNotification.SendMessages: failed to send message to server: {0} channels: {1} messages: {2}: {3}",
					identity.servers, identity.channels, string.Join (";", messages.ToArray ()), ex);
			}
		}

		public static void MessagePump (object dummy)
		{
			IrcMessage message;

			try {
				while (true) {
					message_event.WaitOne ();

					while (true) {
						lock (message_list) {
							Logger.Log ("IrcNotification.MessagePump: {0} messages in queue", message_list.Count);
							if (message_list.Count == 0)
								break;
							message = message_list.Dequeue ();
						}

						try {
							var watch = new Stopwatch ();
							watch.Start ();
							SendMessages (message.Server, message.Password, message.UseSSL, message.Nick, message.Channels, message.Port, message.Messages);
							watch.Stop ();
							Logger.Log ("IrcNotification.MessagePump: sent message in {0} ms ({1})", watch.ElapsedMilliseconds, message);
						} catch (Exception ex) {
							Logger.Log ("IrcNotification.MessagePump: failed to send message {1}: {0}", ex, message);
						}
					}
				}
			} catch (ThreadAbortException) {
				// App is shutting down. No need to spam the logs.
			} catch (Exception ex) {
				Logger.Log ("IrcNotification.MessagePump: Unexpected error. No more messages will be processed: {0}", ex);
			}
		}

		public static void SendMessages (string Server, string Password, bool UseSSL, string Nick, string[] Channels, int Port, IEnumerable<string> messages)
		{
			var client = new TcpClient();
			client.NoDelay = true;
			client.Connect(Server, Port);

			Stream stream = client.GetStream();
			if (UseSSL) {
				SslStream sslStream = new SslStream (stream, false, (a, b, c, d) => true);
				sslStream.AuthenticateAsClient(Server);
				stream = sslStream;
			}

			var reader = new StreamReader (stream);
			var writer = new StreamWriter (stream);

			writer.WriteLine ("PASS {0}", Password);
			writer.WriteLine ("NICK {0}", Nick);
			writer.WriteLine ("USER {0} 0 * :{1}", Nick, "MonkeyWrench");
			foreach (var Channel in Channels)
				foreach (var message in messages)
					writer.WriteLine ("PRIVMSG {0} :{1}", Channel, message);
			writer.WriteLine ("QUIT");
			writer.Flush ();

			reader.Dispose ();
			writer.Dispose ();
			stream.Dispose ();
			client.Close ();
		}

		void NotifySlack (DBWork work, DBRevisionWork revision_work, List<DBPerson> people, string message)
		{
			bool nonfatal = false;

			if (!Evaluate (work, revision_work, out nonfatal)) {
				Logger.Log ("SlackNotification: lane_id: {1} revision_id: {2} host_id: {3} State: {0}: evaluation returned false", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id);
				return;
			} else {
				Logger.Log ("SlackNotification: lane_id: {1} revision_id: {2} host_id: {3} State: {0} enabled: {4}, {5} people", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id, enabled, people.Count);
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
					Logger.Log ("SlackNotification: could not find somebody to notify for revision with id {0} on lane {1}", revision_work.revision_id, revision_work.lane_id);
					continue;
				}
			}

			var rooms = identity.channels.Split (new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			var finalApi = identity.servers;

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
				var json = string.Format (@"
	{{
		""channel"": ""{0}"",
		""username"": ""Wrench"",
		""text"": ""*{1}*"",
		""attachments"": [
			{{
				""fallback"": ""{2}{3}"",
				""color"": ""{4}"",
				""fields"":[
					{{
						""value"": ""{2}{3}"",
						""short"": false
					}}
				]
			}}
		]
	}}
	",
					r,
					nonfatal ? "Test failure" : "Build failure",
					nonfatal ? ":crying_cat_face: " : ":pouting_cat: ",
					finalMessage.Replace ("\\", "\\\\").Replace ("\"", "\\\""),
					nonfatal ? "warning" : "danger"
				);

				var postData = new NameValueCollection ();
				postData.Add ("payload", json);
				try {
					var res = webClient.UploadValues (finalApi, postData);
					var resString = Encoding.UTF8.GetString (res);
					Logger.Log ("SlackNotification: response from server: {0}", resString);
				} catch (WebException wex) {
					string responseText = null;

					if (wex.Response != null) {
						using (var responseStream = wex.Response.GetResponseStream ()) {
							if (responseStream != null) {
								using (var reader = new StreamReader (responseStream))
									responseText = reader.ReadToEnd ();
							}
						}
					}
					if (responseText == null) {
						Logger.Log ("SlackNotification: exception from server (no response): {0}", wex.Message);
					} else {
						Logger.Log ("SlackNotification: server error: {0} with exception: {1}", responseText, wex.Message);
					}
				}
			}
		}

		public override void Notify (DBWork work, DBRevisionWork revision_work)
		{
			if (!(work.State == DBState.Failed || work.State == DBState.Issues || work.State == DBState.Timeout))
				return;
			base.Notify (work, revision_work);
		}

		protected override void Notify (DBWork work, DBRevisionWork revision_work, List<DBPerson> people, string message)
		{
			if (identity.servers.Contains ("hooks.slack.com") || identity.servers.Contains ("hooks-slack-com")) {
				NotifySlack (work, revision_work, people, message);
				return;
			}

			Logger.Log ("IrcNotification.Notify (lane_id: {1} revision_id: {2} host_id: {3} State: {0}) enabled: {4}, {5} people", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id, enabled, people.Count);

			foreach (var person in people) {
				if (string.IsNullOrEmpty (person.irc_nicknames)) {
					using (DB db = new DB ()) {
						List<string> computed_nicks = new List<string> ();
						List<IEnumerable<string>> email_lists = new List<IEnumerable<string>> ();
						email_lists.Add (person.GetEmails (db));
						if (person.Emails != null)
							email_lists.Add (person.Emails);

						foreach (var emails in email_lists) {
							foreach (var email in emails) {
								int at = email.IndexOf ('@');
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
					Logger.Log ("IrcNotification: could not find somebody to notify for revision with id {0} on lane {1}", revision_work.revision_id, revision_work.lane_id);
					continue;
				}

				message = message.Replace ("{red}", "\u00034").Replace ("{bold}", "\u0002").Replace ("{default}", "\u000F");

				var messages = new List<string> ();

				foreach (var nick in person.irc_nicknames.Split (',')) {
					var msg = nick + ": " + message;
					if (cache [msg] != null)
						continue;
					cache.Add (msg, string.Empty, new DateTimeOffset (DateTime.UtcNow.AddHours (1), TimeSpan.Zero));
					messages.Add (msg);
				}

				if (messages.Count > 0)
					SendMessages (messages);
			}
		}
	}
}
