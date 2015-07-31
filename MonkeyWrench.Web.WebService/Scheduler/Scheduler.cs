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
using log4net;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using MonkeyWrench.WebServices;

namespace MonkeyWrench.Scheduler
{
	public static class Scheduler
	{
		private static readonly ILog log = LogManager.GetLogger (typeof (Scheduler));
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
							log.DebugFormat ("got report file '{0}'", file);
							doc.Load (file);
							if (result == null)
								result = new List<XmlDocument> ();
							result.Add (doc);
						} catch (Exception ex) {
							log.ErrorFormat ("exception while checking commit report '{0}': {1}", file, ex);
						}
						try {
							File.Delete (file); // No need to check this file more than once.
						} catch (Exception ex) {
							log.ErrorFormat("Error deleting file: {0}", ex);
							// Ignore any exceptions
						}
					}
				} catch (Exception ex) {
					log.ErrorFormat ("exception while checking commit reports: {0}", ex);
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
					log.Info ("Could not aquire scheduler lock.");
					return false;
				}

				log.Info ("Scheduler lock aquired successfully.");
				
				is_executing = true;
				start = DateTime.Now;

				// SVNUpdater.StartDiffThread ();

				// Check reports
				reports = GetReports (forcefullupdate);

				using (DB db = new DB (true)) {
					lanes = db.GetAllLanes ();
					hosts = db.GetHosts ();
					hostlanes = db.GetAllHostLanes ();

					log.InfoFormat ("Updater will now update {0} lanes.", lanes.Count);

					GITUpdater git_updater = null;
					// SVNUpdater svn_updater = null;

					foreach (DBLane lane in lanes) {
						if (!lane.enabled) {
							log.InfoFormat ("Schedule: lane {0} is disabled, skipping it.", lane.lane);
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
							log.ErrorFormat ("Unknown source control: {0} for lane {1}", lane.source_control, lane.lane);
							continue;
						}
						updater.Clear ();
						updater.AddChangeSets (reports);
						updater.UpdateRevisionsInDB (db, lane, hosts, hostlanes);
					}

					AddRevisionWork (db, lanes, hostlanes);
					AddWork (db, hosts, lanes, hostlanes);
					CheckDependencies (db, hosts, lanes, hostlanes);
				}

				// SVNUpdater.StopDiffThread ();

				log.InfoFormat ("Update finished successfully in {0} seconds.", (DateTime.Now - start).TotalSeconds);

				return true;
			} catch (Exception ex) {
				log.ErrorFormat ("An exception occurred: {0}", ex);
				return false;
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
		public static bool AddRevisionWork (DB db, List<DBLane> lanes, List<DBHostLane> hostlanes)
		{
			var stopwatch = new Stopwatch ();
			stopwatch.Start ();
			int line_count = 0;

			try {
				using (var cmd = db.CreateCommand (@"
					INSERT INTO RevisionWork (lane_id, host_id, revision_id, state)
					SELECT Lane.id, Host.id, Revision.id, 10
					FROM HostLane
					INNER JOIN Host ON HostLane.host_id = Host.id
					INNER JOIN Lane ON HostLane.lane_id = Lane.id
					INNER JOIN Revision ON Revision.lane_id = lane.id
					WHERE HostLane.enabled = true AND
						NOT EXISTS (
							SELECT 1
							FROM RevisionWork 
							WHERE RevisionWork.lane_id = Lane.id AND RevisionWork.host_id = Host.id AND RevisionWork.revision_id = Revision.id
							)
					RETURNING lane_id, host_id, revision_id
				"))
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						int lane_id = reader.GetInt32 (0);
						int host_id = reader.GetInt32 (1);
						int revision_id = reader.GetInt32 (2);

						var info = new GenericNotificationInfo(); 
						info.laneID = lane_id;
						info.hostID = host_id;
						info.revisionID = revision_id;
						info.message = "Commit received.";
						info.state = DBState.Executing;

						Notifications.NotifyGeneric (info);

						line_count++;
					}
				}
				log.DebugFormat ("AddRevisionWork: Added {0} records.", line_count);
				return line_count > 0;
			} catch (Exception ex) {
				log.ErrorFormat ("AddRevisionWork got an exception (will try a slower method): {0}", ex);
				return AddRevisionWorkSlow (db, lanes, hostlanes);
			} finally {
				stopwatch.Stop ();
				log.InfoFormat ("AddRevisionWork [Done in {0} seconds]", stopwatch.Elapsed.TotalSeconds);
			}
		}

		/// <summary>
		/// Returns true if something was added to the database.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="lanes"></param>
		/// <param name="hostlanes"></param>
		/// <returns></returns>
		public static bool AddRevisionWorkSlow (DB db, List<DBLane> lanes, List<DBHostLane> hostlanes)
		{
			var stopwatch = new Stopwatch ();
			stopwatch.Start ();
			int line_count = 0;

			try {
				var selected_lanes = new Dictionary<int, DBLane> ();
				foreach (var hl in hostlanes) {
					if (!hl.enabled)
						continue;

					if (!selected_lanes.ContainsKey (hl.lane_id))
						selected_lanes [hl.lane_id] = lanes.Find ((v) => v.id == hl.lane_id);
				}
				foreach (var l in lanes) {
					if (l.enabled)
						continue;
					if (selected_lanes.ContainsKey (l.id))
						selected_lanes.Remove (l.id);
				}
				foreach (var id in selected_lanes.Keys) {
					using (var cmd = db.CreateCommand (string.Format (@"
						INSERT INTO RevisionWork (lane_id, host_id, revision_id, state)
						SELECT Lane.id, Host.id, Revision.id, 10
						FROM HostLane
						INNER JOIN Host ON HostLane.host_id = Host.id
						INNER JOIN Lane ON HostLane.lane_id = Lane.id
						INNER JOIN Revision ON Revision.lane_id = lane.id
						WHERE HostLane.enabled = true AND Lane.id = {0} AND
							NOT EXISTS (
								SELECT 1
								FROM RevisionWork 
								WHERE RevisionWork.lane_id = Lane.id AND RevisionWork.host_id = Host.id AND RevisionWork.revision_id = Revision.id
								)
						RETURNING lane_id, host_id, revision_id
					", id)))
					using (IDataReader reader = cmd.ExecuteReader ()) {
						while (reader.Read ()) {
							int lane_id = reader.GetInt32 (0);
							int host_id = reader.GetInt32 (1);
							int revision_id = reader.GetInt32 (2);

							var info = new GenericNotificationInfo(); 
							info.laneID = lane_id;
							info.hostID = host_id;
							info.revisionID = revision_id;
							info.message = "Commit received.";
							info.state = DBState.Executing;

							Notifications.NotifyGeneric (info);

							line_count++;
						}
					}
					log.DebugFormat ("AddRevisionWorkSlow: Added {0} records for lane {1}.", line_count, selected_lanes [id].lane);
				}
				return line_count > 0;
			} catch (Exception ex) {
				log.ErrorFormat ("AddRevisionWorkSlow got an exception: {0}", ex);
				return false;
			} finally {
				stopwatch.Stop ();
				log.InfoFormat ("AddRevisionWorkSlow [Done in {0} seconds]", stopwatch.Elapsed.TotalSeconds);
			}
		}

		static void CollectWork (List<DBCommand> commands_in_lane, List<DBLane> lanes, DBLane lane, List<DBCommand> commands)
		{
			while (lane != null) {
				commands_in_lane.AddRange (commands.Where (w => w.lane_id == lane.id));
				lane = lanes.FirstOrDefault ((v) => lane.parent_lane_id == v.id);
			}
		}

		private static void AddWork (DB db, List<DBHost> hosts, List<DBLane> lanes, List<DBHostLane> hostlanes)
		{
			DateTime start = DateTime.Now;
			List<DBCommand> commands = null;
			List<DBLaneDependency> dependencies = null;
			List<DBCommand> commands_in_lane;
			List<DBRevisionWork> revisionwork_without_work = new List<DBRevisionWork> ();
			DBHostLane hostlane;
			StringBuilder sql = new StringBuilder ();
			bool fetched_dependencies = false;
			int lines = 0;

			try {
				/* Find the revision works which don't have work yet */
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = "SELECT * FROM RevisionWork WHERE state = 10;";
					using (IDataReader reader = cmd.ExecuteReader ()) {
						while (reader.Read ()) {
							revisionwork_without_work.Add (new DBRevisionWork (reader));
						}
					}
				}

				log.InfoFormat ("AddWork: Got {0} hosts and {1} revisionwork without work", hosts.Count, revisionwork_without_work.Count);

				foreach (DBLane lane in lanes) {
					commands_in_lane = null;

					foreach (DBHost host in hosts) {
						hostlane = null;
						for (int i = 0; i < hostlanes.Count; i++) {
							if (hostlanes [i].lane_id == lane.id && hostlanes [i].host_id == host.id) {
								hostlane = hostlanes [i];
								break;
							}
						}

						if (hostlane == null) {
							log.DebugFormat ("AddWork: Lane '{0}' is not configured for host '{1}', not adding any work.", lane.lane, host.host);
							continue;
						} else if (!hostlane.enabled) {
							log.DebugFormat ("AddWork: Lane '{0}' is disabled for host '{1}', not adding any work.", lane.lane, host.host);
							continue;
						}

						log.InfoFormat ("AddWork: Lane '{0}' is enabled for host '{1}', adding work!", lane.lane, host.host);

						foreach (DBRevisionWork revisionwork in revisionwork_without_work) {
							bool has_dependencies;

							/* revisionwork_without_work contains rw for all hosts/lanes, filter out the ones we want */
							if (revisionwork.host_id != host.id || revisionwork.lane_id != lane.id)
								continue;

							/* Get commands and dependencies for all lanes only if we know we'll need them */
							if (commands == null)
								commands = db.GetCommands (0);
							if (commands_in_lane == null) {
								commands_in_lane = new List<DBCommand> ();
								CollectWork (commands_in_lane, lanes, lane, commands);
							}

							if (!fetched_dependencies) {
								fetched_dependencies = true;
								dependencies = DBLaneDependency_Extensions.GetDependencies (db, null);
							}

							has_dependencies = dependencies != null && dependencies.Any (dep => dep.lane_id == lane.id);

							log.DebugFormat ("AddWork: Lane '{0}', revisionwork_id '{1}' has dependencies: {2}", lane.lane, revisionwork.id, has_dependencies);

							foreach (DBCommand command in commands_in_lane) {
								int work_state = (int) (has_dependencies ? DBState.DependencyNotFulfilled : DBState.NotDone);

								sql.AppendFormat ("INSERT INTO Work (command_id, revisionwork_id, state) VALUES ({0}, {1}, {2});\n", command.id, revisionwork.id, work_state);
								lines++;


								log.DebugFormat ("Lane '{0}', revisionwork_id '{1}' Added work for command '{2}'", lane.lane, revisionwork.id, command.command);

								if ((lines % 100) == 0) {
									db.ExecuteNonQuery (sql.ToString ());
									sql.Clear ();
									log.DebugFormat ("AddWork: flushed work queue, added {0} items now.", lines);
								}
							}

							sql.AppendFormat ("UPDATE RevisionWork SET state = {0} WHERE id = {1} AND state = 10;", (int) (has_dependencies ? DBState.DependencyNotFulfilled : DBState.NotDone), revisionwork.id);

						}
					}
				}
				if (sql.Length > 0)
					db.ExecuteNonQuery (sql.ToString ());
			} catch (Exception ex) {
				log.ErrorFormat ("AddWork: {0}", ex);
			}
			log.InfoFormat ("AddWork: [Done in {0} seconds]", (DateTime.Now - start).TotalSeconds);
		}

		private static void CheckDependenciesSlow (DB db, List<DBHost> hosts, List<DBLane> lanes, List<DBHostLane> hostlanes, List<DBLaneDependency> dependencies)
		{
			throw new NotImplementedException ("CheckDependenciesSlow not implemented; make sure each lane has only one dependency and that they use either DependentLaneSuccess or DependentLaneIssuesOrSuccess");
		}

		private static void CheckDependencies (DB db, List<DBHost> hosts, List<DBLane> lanes, List<DBHostLane> hostlanes)
		{
			DateTime start = DateTime.Now;
			StringBuilder sql = new StringBuilder ();
			List<DBLaneDependency> dependencies;

			try {
				dependencies = DBLaneDependency_Extensions.GetDependencies (db, null);

				log.InfoFormat ("CheckDependencies: Checking {0} dependencies", dependencies == null ? 0 : dependencies.Count);

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
					log.InfoFormat ("CheckDependencies: Checking dependency {0} for lane {1}", dependency.id, dependency.lane_id);
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
				log.ErrorFormat ("CheckDependencies: There was an exception while checking dependencies db: {0}", ex);
			} finally {
				log.InfoFormat ("CheckDependencies: [Done in {0} seconds]", (DateTime.Now - start).TotalSeconds);
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
				log.ErrorFormat ("Unknown source control for lane {0}: {1}", lane.lane, lane.source_control);
			}
		}
	}
}
