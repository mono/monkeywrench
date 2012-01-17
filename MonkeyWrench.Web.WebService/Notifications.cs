/*
 * WebServices.asmx.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2010 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Services;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;

using Meebey.SmartIrc4net;

namespace MonkeyWrench.Web.WebService
{
	public static class Notifications
	{
		static object lock_obj = new object ();
		static List<NotificationBase> notifications = new List<NotificationBase> ();
		static Dictionary<int, List<NotificationBase>> notifications_per_lane = new Dictionary<int, List<NotificationBase>> ();

		public static void Start ()
		{
			List<DBLaneNotification> lane_notifications = new List<DBLaneNotification> ();

			lock (lock_obj) {
				using (DB db = new DB ()) {
					using (IDbCommand cmd = db.CreateCommand ()) {
						/* Only select the notifications that are actually in use */
						cmd.CommandText = "SELECT Notification.* FROM Notification WHERE id IN (SELECT DISTINCT notification_id FROM LaneNotification);";
						cmd.CommandText += "SELECT * FROM LaneNotification;";
						using (IDataReader reader = cmd.ExecuteReader ()) {
							while (reader.Read ()) {
								DBNotification n = new DBNotification (reader);
								if (n.ircidentity_id.HasValue) {
									Logger.Log ("Starting irc notification");
									notifications.Add (new IrcNotification (n));
								} else if (n.emailidentity_id.HasValue) {
									Logger.Log ("Starting email notification");
									notifications.Add (new EMailNotification (n));
								} else {
									Logger.Log ("Starting unknown notification");
								}
							}
							if (reader.NextResult ()) {
								while (reader.Read ()) {
									lane_notifications.Add (new DBLaneNotification (reader));
								}
							}
						}
					}
				}

				foreach (DBLaneNotification ln in lane_notifications) {
					List<NotificationBase> ns;
					NotificationBase n;
					if (!notifications_per_lane.TryGetValue (ln.lane_id, out ns)) {
						ns = new List<NotificationBase> ();
						notifications_per_lane [ln.lane_id] = ns;
					}
					n = notifications.First ((v) => v.Notification.id == ln.notification_id);
					ns.Add (n);
					Logger.Log ("Notifications: enabled notification {0} '{1}' for lane {2}", n.Notification.id, n.Notification.name, ln.lane_id);
				}
			}
		}

		public static void Restart ()
		{
			Stop ();
			Start ();
		}

		public static void Stop ()
		{
			lock (lock_obj) {
				if (notifications != null) {
					foreach (var n in notifications) {
						n.Stop ();
					}
					notifications.Clear ();
				}
			}
		}

		public static void Notify (DBWork work, DBRevisionWork revision_work)
		{
			Logger.Log ("Notifications.Notify (lane_id: {1} revision_id: {2} host_id: {3} State: {0})", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id);
			if (notifications == null)
				return;

			if (!(work.State == DBState.Failed || work.State == DBState.Issues))
				return;

			ThreadPool.QueueUserWorkItem ((v) => ProcessNotify (work, revision_work));
		}

		private static void ProcessNotify (DBWork work, DBRevisionWork revision_work)
		{
			List<NotificationBase> notifications;

			Logger.Log ("Notifications.ProcessNotify (lane_id: {1} revision_id: {2} host_id: {3} State: {0})", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id);

			try {
				lock (lock_obj) {
					if (!notifications_per_lane.TryGetValue (revision_work.lane_id, out notifications)) {
						Logger.Log ("Notifications.ProcessNotify (lane_id: {1} revision_id: {2} host_id: {3} State: {0}): Lane doesn't have any notifications enabled", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id);
						return;
					}

					foreach (var notification in notifications) {
						notification.Notify (work, revision_work);
					}
				}
			} catch (Exception ex) {
				Logger.Log ("Exception while processing notification: {0}", ex.Message);
			}
		}
	}

	public abstract class NotificationBase
	{
		protected NotificationBase (DBNotification notification)
		{
			Notification = notification;
		}

		public DBNotification Notification { get; private set; }

		private bool Evaluate (DBWork work, DBRevisionWork revision_work, out bool nonfatal)
		{
			DBState newest_state = work.State;

			nonfatal = false;

			if (work.State == DBState.Success)
				return false;

			/* We need to see if there are any successfull builds later than this one */
			using (DB db = new DB ()) {
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"
SELECT state FROM RevisionWork
INNER JOIN Revision ON RevisionWork.revision_id = Revision.id
WHERE RevisionWork.state = 3 AND RevisionWork.lane_id = @lane_id AND RevisionWork.host_id = @host_id AND RevisionWork.completed AND Revision.date > (SELECT date FROM Revision WHERE id = @revision_id)
ORDER BY Revision.date DESC
LIMIT 1;

SELECT nonfatal FROM Command WHERE id = @command_id;
";
					DB.CreateParameter (cmd, "lane_id", revision_work.lane_id);
					DB.CreateParameter (cmd, "host_id", revision_work.host_id);
					DB.CreateParameter (cmd, "revision_id", revision_work.revision_id);
					DB.CreateParameter (cmd, "command_id", work.command_id);

					object obj_state = null;

					using (IDataReader reader = cmd.ExecuteReader ()) {
						if (reader.Read ())
							obj_state = reader [0];
						if (reader.NextResult () && reader.Read ())
							nonfatal = reader.GetBoolean (0);
					}

					if (obj_state != DBNull.Value && obj_state != null) {
						Logger.Log ("NotificationBase.Evaluate: Later work succeeded, nothing to notify");
						return false;
					}
				}
			}

			switch (Notification.Type) {
			case DBNotificationType.AllFailures:
				return work.State == DBState.Issues || work.State == DBState.Failed;
			case DBNotificationType.FatalFailuresOnly:
				if (nonfatal)
					return false;
				return work.State == DBState.Failed && newest_state == DBState.Failed;
			case DBNotificationType.NonFatalFailuresOnly:
				return work.State == DBState.Issues || nonfatal;
			default:
				return false;
			}
		}

		public abstract void Stop ();
		protected abstract void Notify (DBWork work, DBRevisionWork revision_work, List<DBPerson> people, string message);
		public void Notify (DBWork work, DBRevisionWork revision_work)
		{
			List<DBPerson> people = new List<DBPerson> ();
			DBRevision revision;
			DBLane lane;
			DBHost host;
			string message;
			bool nonfatal;

			Logger.Log ("NotificationBase.Notify (lane_id: {1} revision_id: {2} host_id: {3} State: {0})", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id);

			nonfatal = false;
	
			if (!Evaluate (work, revision_work, out nonfatal)) {
				Logger.Log ("NotificationBase.Notify (lane_id: {1} revision_id: {2} host_id: {3} State: {0}) = evaluation returned false", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id);
				return;
			}

			if (nonfatal) {
				message = "Test failure";
			} else {
				message = "{red}{bold}Build failure{default}";
			}

			using (DB db = new DB ()) {
				revision = DBRevision_Extensions.Create (db, revision_work.revision_id);
				lane = DBLane_Extensions.Create (db, revision_work.lane_id);
				host = DBHost_Extensions.Create (db, revision_work.host_id);
			}

			message = string.Format ("{0} in revision {1} on {2}/{3}: {4}/ViewLane.aspx?lane_id={5}&host_id={6}&revision_id={7}",
				message,
				(revision.revision.Length > 8 ? revision.revision.Substring (0, 8) : revision.revision),
				lane.lane, host.host, Configuration.GetWebSiteUrl (), lane.id, host.id, revision.id);

			MonkeyWrench.Scheduler.Scheduler.FindPeopleForCommit (lane, revision, people);
			people = FindPeople (people);

			Notify (work, revision_work, people, message);
		}

		private List<DBPerson> FindPeople (List<DBPerson> people)
		{
			List<DBPerson> result = new List<DBPerson> ();
			for (int i = 0; i < people.Count; i++) {
				FindPerson (people [i], result);
			}
			return result;
		}

		private void FindPerson (DBPerson person, List<DBPerson> people)
		{
			using (DB db = new DB ()) {
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = string.Empty;

					// find registered people with the same email
					if (person.Emails != null) {
						int email_counter = 0;
						foreach (string email in person.Emails) {
							if (string.IsNullOrEmpty (email))
								continue;
							email_counter++;
							cmd.CommandText += "SELECT Person.* FROM Person INNER JOIN UserEmail ON Person.id = UserEmail.person_id WHERE UserEmail.email ILIKE @email" + email_counter.ToString () + ";\n";
							DB.CreateParameter (cmd, "email" + email_counter.ToString (), email);
						}
					}

					// find registered people with the same fullname
					if (!string.IsNullOrEmpty (person.fullname)) {
						cmd.CommandText += "SELECT Person.* FROM Person WHERE fullname ILIKE @fullname;";
						DB.CreateParameter (cmd, "fullname", person.fullname);
					}

					using (IDataReader reader = cmd.ExecuteReader ()) {
						do {
							while (reader.Read ()) {
								DBPerson guy = new DBPerson (reader);
								if (people.Exists ((v) => v.id == guy.id))
									continue;
								people.Add (guy);
							}
						} while (reader.NextResult ());
					}
				}
			}

			if (people.Count == 0)
				people.Add (person);
		}
	}

	public class IrcNotification : NotificationBase
	{
		IrcState joined_state;
		DBIrcIdentity identity;
		string [] channels;
		bool enabled = true;

		class IrcState
		{
			public IrcNotification Notification;
			public IrcClient Client;
			public Thread Thread;
			public ManualResetEvent Connected = new ManualResetEvent (false);

			public IrcState (IrcNotification notification)
			{
				this.Notification = notification;
			}

			public bool WaitForConnected (TimeSpan timeout)
			{
				return Connected.WaitOne (timeout);
			}

			public bool WaitForEmptySendBuffer (TimeSpan timeout)
			{
				DateTime start = DateTime.Now;

				while (!Client.IsSendBufferEmpty) {
					Thread.Sleep (10);
					if (start.Add (timeout) < DateTime.Now)
						return false;
				}

				return true;
			}

			public void Start ()
			{
				Client = new IrcClient ();
				Thread = new Thread (Loop);
				Thread.Start ();
			}

			private void Loop (object obj)
			{
				try {
					IrcClient irc = Client;

					string [] servers;
					string [] nicks;
					int port = 6667;

					Logger.Log ("Connecting to irc: {0} joining {1} as {2} using ssl: {3}", Notification.identity.servers, Notification.identity.channels, Notification.identity.nicks, Notification.identity.use_ssl);

					servers = Notification.identity.servers.Split (',', ' ');
					Notification.channels = Notification.identity.channels.Split (',', ' ');
					nicks = Notification.identity.nicks.Split (',', ' ');

					for (int i = 0; i < servers.Length; i++) {
						int colon = servers [i].IndexOf (':');
						if (colon > 0) {
							int.TryParse (servers [i].Substring (colon + 1), out port);
							servers [i] = servers [i].Substring (0, colon);
							break;
						}
					}

					//irc.AutoRetry = true;
					irc.SendDelay = 200;
					irc.UseSsl = Notification.identity.use_ssl;

					irc.OnAutoConnectError += new AutoConnectErrorEventHandler (irc_OnAutoConnectError);
					irc.OnQueryMessage += new IrcEventHandler (irc_OnQueryMessage);
					irc.OnConnected += new EventHandler (irc_OnConnected);
					irc.OnDisconnected += new EventHandler (irc_OnDisconnected);
					irc.OnRawMessage += new IrcEventHandler (irc_OnRawMessage);
					Logger.Log ("Connecting to servers: {0} : {1}", string.Join (";", servers), port);
					irc.Connect (servers, port);
					irc.Login (nicks, "MonkeyWrench", 0, "MonkeyWrench", Notification.identity.password);

					if (Notification.identity.join_channels)
						irc.RfcJoin (Notification.channels);

					Logger.Log ("Connected to irc: {0} joined {1} as {2}", Notification.identity.servers, Notification.identity.channels, Notification.identity.nicks);
					Connected.Set ();
					irc.Listen ();
				} catch (Exception ex) {
					Logger.Log ("Exception while connecting to irc: {0}", ex);
				}

			}

			void irc_OnRawMessage (object sender, IrcEventArgs e)
			{
				IrcClient irc = Client;

				switch (e.Data.Type) {
				case ReceiveType.ChannelMessage:
					if (e.Data.Message.StartsWith (irc.Nickname)) {
						string cmd = e.Data.Message.Substring (irc.Nickname.Length).TrimStart (':', ' ', ',');
						switch (cmd.ToLowerInvariant ()) {
						case "enable":
							Notification.enabled = true;
							break;
						case "disable":
							Notification.enabled = false;
							break;
						case "state":
							irc.SendMessage (SendType.Message, e.Data.Channel, e.Data.Nick + ": " + (Notification.enabled ? "enabled" : "disabled"));
							break;
						case "help":
						case "h":
						case "?":
						case "/?":
						case "-?":
							irc.SendMessage (SendType.Message, e.Data.Channel, e.Data.Nick + ": enable|disable: enable or disable irc notifications temporarily.");
							break;
						default:
							irc.SendMessage (SendType.Message, e.Data.Channel, e.Data.Nick + ": Don't know how to '" + cmd + "'");
							break;
						}
					}
					break;
				}
				Console.WriteLine ("OnRawMessage");
			}

			void irc_OnQueryMessage (object sender, IrcEventArgs e)
			{
				Console.WriteLine ("OnQueryMessage");
			}

			void irc_OnAutoConnectError (object sender, AutoConnectErrorEventArgs e)
			{
				Console.WriteLine ("irc_OnAutoConnectError");
			}

			void irc_OnDisconnected (object sender, EventArgs e)
			{
				Console.WriteLine ("irc_OnDisconnected");
			}

			void irc_OnConnected (object sender, EventArgs e)
			{
				Console.WriteLine ("irc_OnConnected");
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

			if (identity.join_channels)
				joined_state = Connect ();
		}

		public override void Stop ()
		{
			Disconnect ();
		}

		private IrcState Connect ()
		{
			IrcState state = new IrcState (this);
			state.Start ();
			return state;
		}

		private void Disconnect ()
		{
			if (identity.join_channels) {
				Disconnect (joined_state);
				joined_state = null;
			}
		}

		private void Disconnect (IrcState state)
		{
			try {
				state.Client.Disconnect ();
			} catch (Exception ex) {
				Logger.Log ("Exception while disconnecting from irc: {0}", ex.Message);
			}
		}

		protected override void Notify (DBWork work, DBRevisionWork revision_work, List<DBPerson> people, string message)
		{
			IrcState state;

			Logger.Log ("IrcNotification.Notify (lane_id: {1} revision_id: {2} host_id: {3} State: {0}) enabled: {4}", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id, enabled);

			if (!enabled)
				return;

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

				if (!identity.join_channels) {
					state = Connect ();
					if (!state.WaitForConnected (TimeSpan.FromSeconds (30))) {
						Logger.Log ("IrcNotification: could not connect to server in 30 seconds.");
						continue;
					}
				} else {
					state = joined_state;
				}
				foreach (var nick in person.irc_nicknames.Split (',')) {
					foreach (var channel in channels) {
						state.Client.SendMessage (SendType.Message, channel, nick + ": " + message);
					}
				}
				if (!identity.join_channels) {
					if (!state.WaitForEmptySendBuffer (TimeSpan.FromSeconds (30)))
						Logger.Log ("IrcNotification: waited for 30 seconds for messages to be sent, disconnecting now");
					Disconnect (state);
				}
			}
		}
	}

	public class EMailNotification : NotificationBase
	{
		public EMailNotification (DBNotification notification)
			: base (notification)
		{
			/* nothing to do here really */
		}

		protected override void Notify (DBWork work, DBRevisionWork revision_work, List<DBPerson> people, string message)
		{
			throw new NotImplementedException ();
		}

		public override void Stop ()
		{
			/* Nothing to do */
		}
	}
}