using System;
using System.Collections.Generic;
using log4net;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.WebServices;

namespace MonkeyWrench.Web.WebService
{
	public static class WorkAssignment
	{
		private static readonly ILog log = LogManager.GetLogger (typeof (WorkAssignment));

		public static GetBuildInfoResponse assignRandomLane(DB db, DBHost host, bool multiple_work) {
			var response = new GetBuildInfoResponse ();
			response.Host = host;
			response.Work = new List<List<BuildInfoEntry>> ();

			DBRevisionWork revisionwork = null;
			using (var transaction = db.BeginTransaction())
			using (var cmd = db.CreateCommand ()) {
				cmd.CommandText = @"
					-- We select a revision work for the host to do by getting the latest revision work for each
					-- assigned hostlane and picking a random one. This means that the latest builds are built first
					-- while ensuring that less-used lanes are not starved.
					SELECT *
					FROM (
						-- The distinct+order by limits the query to one revisionwork per hostlane
						SELECT DISTINCT ON (HostLane.id) RevisionWork.*
						FROM Host

						-- Find hostlanes that belong to either us or our masters, and are also enabled
						INNER JOIN HostLane ON HostLane.host_id IN (
							SELECT Host.id
							UNION ALL
							SELECT MasterHost.master_host_id FROM MasterHost WHERE MasterHost.host_id = Host.id
						) AND HostLane.enabled
						
						INNER JOIN RevisionWork ON RevisionWork.lane_id = HostLane.lane_id AND RevisionWork.host_id = HostLane.host_id
						INNER JOIN Revision ON Revision.id = RevisionWork.revision_id
						INNER JOIN Lane ON Lane.id = RevisionWork.lane_id AND Lane.enabled
						
						WHERE Host.id = @host_id
						AND (RevisionWork.workhost_id IS NULL OR RevisionWork.workhost_id = Host.id)
						AND RevisionWork.state NOT IN (9,10,11) -- Don't include revworks marked as ignored, with unfulfilled dependencies, or that have no work
						AND NOT RevisionWork.completed
						
						ORDER BY HostLane.id, RevisionWork.workhost_id IS NULL ASC, RevisionWork.id
					) AS t
					-- Pick a random one
					ORDER BY random() LIMIT 1;
				";
				DB.CreateParameter (cmd, "host_id", host.id);

				while (revisionwork == null) {
					using (var reader = cmd.ExecuteReader ()) {
						if (!reader.Read ())
							return response; // No work for this host

						revisionwork = new DBRevisionWork (reader);
					}

					if (!revisionwork.SetWorkHost (db, host))
						revisionwork = null; // Someone locked it before us, try again with a different revision work
				}

				transaction.Commit ();
			}

			var lane = DBLane_Extensions.Create (db, revisionwork.lane_id);
			var workingForHost = DBHost_Extensions.Create (db, revisionwork.host_id);

			DBHostLane hostlane;
			using (var cmd = db.CreateCommand ()) {
				cmd.CommandText = @"
					SELECT * FROM HostLane WHERE host_id = @host_id AND lane_id = @lane_id;
				";
				DB.CreateParameter (cmd, "host_id", revisionwork.host_id);
				DB.CreateParameter (cmd, "lane_id", revisionwork.lane_id);

				using (var reader = cmd.ExecuteReader ()) {
					if (!reader.Read ()) {
						log.ErrorFormat ("HostLane for lane {0} host {1} no longer exists.", lane.id, host.id);
						return response;
					}
					hostlane = new DBHostLane (reader);
				}
			}

			log.DebugFormat ("Found work for host {0} {4}: {1} (lane: {2} {3})", response.Host.id, revisionwork.id, revisionwork.lane_id, lane.lane, host);

			DBRevision revision = DBRevision_Extensions.Create (db, revisionwork.revision_id);
			List<DBWorkFile> files_to_download = null;
			List<DBLane> dependent_lanes = null;

			// get dependent files
			getDependentLanesAndFiles (db, lane, revision, out dependent_lanes, out files_to_download);

			var pending_work = revisionwork.GetNextWork (db, lane, workingForHost, revision, multiple_work);
			if (pending_work == null || pending_work.Count == 0)
				return response;

			foreach (var work in pending_work) {
				BuildInfoEntry entry = new BuildInfoEntry ();
				entry.Lane = lane;
				entry.HostLane = hostlane;
				entry.Revision = revision;
				entry.Command = DBCommand_Extensions.Create (db, work.command_id);
				entry.FilesToDownload = files_to_download;
				entry.DependentLaneOfFiles = dependent_lanes;
				entry.Work = DBWork_Extensions.Create (db, work.id);
				entry.LaneFiles = lane.GetFiles (db, recursive: true);
				entry.EnvironmentVariables = getEnvVars (db, lane, revisionwork);
				entry.Host = workingForHost;

				var entries = new List<BuildInfoEntry> ();
				entries.Add (entry);
				response.Work.Add (entries);
			}

			// Notify that the revision is assigned
			var notifyInfo = new GenericNotificationInfo ();
			notifyInfo.laneID = revisionwork.lane_id;
			notifyInfo.hostID = revisionwork.host_id;
			notifyInfo.revisionID = revisionwork.revision_id;
			notifyInfo.message = String.Format ("Assigned to host '{0}' ({1})", response.Host.host, response.Host.id);
			notifyInfo.state = DBState.Executing;

			Notifications.NotifyGeneric (notifyInfo);
			return response;
		}

		public static GetBuildInfoResponse assignOnePerLane(DB db, DBHost host, bool multiple_work) {
			var response = new GetBuildInfoResponse ();
			var hosts = new List<DBHost> ();

			response.Work = new List<List<BuildInfoEntry>> ();
			response.Host = host;

			// find the master hosts for this host (if any)
			response.MasterHosts = MonkeyWrench.WebServices.WebServices.FindMasterHosts (db, response.Host);

			// get the hosts to find work for
			if (response.MasterHosts != null && response.MasterHosts.Count > 0) {
				foreach (DBMasterHost mh in response.MasterHosts)
					hosts.Add (DBHost_Extensions.Create (db, mh.master_host_id));
			} else {
				hosts.Add (response.Host);
			}

			// find the enabled hostlane combinations for these hosts
			var hostlanes = new List<DBHostLane> ();
			using (var cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT HostLane.* FROM HostLane INNER JOIN Lane ON Lane.id = HostLane.lane_id WHERE Lane.enabled = TRUE AND HostLane.enabled = TRUE AND (";
				for (int i = 0; i < hosts.Count; i++) {
					if (i > 0)
						cmd.CommandText += " OR ";
					cmd.CommandText += " HostLane.host_id = " + hosts [i].id;
				}
				cmd.CommandText += ")";
				using (var reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						hostlanes.Add (new DBHostLane (reader));
				}
			}

			if (hostlanes.Count == 0)
				return response; // nothing to do here

			var lanes = db.GetAllLanes ();

			/*switch (response.Host.QueueManagement) {
			case DBQueueManagement.OneRevisionWorkAtATime:
				if (hostlanes.Count > 1) {
					int latest = -1;
					DateTime latest_date = DateTime.MaxValue;

					// we need to find the latest revisionwork each hostlane has completed.
					// we want to work on the hostlane which has waited the longest amount
					// of time without getting work done (but which has pending work to do).

					for (int i = 0; i < hostlanes.Count; i++) {
						DBHostLane hl = hostlanes [i];
						// check if this hostlane has pending work.
						// this would ideally be included in the query below, but I'm not sure
						// how to do that while still distinguising the case where nothing has
						// been done ever for a hostlane.
						using (var cmd = db.CreateCommand ()) {
							cmd.CommandText = @"
								SELECT RevisionWork.id
								FROM RevisionWork
								WHERE
								    RevisionWork.host_id = @host_id
								AND (RevisionWork.workhost_id = @workhost_id OR RevisionWork.workhost_id IS NULL)
								AND RevisionWork.completed = false
								AND RevisionWork.state <> 9 AND RevisionWork.state <> 10 AND RevisionWork.state <> 11
								AND lane_id = @lane_id
								LIMIT 1;
							";
							DB.CreateParameter (cmd, "lane_id", hl.lane_id);
							DB.CreateParameter (cmd, "host_id", hl.host_id);
							DB.CreateParameter (cmd, "workhost_id", response.Host.id);

							object obj = cmd.ExecuteScalar ();
							if (obj == DBNull.Value || obj == null) {
								// there is nothing to do for this hostlane
								continue;
							}

						}

						// find the latest completed (this may not be correct, maybe find the latest unstarted?)
						// revisionwork for this hostlane.
						using (var cmd = db.CreateCommand ()) {
							cmd.CommandText = @"
								SELECT 	RevisionWork.endtime
								FROM RevisionWork
								WHERE 
								RevisionWork.host_id = @host_id
								AND (RevisionWork.workhost_id = @workhost_id OR RevisionWork.workhost_id IS NULL)
								AND RevisionWork.completed = true
								AND lane_id = @lane_id
								ORDER BY RevisionWork.endtime DESC
								LIMIT 1;
							";

							DB.CreateParameter (cmd, "lane_id", hl.lane_id);
							DB.CreateParameter (cmd, "host_id", hl.host_id);
							DB.CreateParameter (cmd, "workhost_id", response.Host.id);

							object obj = cmd.ExecuteScalar ();
							if (obj is DateTime) {
								DateTime dt = (DateTime)obj;
								if (dt < latest_date) {
									latest_date = dt;
									latest = i;
								}
							} else {
								// nothing has ever been done for this hostlane.
								latest_date = DateTime.MinValue;
								latest = i;
							}
						}

					}
					if (latest >= 0) {
						DBHostLane tmp = hostlanes [latest];
						hostlanes.Clear ();
						hostlanes.Add (tmp);
					} else {
						hostlanes.Clear (); // there is nothing to do at all
					}
				}
				break;
			}*/

			foreach (DBHostLane hl in hostlanes) {
				int counter = 10;
				DBRevisionWork revisionwork;
				DBLane lane = null;
				DBHost masterhost = null;

				foreach (DBLane l in lanes) {
					if (l.id == hl.lane_id) {
						lane = l;
						break;
					}
				}
				foreach (DBHost hh in hosts) {
					if (hh.id == hl.host_id) {
						masterhost = hh;
						break;
					}
				}

				do {
					revisionwork = db.GetRevisionWork (lane, masterhost, response.Host);
					if (revisionwork == null)
						break;
				} while (!revisionwork.SetWorkHost (db, response.Host) && counter-- > 0);

				if (revisionwork == null)
					continue;

				if (!revisionwork.workhost_id.HasValue || revisionwork.workhost_id != response.Host.id)
					continue; // couldn't lock this revisionwork.

				log.DebugFormat ("Found work for host {0} {4}: {1} (lane: {2} {3})", response.Host.id, revisionwork.id, revisionwork.lane_id, lane.lane, response.Host.host);

				DBRevision revision = DBRevision_Extensions.Create (db, revisionwork.revision_id);
				List<DBWorkFile> files_to_download = null;
				List<DBLane> dependent_lanes = null;

				// get dependent files
				getDependentLanesAndFiles (db, lane, revision, out dependent_lanes, out files_to_download);

				List<DBWorkView2> pending_work = revisionwork.GetNextWork (db, lane, masterhost, revision, multiple_work);

				if (pending_work == null || pending_work.Count == 0)
					continue;

				var environment_variables = getEnvVars (db, lane, revisionwork);
				DBHost host_being_worked_for = hosts.Find (h => h.id == revisionwork.host_id);

				foreach (DBWorkView2 work in pending_work) {
					BuildInfoEntry entry = new BuildInfoEntry ();
					entry.Lane = lane;
					entry.HostLane = hl;
					entry.Revision = revision;
					entry.Command = DBCommand_Extensions.Create (db, work.command_id);
					entry.FilesToDownload = files_to_download;
					entry.DependentLaneOfFiles = dependent_lanes;
					entry.Work = DBWork_Extensions.Create (db, work.id);
					entry.LaneFiles = lane.GetFiles (db, lanes);
					entry.EnvironmentVariables = environment_variables;
					entry.Host = host_being_worked_for;

					// TODO: put work with the same sequence number into one list of entries.
					List<BuildInfoEntry> entries = new List<BuildInfoEntry> ();
					entries.Add (entry);
					response.Work.Add (entries);
				}

				// Notify that the revision is assigned
				var notifyInfo = new GenericNotificationInfo ();
				notifyInfo.laneID = revisionwork.lane_id;
				notifyInfo.hostID = revisionwork.host_id;
				notifyInfo.revisionID = revisionwork.revision_id;
				notifyInfo.message = String.Format ("Assigned to host '{0}' ({1})", response.Host.host, response.Host.id);
				notifyInfo.state = DBState.Executing;

				Notifications.NotifyGeneric (notifyInfo);
			}
			return response;
		}

		private static void getDependentLanesAndFiles(DB db, DBLane lane, DBRevision revision, out List<DBLane> lanes, out List<DBWorkFile> files_to_download) {
			lanes = new List<DBLane> ();
			files_to_download = new List<DBWorkFile> ();

			var dependencies = lane.GetDependencies (db);
			foreach (DBLaneDependency dep in dependencies) {
				if (string.IsNullOrEmpty (dep.download_files))
					continue;

				var dependent_lane = DBLane_Extensions.Create (db, dep.dependent_lane_id);
				var dependent_host = dep.dependent_host_id.HasValue ? DBHost_Extensions.Create (db, dep.dependent_host_id.Value) : null;
				var dependent_revision = dependent_lane.FindRevision (db, revision.revision);
				if (dependent_revision == null) {
					log.ErrorFormat ("Lane {0} ({1}) depends on lane {2} ({3}) revision {4} but it doesn't exist.",
						lane.lane, lane.id, dependent_lane.lane, dependent_lane.id, revision.revision);
					continue;
				}
				var dependent_revwork = DBRevisionWork_Extensions.Find (db, dependent_lane, dependent_host, dependent_revision);

				var files = dependent_revwork.GetFiles (db);
				foreach (var file in files) {
					bool download = true;
					foreach (string exp in dep.download_files.Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
						if (!System.Text.RegularExpressions.Regex.IsMatch (file.filename, FileUtilities.GlobToRegExp (exp))) {
							download = false;
							break;
						}
					}
					if (!download)
						continue;
					files_to_download.Add (file);
					lanes.Add (dependent_lane);
				}
			}
		}

		private static List<DBEnvironmentVariable> getEnvVars(DB db, DBLane lane, DBRevisionWork revwork) {
			var environment_variables = new List<DBEnvironmentVariable> ();
			var set = new HashSet<string> ();
			using (var cmd = db.CreateCommand ()) {
				cmd.CommandText = @"
					SELECT * 
					FROM EnvironmentVariable 
					WHERE 
					(host_id = @host_id OR host_id = @workhost_id OR host_id IS NULL) AND (lane_id = @lane_id OR lane_id IS NULL)
					ORDER BY id;
				;";//, revisionwork.workhost_id, revisionwork.host_id, li);
				DB.CreateParameter (cmd, "host_id", revwork.host_id);
				DB.CreateParameter (cmd, "workhost_id", revwork.workhost_id);
				var laneParam = cmd.CreateParameter ();
				laneParam.ParameterName = "lane_id";

				foreach (int li in db.GetLaneHierarchy (lane.id)) {
					laneParam.Value = li;
					using (var reader = cmd.ExecuteReader ()) {
						while (reader.Read ()) {
							var ev = new DBEnvironmentVariable (reader);
							if (!set.Contains (ev.name)) {
								environment_variables.Add (ev);
								set.Add (ev.name);
							}
						}
					}
				}
				return environment_variables;
			}
		}
	}
}

