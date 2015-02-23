/*
 * Scheduler.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using MonkeyWrench.WebServices;

namespace MonkeyWrench.Scheduler
{
	public static class Scheduler
	{
		private static bool is_executing;

		public static bool IsExecuting
		{
			get { return is_executing; }
		}
	
		public static void Main (string [] args)
		{
			ProcessHelper.Exit (Main2 (args)); // Work around #499702
		}

		public static int Main2 (string [] args)
		{
			if (!Configuration.LoadConfiguration (args))
				return 1;

			return ExecuteScheduler (false) ? 0 : 1;
		}

		public static void ExecuteSchedulerAsync (bool forcefullupdate)
		{
			Async.Execute (delegate (object o) 
			{
				ExecuteScheduler (forcefullupdate); 
			});
		}

		public static List<XmlDocument> GetReports (bool forcefullupdate)
		{
			List<XmlDocument> result = null;
			if (!Configuration.ForceFullUpdate && !forcefullupdate) {
				try {
					foreach (string file in Directory.GetFiles (Configuration.GetSchedulerCommitsDirectory (), "*.xml")) {
						string hack = File.ReadAllText (file);
						if (!hack.Contains ("</directory>"))
							hack = hack.Replace ("</directory", "</directory>");
						File.WriteAllText (file, hack);
						XmlDocument doc = new XmlDocument ();
						try {
							Logger.Log ("Updater: got report file '{0}'", file);
							doc.Load (file);
							if (result == null)
								result = new List<XmlDocument> ();
							result.Add (doc);
						} catch (Exception ex) {
							Logger.Log ("Updater: exception while checking commit report '{0}': {1}", file, ex);
						}
						try {
							File.Delete (file); // No need to check this file more than once.
						} catch {
							// Ignore any exceptions
						}
					}
				} catch (Exception ex) {
					Logger.Log ("Updater: exception while checking commit reports: {0}", ex);
				}
			}
			return result;
		}

		public static bool ExecuteScheduler (bool forcefullupdate)
		{
			DateTime start;
			Lock scheduler_lock = null;
			List<DBLane> lanes;

			List<DBHost> hosts;
			List<DBHostLane> hostlanes;
			List<XmlDocument> reports;
			
			try {
				scheduler_lock = Lock.Create ("MonkeyWrench.Scheduler");
				if (scheduler_lock == null) {
					Logger.Log ("Could not aquire scheduler lock.");
					return false;
				}

				Logger.Log ("Scheduler lock aquired successfully.");
				
				is_executing = true;
				start = DateTime.Now;

				// SVNUpdater.StartDiffThread ();

				// Check reports
				reports = GetReports (forcefullupdate);

				using (DB db = new DB (true)) {
					lanes = db.GetAllLanes ();
					hosts = db.GetHosts ();
					hostlanes = db.GetAllHostLanes ();

					Logger.Log ("Updater will now update {0} lanes.", lanes.Count);

					GITUpdater git_updater = null;
					// SVNUpdater svn_updater = null;

					foreach (DBLane lane in lanes) {
						if (!lane.enabled) {
							Logger.Log ("Schedule: lane {0} is disabled, skipping it.", lane.lane);
							continue;
						}

						SchedulerBase updater;
						switch (lane.source_control) {
							/*
						case "svn":
							if (svn_updater == null)
								svn_updater = new SVNUpdater (forcefullupdate);
							updater = svn_updater;
							break;
							 * */
						case "git":
							if (git_updater == null)
								git_updater = new GITUpdater (forcefullupdate);
							updater = git_updater;
							break;
						default:
							Logger.Log ("Unknown source control: {0} for lane {1}", lane.source_control, lane.lane);
							continue;
						}
						updater.Clear ();
						updater.AddChangeSets (reports);
						updater.UpdateRevisionsInDB (db, lane, hosts, hostlanes);
					}

					AddRevisionWork (db);
					AddWork (db);
					CheckDependencies (db, hosts, lanes, hostlanes);
				}

				Logger.Log ("Update done, waiting for diff thread to finish...");

				// SVNUpdater.StopDiffThread ();

				Logger.Log ("Update finished successfully in {0} seconds.", (DateTime.Now - start).TotalSeconds);

				return true;
			} finally {
				if (scheduler_lock != null)
					scheduler_lock.Unlock ();
				is_executing = false;
			}
		}

		/// <summary>
		/// Returns true if something was added to the database.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="lane"></param>
		/// <param name="host"></param>
		/// <returns></returns>
		public static bool AddRevisionWork (DB db)
		{
			using (var transaction = db.BeginTransaction ())
			using (var cmd = db.CreateCommand ()) {
				cmd.CommandText = @"
					CREATE TEMPORARY TABLE new_rev_works ON COMMIT DROP AS
						SELECT Lane.id AS lid, Host.id AS hid, Revision.id AS rid
						FROM HostLane
						INNER JOIN Host ON HostLane.host_id = Host.id
						INNER JOIN Lane ON HostLane.lane_id = Lane.id
						INNER JOIN Revision ON Revision.lane_id = lane.id
						WHERE HostLane.enabled = true AND
							NOT EXISTS (
								SELECT 1
								FROM RevisionWork 
								WHERE RevisionWork.lane_id = Lane.id AND RevisionWork.host_id = Host.id AND RevisionWork.revision_id = Revision.id
							);

					INSERT INTO RevisionWork(lane_id, host_id, revision_id, state)
					SELECT lid, hid, rid, 10
					FROM new_rev_works;
				";
				int line_count = cmd.ExecuteNonQuery ();

				cmd.CommandText = @"
					SELECT lid, lane.lane, hid, host.host, rid, revision.revision, lane.repository
					FROM new_rev_works
					INNER JOIN Lane ON Lane.id = lid
					INNER JOIN Host ON Host.id = hid
					INNER JOIN Revision ON Revision.id = rid;
				";

				using (var reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						var info = new NotificationBase.NewRevisionInfo ();
						info.laneID = reader.GetInt32 (0);
						info.lane = reader.GetString (1);
						info.hostID = reader.GetInt32 (2);
						info.host = reader.GetString (3);
						info.revID = reader.GetInt32 (4);
						info.hash = reader.GetString (5);
						info.repoURL = reader.GetString (6);

						Notifications.NotifyRevisionAdded (info);
					}
				}

				transaction.Commit ();

				Logger.Log("AddRevisionWork: Added {0} records.", line_count);
				return line_count > 0;
			}
		}

		static void CollectWork (List<DBCommand> commands_in_lane, List<DBLane> lanes, DBLane lane, List<DBCommand> commands)
		{
			while (lane != null) {
				commands_in_lane.AddRange (commands.Where (w => w.lane_id == lane.id));
				lane = lanes.FirstOrDefault ((v) => lane.parent_lane_id == v.id);
			}
		}

		private static void AddWork (DB db)
		{
			using (var transaction = db.BeginTransaction ())
			using (var cmd = db.CreateCommand ()) {
				DB.CreateParameter (cmd, "DependencyNotFulfilled", (int)DBState.DependencyNotFulfilled);
				DB.CreateParameter (cmd, "NotDone", (int)DBState.NotDone);
				DB.CreateParameter (cmd, "NoWorkYet", (int)DBState.NoWorkYet);

				cmd.CommandText = @"
					-- Prevent other connections from adding revisionworks between us adding work for them and updating their state.
					LOCK TABLE revisionwork IN ROW EXCLUSIVE MODE;

					-- Add works for each revisionwork
					INSERT INTO work (command_id, revisionwork_id, state)
					SELECT
						command.id,
						revisionwork.id,
						CASE
							WHEN EXISTS(SELECT 1 FROM lanedependency WHERE lane_id = revisionwork.lane_id) THEN @DependencyNotFulfilled
							ELSE @NotDone
						END
					FROM revisionwork
					INNER JOIN hostlane ON hostlane.lane_id = revisionwork.lane_id AND hostlane.host_id = revisionwork.host_id
					INNER JOIN command ON command.lane_id = revisionwork.lane_id
					WHERE revisionwork.state = @NoWorkYet AND hostlane.enabled;

					-- Update revisionwork state
					UPDATE revisionwork
					SET state = CASE
						WHEN EXISTS(SELECT 1 FROM lanedependency WHERE lane_id = revisionwork.lane_id) THEN @DependencyNotFulfilled
						ELSE @NotDone
					END
					WHERE state = @NoWorkYet;
				";
				cmd.ExecuteNonQuery ();
				transaction.Commit ();
			}
		}

		private static void CheckDependenciesSlow (DB db, List<DBHost> hosts, List<DBLane> lanes, List<DBHostLane> hostlanes, List<DBLaneDependency> dependencies)
		{
			DateTime start = DateTime.Now;
			//List<DBRevision> revisions = new List<DBRevision> ();
			//List<DBCommand> commands = null;
			//IEnumerable<DBLaneDependency> dependencies_in_lane;
			//IEnumerable<DBCommand> commands_in_lane;
			//List<DBRevisionWork> revisionwork_without_work = new List<DBRevisionWork> ();
			//DBHostLane hostlane;
			//StringBuilder sql = new StringBuilder ();

			try {
				Logger.Log ("CheckDependenciesSlow: IMPLEMENTED, BUT NOT TESTED");
				return;

				//Logger.Log (1, "CheckDependenciesSlow: Checking {0} dependencies", dependencies.Count);

				///* Find the revision works which has unfulfilled dependencies */
				//using (IDbCommand cmd = db.CreateCommand ()) {
				//    cmd.CommandText = "SELECT * FROM RevisionWork WHERE state = 9;";
				//    using (IDataReader reader = cmd.ExecuteReader ()) {
				//        while (reader.Read ()) {
				//            revisionwork_without_work.Add (new DBRevisionWork (reader));
				//        }
				//    }
				//}

				//Logger.Log (1, "CheckDependencies: Got {0} revisionwork with unfulfilled dependencies", revisionwork_without_work.Count);

				//foreach (DBLane lane in lanes) {
				//    dependencies_in_lane = null;
				//    commands_in_lane = null;

				//    foreach (DBHost host in hosts) {
				//        hostlane = null;
				//        for (int i = 0; i < hostlanes.Count; i++) {
				//            if (hostlanes [i].lane_id == lane.id && hostlanes [i].host_id == host.id) {
				//                hostlane = hostlanes [i];
				//                break;
				//            }
				//        }

				//        if (hostlane == null) {
				//            Logger.Log (2, "CheckDependencies: Lane '{0}' is not configured for host '{1}', not checking dependencies.", lane.lane, host.host);
				//            continue;
				//        } else if (!hostlane.enabled) {
				//            Logger.Log (2, "CheckDependencies: Lane '{0}' is disabled for host '{1}', not checking dependencies.", lane.lane, host.host);
				//            continue;
				//        }

				//        Logger.Log (1, "CheckDependencies: Lane '{0}' is enabled for host '{1}', checking dependencies...", lane.lane, host.host);

				//        foreach (DBRevisionWork revisionwork in revisionwork_without_work) {
				//            bool dependencies_satisfied = true;

				//            /* revisionwork_without_work contains rw for all hosts/lanes, filter out the ones we want */
				//            if (revisionwork.host_id != host.id || revisionwork.lane_id != lane.id)
				//                continue;

				//            /* Get commands and dependencies for all lanes only if we know we'll need them */
				//            if (commands == null)
				//                commands = db.GetCommands (0);
				//            if (commands_in_lane == null)
				//                commands_in_lane = commands.Where (w => w.lane_id == lane.id);
				//            if (dependencies == null)
				//                dependencies = DBLaneDependency_Extensions.GetDependencies (db, null);
				//            if (dependencies_in_lane == null)
				//                dependencies_in_lane = dependencies.Where (dep => dep.lane_id == lane.id);

				//            /* Check dependencies */
				//            if (dependencies_in_lane.Count () > 0) {
				//                DBRevision revision = revisions.FirstOrDefault (r => r.id == revisionwork.revision_id);
				//                if (revision == null) {
				//                    revision = DBRevision_Extensions.Create (db, revisionwork.revision_id);
				//                    revisions.Add (revision);
				//                }

				//                Logger.Log (2, "CheckDependencies: Lane '{0}', revision '{1}' checking dependencies...", lane.lane, revision.revision);

				//                foreach (DBLaneDependency dependency in dependencies)
				//                    dependencies_satisfied &= dependency.IsSuccess (db, revision.revision);

				//                Logger.Log (2, "CheckDependencies: Lane '{0}', revision '{1}' dependency checking resulted in: {2}.", lane.lane, revision.revision, dependencies_satisfied);
				//            }

				//            if (!dependencies_satisfied)
				//                continue;

				//            Logger.Log (2, "CheckDependencies: Lane '{0}', revisionwork_id '{1}' dependencies fulfilled", lane.lane, revisionwork.id);

				//            sql.Length = 0;
				//            sql.AppendFormat ("UPDATE Work SET state = 0 WHERE revisionwork_id = {0};\n", revisionwork.id);
				//            sql.AppendFormat ("UPDATE RevisionWork SET state = 0 WHERE id = {0};\n", revisionwork.id);

				//            db.ExecuteNonQuery (sql.ToString ());
				//        }
				//    }
				//}
			} catch (Exception ex) {
				Logger.Log ("CheckDependencies: There was an exception while checking dependencies db: {0}", ex.ToString ());
			} finally {
				Logger.Log ("CheckDependencies: [Done in {0} seconds]", (DateTime.Now - start).TotalSeconds);
			}
		}

		private static void CheckDependencies (DB db, List<DBHost> hosts, List<DBLane> lanes, List<DBHostLane> hostlanes)
		{
			DateTime start = DateTime.Now;
			StringBuilder sql = new StringBuilder ();
			List<DBLaneDependency> dependencies;

			try {
				dependencies = DBLaneDependency_Extensions.GetDependencies (db, null);

				Logger.Log (1, "CheckDependencies: Checking {0} dependencies", dependencies == null ? 0 : dependencies.Count);

				if (dependencies == null || dependencies.Count == 0)
					return;

				/* Check that there is only 1 dependency per lane and only DependentLaneSuccess condition */
				foreach (DBLaneDependency dep in dependencies) {
					if (dependencies.Any (dd => dep.id != dd.id && dep.lane_id == dd.lane_id)) {
						CheckDependenciesSlow (db, hosts, lanes, hostlanes, dependencies);
						return;
					}
					if (dep.Condition != DBLaneDependencyCondition.DependentLaneSuccess && dep.Condition != DBLaneDependencyCondition.DependentLaneIssuesOrSuccess) {
						CheckDependenciesSlow (db, hosts, lanes, hostlanes, dependencies);
						return;
					}
				}

				foreach (DBLaneDependency dependency in dependencies) {
					Logger.Log (1, "CheckDependencies: Checking dependency {0} for lane {1}", dependency.id, dependency.lane_id);
					/* Find the revision works which has filfilled dependencies */
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = @"
SELECT RevisionWork.id
FROM RevisionWork
INNER JOIN Lane ON Lane.id = RevisionWork.lane_id
INNER JOIN Host ON Host.id = RevisionWork.host_id
INNER JOIN Revision ON Revision.id = RevisionWork.revision_id
INNER JOIN LaneDependency ON LaneDependency.lane_id = RevisionWork.lane_id
WHERE
	RevisionWork.lane_id = @lane_id AND RevisionWork.state = 9 AND
	EXISTS (
		SELECT SubRevisionWork.id
		FROM RevisionWork SubRevisionWork
		INNER JOIN Revision SubRevision ON SubRevisionWork.revision_id = SubRevision.id
		WHERE SubRevisionWork.completed = true AND ";
			if (dependency.Condition == DBLaneDependencyCondition.DependentLaneSuccess) {
				cmd.CommandText += "SubRevisionWork.state = 3 ";
			} else if (dependency.Condition == DBLaneDependencyCondition.DependentLaneIssuesOrSuccess) {
				cmd.CommandText += "(SubRevisionWork.state = 3 OR SubRevisionWork.state = 8) ";
			}
			
			cmd.CommandText +=
			@"AND SubRevision.revision = Revision.revision 
			AND SubRevisionWork.lane_id = @dependent_lane_id";

						if (dependency.dependent_host_id.HasValue) {
							DB.CreateParameter (cmd, "dependent_host_id", dependency.dependent_host_id.Value);
							cmd.CommandText += @"
			AND SubRevisionWork.host_id = @dependent_host_id";
						}

						cmd.CommandText += @"
		);";
						DB.CreateParameter (cmd, "lane_id", dependency.lane_id);
						DB.CreateParameter (cmd, "dependent_lane_id", dependency.dependent_lane_id);

						sql.Length = 0;
						using (IDataReader reader = cmd.ExecuteReader ()) {
							while (reader.Read ()) {
								int rw_id = reader.GetInt32 (0);
								sql.AppendFormat ("UPDATE Work SET state = 0 WHERE revisionwork_id = {0} AND state = 9;\n", rw_id);
								sql.AppendFormat ("UPDATE RevisionWork SET state = 0 WHERE id = {0} AND state = 9;\n", rw_id);
							}
						}
						db.ExecuteNonQuery (sql.ToString ());
					}


				}
			} catch (Exception ex) {
				Logger.Log ("CheckDependencies: There was an exception while checking dependencies db: {0}", ex.ToString ());
			} finally {
				Logger.Log ("CheckDependencies: [Done in {0} seconds]", (DateTime.Now - start).TotalSeconds);
			}
		}

		public static void FindPeopleForCommit (DBLane lane, DBRevision revision, List<DBPerson> people)
		{
			if (lane.source_control == "git") {
				GITUpdater.FindPeopleForCommit (lane, revision, people);
				/*
			} else if (lane.source_control == "svn") {
				SVNUpdater.FindPeopleForCommit (lane, revision, people);
				 * */
			} else {
				Logger.Log ("FindPeopleForCommit (): unknown source control: '{0}'", lane.source_control);
			}
		}
	}
}