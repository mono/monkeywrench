

using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using log4net;

using MonkeyWrench.WebServices;
using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;

namespace MonkeyWrench.WebServices {
	public class GitHubNotification : NotificationBase {
		/* NOTE: this uses curl because, for some reason, connecting to GitHub via HttpRequest
		 * in MonkeyWrench fails with a "ConnectFailure" WebException (it works fine from a simple
		 * CLI app and a simple ASP server).
		 * The previous version using HttpRequest can be found in the git history.
		 */
		
		static readonly ILog log = LogManager.GetLogger (typeof (GitHubNotification));
		private const string HOST = "https://api.github.com";

		private readonly string username, token;
		private static readonly Regex GITHUB_RE = new Regex(@"([a-zA-Z0-9\-]+)/([a-zA-Z0-9\-]+)(?:\.git)?$");

		private static readonly Dictionary<DBState, string> STATE_TO_GITHUB = new Dictionary<DBState, string> {
			{DBState.Aborted, "error"},
			{DBState.DependencyNotFulfilled, "pending"},
			{DBState.Executing, "pending"},
			{DBState.Failed, "failure"},
			{DBState.Ignore, "success"},
			{DBState.Issues, "failure"},
			{DBState.NotDone, "pending"},
			{DBState.NoWorkYet, "pending"},
			{DBState.Paused, "pending"},
			{DBState.Skipped, "error"},
			{DBState.Success, "success"},
			{DBState.Timeout, "error"}
		};

		public GitHubNotification (DBNotification notification) : base (notification) {
			using (var db = new DB ())
			using (var cmd = db.CreateCommand ()) {
				cmd.CommandText = @"SELECT username, token FROM githubidentity WHERE id = @id;";
				DB.CreateParameter (cmd, "id", notification.githubidentity_id);
				using (var reader = cmd.ExecuteReader ()) {
					reader.Read ();
					username = reader.GetString (0);
					token = reader.GetString (1);
				}
			}
		}

		public override void Stop () {
		}

		/// Struct containing needed info about a RevisionWork that can
		/// be returned in one query.
		private struct RevisionWorkInfo {
			public string hash;
			public string repoURL;
			public string laneName;
			public int laneID;
			public string command;
		}

		/**
		 * Fetches info needed from the database.
		 */
		private RevisionWorkInfo getWorkInfo(DB db, int revisionWorkID, int workID) {
			using (var cmd = db.CreateCommand ()) {
				cmd.CommandText = @"
					SELECT revision.revision, lane.id, lane.repository, lane.lane
					FROM revisionwork
					INNER JOIN revision ON revision.id = revisionwork.revision_id
					INNER JOIN lane ON lane.id = revision.lane_id
					WHERE revisionwork.id = @rwID;

					SELECT command.command
					FROM work
					INNER JOIN command ON command.id = work.command_id
					WHERE work.id = @wID
				";
				DB.CreateParameter (cmd, "rwID", revisionWorkID);
				DB.CreateParameter (cmd, "wID", workID);
				using (var reader = cmd.ExecuteReader ()) {
					RevisionWorkInfo info;

					reader.Read ();

					info.hash = reader.GetString (0);
					info.laneID = reader.GetInt32 (1);
					info.repoURL = reader.GetString (2);
					info.laneName = reader.GetString (3);

					reader.NextResult ();
					reader.Read ();

					info.command = reader.GetString (0);

					return info;
				}
			}
		}

		/**
		 * Given a URL in the form of `git@github.com:user/repo.git`,
		 * returns the username and repository name.
		 * 
		 * Throws an error if the URL doesn't match
		 */
		private Tuple<string,string> gitRepoToGitHub(string repoUrl) {
			var m = GITHUB_RE.Match (repoUrl);
			if (!m.Success)
				throw new Exception ("Invalid GitHub repository: " + repoUrl);
			return Tuple.Create (m.Groups [1].Value, m.Groups [2].Value);
		}

		/**
		 * Creates an HttpWebRequest to the GitHub statues endpoint, setting up the url and headers.
		 */
		private void sendNotification(string repoURL, string hash, JObject payload) {
			var gitHubRepo = gitRepoToGitHub (repoURL);

			// Generate URL for API call
			var apiUrl = string.Format ("{0}/repos/{1}/{2}/statuses/{3}",
				HOST,
				Uri.EscapeDataString (gitHubRepo.Item1),
				Uri.EscapeDataString (gitHubRepo.Item2),
				Uri.EscapeDataString (hash)
			);

			using (var proc = new Process ()) {
				proc.StartInfo.FileName = "curl";
				proc.StartInfo.Arguments = string.Format (
					@"-q --fail --location --silent -X POST -d @- -A MonkeyWrench -H 'Content-Type: application/json; charset=utf-8' " +
					@"-u {0}:{1} {2}",
					username, token, apiUrl
				);
				proc.StartInfo.UseShellExecute = false;
				proc.StartInfo.RedirectStandardInput = true;
				proc.StartInfo.RedirectStandardOutput = true;
				proc.StartInfo.RedirectStandardError = true;
				proc.StartInfo.StandardOutputEncoding = Encoding.UTF8;
				proc.OutputDataReceived += (sender, e) => {}; // Don't care about the response
				proc.ErrorDataReceived += (sender, e) => {
					if (!string.IsNullOrEmpty (e.Data))
						log.WarnFormat ("curl stderr: {0}", e.Data);
				};

				proc.Start ();
				proc.BeginOutputReadLine ();
				proc.BeginErrorReadLine ();

				using (var strm = new JsonTextWriter (proc.StandardInput))
					payload.WriteTo (strm);
				
				proc.WaitForExit ();
				if (proc.ExitCode != 0)
					log.ErrorFormat ("Failed to send GitHub notification. Code: {0}", proc.ExitCode);
			}
		}

		/**
		 * Builds a JObject containing the status data.
		 */
		private JObject createPayload(int laneID, int hostID, int revisionID, string description, string state) {
			JObject statusObj = new JObject ();
			statusObj ["context"] = String.Format("wrench/{0}/{1}", laneID, hostID);
			statusObj ["target_url"] = String.Format ("{0}/ViewLane.aspx?lane_id={1}&host_id={2}&revision_id={3}",
				Configuration.GetWebSiteUrl (),
				laneID,
				hostID,
				revisionID
			);
			statusObj ["description"] = description;
			statusObj ["state"] = state;
			return statusObj;
		}

		/**
		 * Notification callback for work updates.
		 */
		public override void Notify (DBWork work, DBRevisionWork revision_work) {
			// Get info
			RevisionWorkInfo info;
			using (var db = new DB ())
				info = getWorkInfo (db, revision_work.id, work.id);
			
			// Create object to send
			var statusObj = createPayload(revision_work.lane_id, revision_work.host_id, revision_work.revision_id,
				String.Format("Lane: {0}, Status: {1}, Step: {2}, StepStatus: {3}",
					info.laneName,
					revision_work.State,
					info.command,
					work.State
				),
				STATE_TO_GITHUB [revision_work.State]
			);
			sendNotification (info.repoURL, info.hash, statusObj);
		}

		// Don't care about related people and the autogenerated message.
		protected override void Notify (DBWork work, DBRevisionWork revision_work, List<DBPerson> people, string message) {
			throw new NotImplementedException ();
		}

		public override void NotifyGeneric (GenericNotificationInfo info)
		{
			string laneName, hostName, hash, repoURL;

			// Fetch info from database
			using (var db = new DB ())
			using (var cmd = db.CreateCommand (@"
				SELECT lane.lane, host.host, revision.revision, lane.repository
				FROM revisionwork
				INNER JOIN lane ON lane.id = revisionwork.lane_id
				INNER JOIN host ON host.id = revisionwork.host_id
				INNER JOIN revision ON revision.id = revisionwork.revision_id
				WHERE revisionwork.lane_id = @laneid AND revisionwork.host_id = @hostid AND revisionwork.revision_id = @revisionid
			")) {
				DB.CreateParameter (cmd, "laneid", info.laneID);
				DB.CreateParameter (cmd, "hostid", info.hostID);
				DB.CreateParameter (cmd, "revisionid", info.revisionID);

				using (var reader = cmd.ExecuteReader ()) {
					if (!reader.Read ()) {
						log.WarnFormat ("GitHub Notifier: RevisionWork for lane {0} host {1} revision {2} no longer exists.", info.laneID, info.hostID, info.revisionID);
						return;
					}

					laneName = reader.GetString (0);
					hostName = reader.GetString (1);
					hash = reader.GetString (2);
					repoURL = reader.GetString (3);
				}
			}

			string message = String.Format ("Lane: {0}, Host: {1}: {2}", laneName, hostName, info.message);
			var statusObj = createPayload (info.laneID, info.hostID, info.revisionID, message, STATE_TO_GITHUB[info.state]);
			sendNotification (repoURL, hash, statusObj);
		}
	}
}

