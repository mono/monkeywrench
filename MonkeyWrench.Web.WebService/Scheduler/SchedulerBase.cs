/*
 * SchedulerBase.cs
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
using System.Text;
using System.Threading;
using System.Xml;
using log4net;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Scheduler
{
	public abstract class SchedulerBase
	{
		private static readonly ILog log = LogManager.GetLogger (typeof (SchedulerBase));
		private bool force_full_update;

		// a tuple, each path and the revisions the path was modified
		private List<string> paths;
		private List<string> min_revisions;

		protected SchedulerBase (bool ForceFullUpdate)
		{
			force_full_update = ForceFullUpdate;
		}

		public virtual void Clear ()
		{
			min_revisions = null;
			paths = null;
		}

		/// <summary>
		/// GIT / SVN / etc
		/// </summary>
		public abstract string Type { get; }

		/// <summary>
		/// If the scheduler is to ignore commit reports and update everything
		/// </summary>
		public bool ForceFullUpdate
		{
			get { return force_full_update; }
		}

		protected void AddChangedPath (string path, string revision)
		{
			int existing = -1;

			if (paths == null) {
				paths = new List<string> ();
				min_revisions = new List<string> ();
			} else {
				existing = paths.IndexOf (path);
			}

			if (existing == -1) {
				paths.Add (path);
				min_revisions.Add (revision);
			} else {
				if (CompareRevisions (string.Empty, min_revisions [existing], revision) > 0) {
					min_revisions [existing] = revision;
					log.InfoFormat ("Added changeset for {1} with path: {0}", path, revision);
				}
			}
		}

		protected virtual void AddChangeSet (XmlDocument doc)
		{
			XmlNode rev = doc.SelectSingleNode ("/monkeywrench/changeset");
			string revision = rev.Attributes ["revision"].Value;
			string root = rev.Attributes ["root"].Value;
			string sc = rev.Attributes ["sourcecontrol"].Value;

			if (!string.Equals (sc, Type, StringComparison.OrdinalIgnoreCase))
				return;

			foreach (XmlNode node in doc.SelectNodes ("/monkeywrench/changeset/directories/directory")) {
				log.InfoFormat ("Checking changeset directory: '{0}'", node.InnerText);
				AddChangedPath (root + "/" + node.InnerText, revision);
			}
		}

		public void AddChangeSets (List<XmlDocument> docs)
		{
			if (docs == null || docs.Count == 0)
				return;

			foreach (XmlDocument doc in docs) {
				AddChangeSet (doc);
			}
		}

		protected abstract bool UpdateRevisionsInDBInternal (DB db, DBLane lane, string repository, Dictionary<string, DBRevision> revisions, List<DBHost> hosts, List<DBHostLane> hostlanes, string min_revision, string max_revision);

		protected abstract int CompareRevisions (string repository, string a, string b);

		/*protected void Log (string msg, params object [] args)
		{
			Logger.Log (Type + ": " + msg, args);
		}

		protected void Log (int verbosity, string msg, params object [] args)
		{
			Logger.Log (verbosity, Type + ": " + msg, args);
		}*/

		/*
		/// <summary>
		/// Checks if a lane has any reported commits.
		/// If so, min_revision will be the first reported commit (otherwise min_revision will be null).
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="min_revision"></param>
		/// <returns></returns>
		private bool HasCommits (DBLane lane, out string min_revision)
		{
			bool found = false;

			min_revision = null;

			if (Configuration.ForceFullUpdate || ForceFullUpdate)
				return true;

			if (paths == null || paths.Count == 0)
				return false;

			foreach (string repo in lane.repository.Split (';')) {
				Uri uri = new Uri (repo);
				string dir = uri.Host + uri.LocalPath;
				for (int i = 0; i < paths.Count; i++) {
					if (paths [i].StartsWith (dir)) {
						if (!found) { 
							min_revision = min_revisions [i];
							found = true;
						} else if (CompareRevisions (string.Empty, min_revisions [i], min_revision) < 0) {
							min_revision = min_revisions [i];
							Logger.Log ("SVN: A commit report shows that {0} (lane: {2}) was changed in r{1}", paths [i], min_revision, repo);
						}
					}
				}
			}

			return min_revision != null;
		}
		 * */

		string [] splitWithMiniumElements (string toSplit, int min)
		{
			string [] result;

			if (string.IsNullOrEmpty (toSplit))
				return new string [min];

			result = toSplit.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			if (result.Length < min) {
				// Not as many elements as requested. Add more and fill new empty
				// entries with the last element of the provided entries.
				string [] tmp = new string [min];
				Array.Copy (result, tmp, min);
				for (int i = result.Length; i < tmp.Length; i++)
					tmp [i] = result [result.Length - 1];
				result = tmp;
			}

			return result;
		}


		/// <summary>
		/// This method must return true if a revision was added to the database.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="lane"></param>
		/// <param name="hosts"></param>
		/// <param name="hostlanes"></param>
		/// <returns></returns>
		public bool UpdateRevisionsInDB (DB db, DBLane lane, List<DBHost> hosts, List<DBHostLane> hostlanes)
		{
			Dictionary<string, DBRevision> revisions;
			bool update_steps = false;
			string [] min_revisions;
			string [] max_revisions;
			string [] repositories;
			bool skip_lane;

			log.InfoFormat ("Updating '{0}', ForceFullUpdate: {1}", lane.lane, ForceFullUpdate);

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
					log.InfoFormat ("Skipping lane {0}, not enabled or configured on any host.", lane.lane);
					return false;
				}

				/*
				// check for commit reports
				if (!HasCommits (lane)) {
					Log ("Skipping lane {0}, no commits.", lane.lane);
					return false;
				}
				 **/

				revisions = db.GetDBRevisions (lane.id, false);

				repositories = lane.repository.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				min_revisions = splitWithMiniumElements (lane.min_revision, repositories.Length);
				max_revisions = splitWithMiniumElements (lane.max_revision, repositories.Length);

				for (int i = 0; i < repositories.Length; i++) {
					UpdateRevisionsInDBInternal (db, lane, repositories [i], revisions, hosts, hostlanes, min_revisions [i], max_revisions [i]);
				}

				log.InfoFormat ("Updating db for lane '{0}'... [Done], update_steps: {1}", lane.lane, update_steps);
			} catch (Exception ex) {
				log.ErrorFormat ("There was an exception while updating db for lane '{0}': {1}", lane.lane, ex);
			}

			return update_steps;
		}
	}
}
