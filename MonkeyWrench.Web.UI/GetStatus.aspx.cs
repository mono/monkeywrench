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
			var start = DateTime.Now;
			base.OnLoad (e);
			login = Authentication.CreateLogin (Request);
			Response.AppendHeader ("Access-Control-Allow-Origin", "*");
			Dictionary<String, Object> buildStatusResponse = null;
			try {
				if (!string.IsNullOrEmpty (Request ["lane_id"])) {
					var laneId = Utils.TryParseInt32 (Request ["lane_id"]);
					var revisionId = Utils.TryParseInt32 (Request ["revision_id"]);
					if (laneId.HasValue && revisionId.HasValue)
						buildStatusResponse = FetchBuildStatus (laneId.Value, revisionId.Value);
				} else {
					var laneName = Request ["lane_name"];
					var commit = Request ["commit"];
					if (string.IsNullOrEmpty (laneName) || string.IsNullOrEmpty (commit))
						ThrowJsonError (400, "Either lane_name+commit or lane_id+revision_id must be provided to resolve build.");
					buildStatusResponse = FetchBuildStatus (laneName, commit);
				}
				buildStatusResponse.Add ("generation_time", (DateTime.Now - start).TotalMilliseconds);
				Response.Write (JsonConvert.SerializeObject (buildStatusResponse));
			} catch (System.Web.Services.Protocols.SoapException) {
				Response.StatusCode = 403;
				Response.Write (JsonConvert.SerializeObject (new Dictionary<String, String> { {
						"error",
						"You are not authorized to use this resource."
					}
				}));
			} catch (HttpException exp) {
				Response.StatusCode = exp.GetHttpCode ();
				Response.Write (exp.Message);
			} finally {
				Response.Flush ();
				Response.Close ();
			}
		}

		// Handles requests:
		// https://<wrenchhost>/GetStatus.aspx?lane_name=some-lane-name-master&commit=aff123
		protected Dictionary<String, Object> FetchBuildStatus(string laneName, string commit)
		{
			var lane = Utils.WebService.FindLane (login, null, laneName).lane;
			if (lane == null)
				ThrowJsonError (404, string.Format ("Lane could not be found with lane_name='{0}'", laneName));
			var revision = Utils.WebService.FindRevisionForLane (login, null, commit, lane.id, "").Revision;
			if (revision == null)
				ThrowJsonError (404,
					string.Format ("Revision with commit='{0}' was not found for lane_name='{1}'", commit, laneName));
			return FetchBuildStatus (lane.id, revision.id);
		}

		// Handles requests:
		// https://<wrenchhost>/GetStatus.aspx?lane_id=9939&host_id=883&revision_id=484848
		protected Dictionary<String, Object> FetchBuildStatus(int laneId, int revisionId)
		{
			var workResponse = Utils.WebService.GetRevisionWorkForLane (login, laneId, revisionId, 0).RevisionWork;
			if (workResponse.Count == 0)
				ThrowJsonError (404, "Build not found. Invalid revision_id or lane_id");
			var work = workResponse.First ();
			var host = Utils.WebService.FindHost (login, work.host_id, "").Host;

			return BuildStatusFrom (laneId, revisionId, work, host);
		}

		private string ThrowJsonError(int code, string msg)
		{
			throw new HttpException(code, "{\"error\": \"" + msg + "\"}");
		}

		private string BuildLink(int laneId, int revId, int hostId)
		{
			return string.Format ("{0}ViewLane.aspx?lane_id={1}&host_id={2}&revision_id={3}",
				Configuration.WebSiteUrl, laneId, hostId, revId);
		}

		private string BuildFileLink(int fileId)
		{
			return string.Format ("{0}GetFile.aspx?id={1}", Configuration.WebSiteUrl, fileId);
		}

		private Dictionary<String, Object> BuildStatusFrom(int laneId, int revId, DBRevisionWork work, DBHost host)
		{
			if (host == null)
				throw new HttpException(404, "Build has not been assigned yet, cannot generate status.");
			var buildView = Utils.WebService.GetViewLaneData (login, laneId, "", host.id, "", revId, "");
			var steps = new List<Dictionary<String, Object>>();
			for (int s = 0; s < buildView.WorkViews.Count; s++) {
				steps.Add (BuildStepStatus (s, buildView.WorkViews [s], buildView.WorkFileViews [s]));
			}
			return new Dictionary<String, Object> {
				{ "build_host", buildView.WorkHost.host },
				{ "build_host_id", buildView.WorkHost.id },
				{ "commit", buildView.Revision.revision },
				{ "completed", buildView.RevisionWork.completed },
				{ "end_time", work.endtime },
				{ "host", host.host },
				{ "host_id", host.id },
				{ "lane_id", laneId },
				{ "lane_name", buildView.Lane.lane },
				{ "revision_id", revId },
				{ "repository", buildView.Lane.repository },
				{ "start_time", buildView.WorkViews [0].starttime },
				{ "status", work.State.ToString ().ToLowerInvariant () },
				{ "steps", steps },
				{ "url", BuildLink (laneId, revId, host.id) }
			};
		}

		private Dictionary<String, Object> BuildStepStatus(int idx, DBWorkView2 step, List<DBWorkFileView> files)
		{
			var d = new Dictionary<String, Object>();
			var fs = BuildStepFiles (files);

			d.Add ("duration", MonkeyWrench.Utilities.GetDurationFromWorkView (step).TotalSeconds);
			if (fs.Count != 0) {
				d.Add ("files", fs);
			}
			d.Add ("order", idx);
			d.Add ("step", step.command);
			d.Add ("status", step.State.ToString ().ToLowerInvariant ());

			return d;
		}

		private Dictionary<String, String> BuildStepFiles(List<DBWorkFileView> files)
		{
			var d = new Dictionary<String, String>();
			files.ForEach ((DBWorkFileView f) => {
				d.Add (f.filename, BuildFileLink (f.id));
			});
			return d;
		}
	}
}

