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
            base.OnLoad(e);
            login = Authentication.CreateLogin(Request);
            Response.AppendHeader("Access-Control-Allow-Origin", "*");
            Dictionary<String, String> buildStatusResponse = null;

            if (Request.Params.Get("lane_id") != null) {
                var laneId = Utils.TryParseInt32(Request.Params.Get("lane_id"));
                var revisionId = Utils.TryParseInt32(Request.Params.Get("revision_id"));
                if (laneId.HasValue && revisionId.HasValue)
                    buildStatusResponse = FetchBuildByIds(laneId.Value, revisionId.Value);
            } else {
                var laneName = Request.Params.Get("lane_name");
                var commit = Request.Params.Get("commit");
                if (laneName != null && commit != null)
                    buildStatusResponse = FetchBuildByName(laneName, commit);
            }
            if (buildStatusResponse != null)
                Response.Write(JsonConvert.SerializeObject(buildStatusResponse));
            else
                Response.Write("{\"error\": \"Build could not be resolved. Check your arguments.\"}");
           
        }

        // Handles requests:
        // https://<wrenchhost>/GetStatus.aspx?lane_id=9939&host_id=883&revision_id=484848
        protected Dictionary<String, String> FetchBuildByIds(int laneId, int revisionId)
        {
            var lane = Utils.WebService.GetLane(login, laneId).lane;
            var revision = Utils.WebService.FindRevision(login, revisionId, "").Revision;
            var work = Utils.WebService.GetRevisionWorkForLane(login, lane.id, revision.id, 0).RevisionWork.First();
            var host = Utils.WebService.FindHost(login, work.host_id, "").Host;

            return BuildStatusFrom(lane, revision, work, host);
        }

        // Handles requests:
        // https://<wrenchhost>/GetStatus.aspx?lane_name=some-lane-name-master&commit=aff123
        protected Dictionary<String, String> FetchBuildByName(string laneName, string commit)
        {
            var lane = Utils.WebService.FindLane(login, null, laneName).lane;
            var revision = Utils.WebService.FindRevisionForLane(login, 0, commit, lane.id, "").Revision;
            var work = Utils.WebService.GetRevisionWorkForLane(login, lane.id, revision.id, 0).RevisionWork.First();
            var host = Utils.WebService.FindHost(login, work.host_id, "").Host;

            return BuildStatusFrom(lane, revision, work, host);
        }

        private string BuildLink(int lane_id, int rev_id, int host_id)
        {
            return string.Format("{0}/ViewLane.aspx?lane_id={1}&host_id={2}&revision_id={3}",
                Configuration.WebSiteUrl, lane_id, host_id, rev_id);
        }

        private Dictionary<String, String> BuildStatusFrom(DBLane lane, DBRevision rev, DBRevisionWork work, DBHost host)
        {
            var d = new Dictionary<String, String>();
            d.Add("status", work.State.ToString());
            d.Add("start_time", work.endtime.ToString());
            d.Add("url", BuildLink(lane.id, rev.id, host.id));
            d.Add("build_bot", host.host);
            return d;
        }
    }
}

