/*
 * SchedulerSVN.cs
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
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Scheduler
{
	class SVNUpdater : SchedulerBase
	{
		private static bool quit_svn_diff = false;
		private static Thread diff_thread;
		
		public SVNUpdater (bool ForceFullUpdate)
			: base (ForceFullUpdate)
		{
		}

		public override string Type
		{
			get { return "SVN"; }
		}

		protected override int CompareRevisions (string repository, string a, string b)
		{
			int aa = int.Parse (a);
			int bb = int.Parse (b);
			return aa == bb ? 0 : ((aa < bb) ? -1 : 1);
		}

		protected override bool UpdateRevisionsInDBInternal (DB db, DBLane lane, string repository,Dictionary<string, DBRevision> revisions, List<DBHost> hosts, List<DBHostLane> hostlanes, string min_revision)
		{
			string revision;
			XmlDocument svn_log;
			bool update_steps = false;
			DBRevision r;
			int min_revision_int = string.IsNullOrEmpty (min_revision) ? 0 : int.Parse (min_revision);
			int max_revision_int = int.MaxValue;
			int current_revision;
			string log;
			XmlNode n;
			XmlAttribute attrib;

			Log ("Updating '{0}'", lane.lane);

			if (min_revision_int == 0 && !string.IsNullOrEmpty (lane.min_revision))
				min_revision_int = int.Parse (lane.min_revision);
			if (!string.IsNullOrEmpty (lane.max_revision))
				max_revision_int = int.Parse (lane.max_revision);

			log = GetSVNLog (lane, repository, min_revision_int, max_revision_int);

			if (string.IsNullOrEmpty (log)) {
				Log ("Didn't get a svn log for '{0}'", repository);
				return false;
			}

			svn_log = new XmlDocument ();
			svn_log.PreserveWhitespace = true;
			svn_log.Load (new StringReader (log));

			foreach (XmlNode node in svn_log.SelectNodes ("/log/logentry")) {
				revision = node.Attributes ["revision"].Value;

				if (revisions.ContainsKey (revision))
					continue;

				try {
					current_revision = int.Parse (revision);
					if (current_revision < min_revision_int || current_revision > max_revision_int)
						continue;
				} catch {
					continue;
				}

				r = new DBRevision ();
				attrib = node.Attributes ["revision"];
				if (attrib == null || string.IsNullOrEmpty (attrib.Value)) {
					Log ("An entry without revision in {0}, skipping entry", repository);
					continue;
				}
				r.revision = attrib.Value;
				r.lane_id = lane.id;

				n = node.SelectSingleNode ("author");
				if (n != null) {
					r.author = n.InnerText;
				} else {
					Log ("No author specified in r{0} in {1}", r.revision, repository);
					r.author = "?";
				}
				n = node.SelectSingleNode ("date");
				if (n != null) {
					DateTime dt;
					if (DateTime.TryParse (n.InnerText, out dt)) {
						r.date = dt;
					} else {
						Log ("Could not parse the date '{0}' in r{1} in {2}", n.InnerText, r.revision, repository);
						r.date = DateTime.MinValue;
					}
				} else {
					Log ("No date specified in r{0} in {1}", r.revision, repository);
					r.date = DateTime.MinValue;
				}
				n = node.SelectSingleNode ("msg");
				if (n != null) {
					r.log_file_id = db.UploadString (n.InnerText, ".log", false).id;
				} else {
					Log ("No msg specified in r{0} in {1}", r.revision, repository);
					r.log_file_id = null;
				}

				r.Save (db);

				update_steps = true;
				Log ("Saved revision '{0}' for lane '{1}'", r.revision, lane.lane);
			}

			return update_steps;
		}

		private string GetSVNLog (DBLane dblane, string repository, int min_revision, int max_revision)
		{
			StringBuilder result = new StringBuilder ();
			string revs = string.Empty;

			try {
				Log ("Retrieving svn log for '{0}', repository: '{1}', min_revision: {2} max_revision: {3}", dblane.lane, repository, min_revision, max_revision);

				if (min_revision > 0) {
					revs = " -r " + min_revision.ToString ();
					if (max_revision < int.MaxValue) {
						revs += ":" + max_revision.ToString ();
					} else {
						revs += ":HEAD";
					}
				}

				using (Process p = new Process ()) {
					p.StartInfo.FileName = "svn";
					p.StartInfo.Arguments = "log --stop-on-copy --xml --non-interactive " + repository + revs;
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
						Log ("Getting log took more than 10 minutes, aborting.");
						try {
							p.Kill ();
							p.WaitForExit (10000); // Give the process 10 more seconds to completely exit.
						} catch (Exception ex) {
							Log ("Aborting svn log retrieval failed: {0}", ex.ToString ());
						}
					}

					if (p.HasExited && p.ExitCode == 0) {
						Log ("Got svn log successfully");
						return result.ToString ();
					} else {
						return null;
					}
				}
			} catch (Exception ex) {
				Log ("Exception while trying to get svn log: {0}", ex.ToString ());
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
						using (IDbCommand cmd = db.CreateCommand ()) {
							cmd.CommandText = @"
SELECT Revision.*, Lane.repository, Lane.lane
FROM Revision 
INNER JOIN Lane ON Lane.id = Revision.lane_id 
WHERE (Revision.diff IS NULL OR Revision.diff = '') AND Revision.diff_file_id IS NULL AND Lane.source_control = 'svn';";
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

									revision.diff_file_id = db_save.UploadString (diff, ".log", false).id;
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
