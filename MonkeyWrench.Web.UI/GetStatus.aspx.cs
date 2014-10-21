using System;
using System.Web;
using System.Web.UI;
using MonkeyWrench.DataClasses.Logic;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Web.UI
{
	// API - GetStatus.aspx
	//
	// This cannot handle builds on multiple hosts as this stands, the following combinations are supported:
	//
	// lanename=<name>, commit=<commit-sha> => build status
	//
	// lane_id=<id>, revision_id=<id>       => build status
	// (note that these are similar to parameters from ViewLane.aspx which may be used, but host_id will be ignored)


	public partial class GetStatus : System.Web.UI.Page
	{
		private new Master Master {
			get { return base.Master as Master; }
		}

		private WebServiceLogin login;

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad (e);
			login = Authentication.CreateLogin (Request);
			Response.AppendHeader ("Access-Control-Allow-Origin", "*");
			Dictionary<String, Object> buildStatusResponse = null;

			if (!string.IsNullOrEmpty (Request ["lane_id"])) {
				var laneId = Utils.TryParseInt32 (Request ["lane_id"]);
				var revisionId = Utils.TryParseInt32 (Request ["revision_id"]);
				if (laneId.HasValue && revisionId.HasValue)
					buildStatusResponse = FetchBuildStatus (laneId.Value, revisionId.Value);
			} else {
				var laneName = Request ["lane_name"];
				var commit = Request ["commit"];
				if (string.IsNullOrEmpty (laneName) || string.IsNullOrEmpty (commit))
					throw new HttpException(400, "Either lanename+commit or lane_id+revision_id must be provided to resolve build.");
				buildStatusResponse = FetchBuildStatus (laneName, commit);
			}
			Response.Write (JsonConvert.SerializeObject (buildStatusResponse));
		}

		// Handles requests:
		// https://<wrenchhost>/GetStatus.aspx?lane_id=9939&host_id=883&revision_id=484848
		protected Dictionary<String, Object> FetchBuildStatus(int laneId, int revisionId)
		{
			var work = Utils.WebService.GetRevisionWorkForLane (login, laneId, revisionId, 0).RevisionWork.First ();
			if (work == null)
				throw new HttpException(404, "Build not found. Invalid revision_id or lane_id");
			var host = Utils.WebService.FindHost (login, work.host_id, "").Host;

			return BuildStatusFrom (laneId, revisionId, work, host);
		}

		// Handles requests:
		// https://<wrenchhost>/GetStatus.aspx?lane_name=some-lane-name-master&commit=aff123
		protected Dictionary<String, Object> FetchBuildStatus(string laneName, string commit)
		{
			var lane = Utils.WebService.FindLane (login, null, laneName).lane;
			if (lane == null)
				throw new HttpException(404, string.Format ("Lane could not be found with name {0}", laneName));
			var revision = Utils.WebService.FindRevisionForLane (login, 0, commit, lane.id, "").Revision;
			if (revision == null)
				throw new HttpException(404,
					string.Format ("Revision with commit hash of {0} was not found for lane {1}", commit, laneName));
			var work = Utils.WebService.GetRevisionWorkForLane (login, lane.id, revision.id, 0).RevisionWork.First ();
			var host = Utils.WebService.FindHost (login, work.host_id, "").Host;

			return BuildStatusFrom (lane, revision, work, host);
		}

		private string BuildLink(int laneId, int revId, int hostId)
		{
			return string.Format ("{0}/ViewLane.aspx?lane_id={1}&host_id={2}&revision_id={3}",
				Configuration.WebSiteUrl, laneId, hostId, revId);
		}

		private string BuildFileLink(int fileId)
		{
			return string.Format ("{0}/GetFile.aspx?id={1}", Configuration.WebSiteUrl, fileId);
		}

		private Dictionary<String, Object> BuildStatusFrom(int laneId, int revId, DBRevisionWork work, DBHost host)
		{
			var d = new Dictionary<String, Object>();

			var buildView = Utils.WebService.GetViewLaneData (login, laneId, "", host.id, "", revId, "");
			var steps = new List<Dictionary<String, String>>();
			for (int s = 0; s < buildView.WorkViews.Count; s++) {
				steps.Add (BuildStepStatus (buildView.WorkViews [s], buildView.WorkFileViews [s]));
			}
			d.Add ("status", work.State.ToString ());
			d.Add ("steps", steps);
			d.Add ("start_time", work.endtime.ToString ());
			d.Add ("url", BuildLink (laneId, revId, host.id));
			d.Add ("build_bot", host.host);
			return d;
		}

		private Dictionary<String, String> BuildStepStatus(DBWorkView2 step, List<DBWorkFileView> files)
		{
			var d = new Dictionary<String, String>();
			var logFile = files.Find (f => f.filename == step.command + ".log");

			d.Add ("step", step.command);
			d.Add ("status", step.State.ToString ());
			if (logFile != null) {
				d.Add ("log", BuildFileLink (logFile.id));
			}

			return d;
		}
	}
}

