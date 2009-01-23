/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
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
using System.Text;
using System.Threading;
using System.Xml;

namespace Builder
{
	class SVNUpdater : IUpdater
	{
		private static bool quit_svn_diff = false;
		private static Thread diff_thread;

		public bool UpdateRevisionsInDB (DB db, DBLane lane, List<DBHost> hosts, List<DBHostLane> hostlanes)
		{
			string revision;
			XmlDocument svn_log;
			Dictionary<string, DBRevision> revisions;
			bool update_steps = false;
			DBRevision r;
			int min_revision = 0;
			int max_revision = int.MaxValue;
			int current_revision;
			string log;
			bool skip_lane;

			Logger.Log ("SVN: Updating '{0}'", lane.lane);

			try {
				// Skip lanes which aren't configured/enabled on any host completely.
				skip_lane = true;
				for (int i = 0; i < hostlanes.Count; i++) {
					if (hostlanes [i].lane_id == lane.id && hostlanes [i].enabled) {
						skip_lane = false;
						break;
					}
				}
				if (skip_lane) {
					Logger.Log ("SVN: Skipping lane {0}, not enabled or configured on any host.", lane.lane);
					return false;
				}

				revisions = db.GetDBRevisions (lane.id);

				foreach (string repository in lane.repository.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
					log = GetSVNLog (lane, repository);

					if (string.IsNullOrEmpty (log)) {
						Logger.Log ("SVN: Didn't get a svn log for '{0}'", repository);
						continue;
					}

					svn_log = new XmlDocument ();
					svn_log.PreserveWhitespace = true;
					svn_log.Load (new StringReader (log));

					if (!string.IsNullOrEmpty (lane.min_revision))
						min_revision = int.Parse (lane.min_revision);
					if (!string.IsNullOrEmpty (lane.max_revision))
						max_revision = int.Parse (lane.max_revision);

					foreach (XmlNode node in svn_log.SelectNodes ("/log/logentry")) {
						revision = node.Attributes ["revision"].Value;

						if (revisions.ContainsKey (revision))
							continue;

						try {
							current_revision = int.Parse (revision);
							if (current_revision < min_revision || current_revision > max_revision)
								continue;
						} catch {
							continue;
						}

						r = new DBRevision ();
						r.revision = node.Attributes ["revision"].Value;
						r.lane_id = lane.id;
						r.author = node.SelectSingleNode ("author").InnerText;
						r.log = node.SelectSingleNode ("msg").InnerText;
						r.date = DateTime.Parse (node.SelectSingleNode ("date").InnerText);
						r.Save (db.Connection);

						update_steps = true;
						Logger.Log ("SVN: Saved revision '{0}' for lane '{1}'", r.revision, lane.lane);
					}

					svn_log = null;
				}

				Logger.Log ("SVN: Updating svn db for lane '{0}'... [Done], update_steps: {1}", lane.lane, update_steps);
			} catch (Exception ex) {
				Logger.Log ("SVN: There was an exception while updating db for lane '{0}': {1}", lane.lane, ex.ToString ());
			}

			return update_steps;
		}
		
		private static string GetSVNLog (DBLane dblane, string repository)
		{
			StringBuilder result = new StringBuilder ();
			string revs = string.Empty;

			try {
				Logger.Log ("SVN: Retrieving svn log for '{0}', repository: '{1}'", dblane.lane, repository);

				if (!string.IsNullOrEmpty (dblane.min_revision)) {
					if (string.IsNullOrEmpty (dblane.max_revision))
						revs = " -r " + dblane.min_revision + ":HEAD";
					else
						revs = " -r " + dblane.min_revision + ":" + dblane.max_revision;
				}

				using (Process p = new Process ()) {
					p.StartInfo.FileName = "svn";
					p.StartInfo.Arguments = "log --xml --non-interactive " + repository + revs;
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.RedirectStandardOutput = true;
					p.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
					{
						result.AppendLine (e.Data);
					};
					p.Start ();
					p.BeginOutputReadLine ();

					// Wait 10 minutes for svn to finish, otherwise abort.
					if (!p.WaitForExit (1000 * 60 * 10)) {
						Logger.Log ("Getting svn log took more than 10 minutes, aborting.");
						try {
							p.Kill ();
							p.WaitForExit (10000); // Give the process 10 more seconds to completely exit.
						} catch (Exception ex) {
							Logger.Log ("Aborting svn log retrieval failed: {0}", ex.ToString ());
						}
					}

					if (p.HasExited && p.ExitCode == 0) {
						Logger.Log ("SVN: Got svn log successfully");
						return result.ToString ();
					} else {
						return null;
					}
				}
			} catch (Exception ex) {
				Logger.Log ("SVN: Exception while trying to get svn log: {0}", ex.ToString ());
				return null;
			}
		}

		public static void StartDiffThread ()
		{
			quit_svn_diff = false;
			diff_thread = new Thread (UpdateSVNDiff);
			diff_thread.Start ();
		}

		public static void StopDiffThread ()
		{
			quit_svn_diff = true;
			diff_thread.Join (TimeSpan.FromMinutes (10));
		}

		#region SVN diff
		private static void UpdateSVNDiff (object dummy)
		{
			try {
				Logger.Log ("SVNDiff: Thread started.");
				using (DB db = new DB (true)) {
					using (DB db_save = new DB (true)) {
						using (IDbCommand cmd = db.Connection.CreateCommand ()) {
							cmd.CommandText = @"
SELECT Revision.*, Lane.repository, Lane.lane
FROM Revision 
INNER JOIN Lane ON Lane.id = Revision.lane_id 
WHERE Revision.diff is NULL OR Revision.diff = '';";
							using (IDataReader reader = cmd.ExecuteReader ()) {
								while (!quit_svn_diff && reader.Read ()) {
									DBRevision revision = new DBRevision (reader);
									string repositories = reader.GetString (reader.GetOrdinal ("repository"));
									string lane = reader.GetString (reader.GetOrdinal ("lane"));
									string diff = null;

									foreach (string repository in repositories.Split (',')) {
										diff = GetSVNDiff (lane, repository, revision.revision);
										if (!string.IsNullOrEmpty (diff))
											break;
									}

									if (string.IsNullOrEmpty (diff))
										diff = "No diff";

									revision.diff = diff;
									revision.Save (db_save);
									Logger.Log ("SVNDiff: Got diff for lane '{0}', revision '{1}'", lane, revision.revision);
								}
							}
						}
					}
				}
				Logger.Log ("SVNDiff: Thread stopping. Done: {0}", !quit_svn_diff);
			} catch (Exception ex) {
				Logger.Log ("SVNDiff: Exception: {0} \n{1}", ex.Message, ex.StackTrace);
			}
		}

		private static string GetSVNDiff (string lane, string repository, string revision)
		{
			StringBuilder result = new StringBuilder ();

			try {
				Logger.Log ("SVNDiff: Getting svn diff for revision '{0}' in lane '{1}'", revision, lane);

				using (Process p = new Process ()) {
					p.StartInfo.FileName = "svn";
					p.StartInfo.Arguments = "diff --change " + revision + " --non-interactive " + repository;
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.RedirectStandardOutput = true;
					p.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
					{
						result.AppendLine (e.Data);
					};
					p.Start ();
					p.BeginOutputReadLine ();

					// Wait 10 minutes for svn to finish, otherwise abort.
					if (!p.WaitForExit (1000 * 60 * 10)) {
						Logger.Log ("SVNDiff: Getting svn diff for revision '{0}' and lane '{1}' took more than 10 minutes, aborting.", revision, lane);
						try {
							p.Kill ();
							p.WaitForExit (10000); // Give the process 10 more seconds to completely exit.ss
						} catch (Exception ex) {
							Logger.Log ("SVNDiff: Aborting svn diff failed: {0}", ex.ToString ());
						}
					}

					if (p.HasExited && p.ExitCode == 0) {
						Logger.Log ("SVNDiff: Got svn diff for revision '{0}' and lane '{1}' successfully.", revision, lane);
						return result.ToString ();
					} else {
						return null;
					}
				}
			} catch (Exception ex) {
				Logger.Log ("SVNDiff: Exception while trying to get svn diff for revision '{0}' and lane '{1}': {2}", revision, lane, ex.ToString ());
				return null;
			} finally {
				result.Length = 0;
			}
		}
		#endregion

	}
}
