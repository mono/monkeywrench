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

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;

namespace MonkeyWrench.WebServices
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
				if (notifications_per_lane != null)
					notifications_per_lane.Clear ();
			}
		}

		public static void Notify (DBWork work, DBRevisionWork revision_work)
		{
			Logger.Log ("Notifications.Notify (lane_id: {1} revision_id: {2} host_id: {3} State: {0})", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id);
			if (notifications == null)
				return;

			if (!(work.State == DBState.Failed || work.State == DBState.Issues || work.State == DBState.Timeout))
				return;

			ThreadPool.QueueUserWorkItem ((v) => ProcessNotify (work, revision_work));
		}

		private static void ProcessNotify (DBWork work, DBRevisionWork revision_work)
		{
			List<NotificationBase> notifications;

			Logger.Log ("Notifications.ProcessNotify (lane_id: {1} revision_id: {2} host_id: {3} State: {0})", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id);

			try {
				lock (lock_obj) {
					// We broadcast the notification to the API endpoint
					WebNotification.BroadcastBuildNotification (work, revision_work);

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

			if ((work.State == DBState.Issues || work.State == DBState.Timeout) && Notification.Type == DBNotificationType.FatalFailuresOnly)
				return false;

			/* We need to see if there are any successfull builds later than this one */
			using (DB db = new DB ()) {
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"
SELECT state FROM RevisionWork
INNER JOIN Revision ON RevisionWork.revision_id = Revision.id
WHERE RevisionWork.lane_id = @lane_id AND RevisionWork.host_id = @host_id AND RevisionWork.completed AND Revision.date > (SELECT date FROM Revision WHERE id = @revision_id) AND";

					switch (Notification.Type) {
					case DBNotificationType.FatalFailuresOnly:
						// Success or Issues
						cmd.CommandText += " (RevisionWork.state = 3 OR RevisionWork.state = 8) ";
						break;
					case DBNotificationType.AllFailures:
					case DBNotificationType.NonFatalFailuresOnly:
					default:
						// Just success
						cmd.CommandText += " RevisionWork.state = 3 ";
						break;
					}

	cmd.CommandText += @"
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
				return work.State == DBState.Issues || work.State == DBState.Failed || work.State == DBState.Timeout;
			case DBNotificationType.FatalFailuresOnly:
				if (nonfatal)
					return false;
				return work.State == DBState.Failed && newest_state == DBState.Failed;
			case DBNotificationType.NonFatalFailuresOnly:
				return (work.State == DBState.Issues || work.State == DBState.Timeout) || nonfatal;
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
		DBIrcIdentity identity;
		bool enabled = true;
		ObjectCache cache = new MemoryCache ("IrcCache");

		static Thread message_thread;
		static Queue<IrcMessage> message_list = new Queue<IrcMessage> ();
		static AutoResetEvent message_event = new AutoResetEvent (false);
		static bool message_pumping = true;

		class IrcMessage {
			public string Server;
			public string Password;
			public bool UseSSL;
			public string Nick;
			public string[] Channels;
			public int Port;
			public IEnumerable<string> Messages;
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

				var message = new IrcMessage ()
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
				while (message_pumping) {
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
							Logger.Log ("IrcNotification.MessagePump: sent message in {0} ms", watch.ElapsedMilliseconds);
						} catch (Exception ex) {
							Logger.Log ("IrcNotification.MessagePump: failed to send message: {0}", ex);
						}
					}
				}
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
				try {
					sslStream.AuthenticateAsClient(Server);
				} catch (IOException ex) {
					throw new Exception ("Could not connect to: " + Server + ":" + Port + " " + ex.Message, ex);
				}
				stream = sslStream;
			}

			var reader = new StreamReader (stream);
			var writer = new StreamWriter (stream);

//			var t = new Thread (() => {
//				try {
//					var output = reader.ReadToEnd ();
//					if (!string.IsNullOrEmpty (output))
//						Logger.Log ("IRC output: \n{0}", output);
//				} catch (ThreadInterruptedException) {
//					// do nothing.
//				} catch (Exception ex) {
//					Logger.Log ("IrcNotification.SendMessages: exception while reading irc output: {0}", ex);
//				}
//			}) {
//				IsBackground = true,
//			};
//			t.Start ();

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

//			t.Interrupt ();
//			if (!t.Join (TimeSpan.FromSeconds (5)))
//				Logger.Log ("IrcNotification.SendMessages: reader thread did not finish within 30 seconds.");
		}

		protected override void Notify (DBWork work, DBRevisionWork revision_work, List<DBPerson> people, string message)
		{
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

