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
using System.Collections.Concurrent;
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
using System.Collections.Specialized;

namespace MonkeyWrench.WebServices
{
	public struct GenericNotificationInfo
	{
		public int laneID;
		public int hostID;
		public int revisionID;

		public string message;
		public DBState state;

		public GenericNotificationInfo (int laneID, int hostID, int revisionID, string message, DBState state)
		{
			this.laneID = laneID;
			this.hostID = hostID;
			this.revisionID = revisionID;
			this.message = message;
			this.state = state;
		}
	}

	class QueuedNotification {
		public bool IsInfo;
		public GenericNotificationInfo info;
		public DBRevisionWork revision_work;
		public DBWork work;
	}

	public static class Notifications
	{
		static object lock_obj = new object ();
		static List<NotificationBase> notifications = new List<NotificationBase> ();
		static Dictionary<int, List<NotificationBase>> notifications_per_lane = new Dictionary<int, List<NotificationBase>> ();
		static Thread notifier_thread;
		static BlockingCollection<QueuedNotification> queued_notifications;

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
								} else if (n.githubidentity_id.HasValue) {
									Logger.Log ("Starting GitHub notification");
									notifications.Add (new GitHubNotification (n));
								} else {
									Logger.Log ("Unknown notification");
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

			if (notifier_thread == null) {
				notifier_thread = new Thread (NotificationProcessor);
				notifier_thread.IsBackground = true;
				notifier_thread.Start ();
				queued_notifications = new BlockingCollection<QueuedNotification> ();
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

		static void NotificationProcessor ()
		{
			try {
				while (true) {
					var item = queued_notifications.Take ();
					if (item.IsInfo) {
						ProcessNotify (item.info);
					} else {
						ProcessNotify (item.work, item.revision_work);
					}
				}
			} catch (Exception ex) {
				Logger.Log ("Notifications.NotificationProcessor: exception caught, no more notifications will be processed: {0}", ex);
			}
		}

		public static void Notify (DBWork work, DBRevisionWork revision_work)
		{
			Logger.Log ("Notifications.Notify (lane_id: {1} revision_id: {2} host_id: {3} State: {0})", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id);
			if (notifications == null)
				return;

			queued_notifications.Add (new QueuedNotification { IsInfo = false, work = work, revision_work = revision_work });
		}

		public static void NotifyGeneric (GenericNotificationInfo info)
		{
			queued_notifications.Add (new QueuedNotification { IsInfo = true, info = info });
		}

		private static void ProcessNotify (DBWork work, DBRevisionWork revision_work)
		{
			Logger.Log ("Running notifiers for lane_id: {1}, revision_id: {2}, host_id: {3}, State: {0}", work.State, revision_work.lane_id, revision_work.revision_id, revision_work.host_id);

			try {
				lock (lock_obj) {
					List<NotificationBase> notifiers;

					// We broadcast the notification to the API endpoint
					WebNotification.BroadcastBuildNotification (work, revision_work);

					if (!notifications_per_lane.TryGetValue (revision_work.lane_id, out notifiers)) {
						return;
					}

					foreach (var notification in notifiers) {
						notification.Notify (work, revision_work);
					}
				}
			} catch (Exception ex) {
				Logger.Log ("Exception while processing notification: {0}", ex);
			}
		}

		private static void ProcessNotify(GenericNotificationInfo info)
		{
			Logger.Log ("Running notifiers for lane_id: {1}, revision_id: {2}, host_id: {3}, message: {0}", info.message, info.laneID, info.revisionID, info.hostID);

			try {
				lock (lock_obj) {
					List<NotificationBase> notifiers;

					if (!notifications_per_lane.TryGetValue (info.laneID, out notifiers)) {
						return;
					}

					foreach (var notification in notifiers) {
						notification.NotifyGeneric (info);
					}
				}
			} catch (Exception ex) {
				Logger.Log ("Exception while processing notification: {0}", ex);
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

		protected bool Evaluate (DBWork work, DBRevisionWork revision_work, out bool nonfatal)
		{
			DBState newest_state = work.State;

			nonfatal = false;

			if (work.State == DBState.Success)
				return false;

			if ((work.State == DBState.Issues || work.State == DBState.Timeout) && Notification.Type == DBNotificationType.FatalFailuresOnly)
				return false;

			/* We need to see if there are any successfull builds later than this one */
			using (DB db = new DB ()) {
				using (var cmd = db.CreateCommand ()) {
					cmd.CommandText += @"SELECT nonfatal FROM Command WHERE id = @command_id;";
					DB.CreateParameter (cmd, "command_id", work.command_id);

					using (IDataReader reader = cmd.ExecuteReader ()) {
						if (reader.Read ())
							nonfatal = reader.GetBoolean (0);
					}
				}

				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"
SELECT state FROM RevisionWork
INNER JOIN Revision ON RevisionWork.revision_id = Revision.id
WHERE RevisionWork.lane_id = @lane_id AND RevisionWork.host_id = @host_id AND RevisionWork.completed AND Revision.date > (SELECT date FROM Revision WHERE id = @revision_id) AND";

					if (nonfatal) {
						cmd.CommandText += " RevisionWork.state = 3 ";
					} else {
						cmd.CommandText += " (RevisionWork.state = 3 OR RevisionWork.state = 8) ";
					}

	cmd.CommandText += @"
ORDER BY Revision.date DESC
LIMIT 1;
";
					DB.CreateParameter (cmd, "lane_id", revision_work.lane_id);
					DB.CreateParameter (cmd, "host_id", revision_work.host_id);
					DB.CreateParameter (cmd, "revision_id", revision_work.revision_id);
					DB.CreateParameter (cmd, "command_id", work.command_id);

					object obj_state = null;

					using (IDataReader reader = cmd.ExecuteReader ()) {
						if (reader.Read ())
							obj_state = reader [0];
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
		public virtual void Notify (DBWork work, DBRevisionWork revision_work)
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

		/**
		 * Used for notifications not tied to a specific work. (ex. revision downloaded or assigned)
		 */
		public virtual void NotifyGeneric(GenericNotificationInfo info) {}

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
}

