
namespace MonkeyWrench.Web.UI
{
	using System;
	using System.Web;
	using System.Web.UI;
	using System.Linq;
	using System.Collections.Generic;

	using MonkeyWrench.DataClasses;
	using MonkeyWrench.DataClasses.Logic;
	using MonkeyWrench.Web.WebServices;
	
	public partial class GetLatest : System.Web.UI.Page
	{

		private new Master Master
		{
			get { return base.Master as Master; } 
		}

		private WebServiceLogin webServiceLogin;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e); 
			webServiceLogin = Authentication.CreateLogin (Request);

			var laneName = Request.QueryString ["laneName"];
			var baseURL = Request.QueryString ["url"] ?? "http://storage.bos.internalx.com";

			var revision = getLatestRevision (webServiceLogin, laneName);
			var URL = String.Format ("{0}/{1}/{2}/{3}/manifest", baseURL, laneName, revision.Substring(0,2), revision);


			Response.AppendHeader ("Access-Control-Allow-Origin", "*");
			Response.Redirect (URL);
		}
			


		string getLatestRevision (WebServiceLogin login, string laneName){
			var lane = Utils.WebService.FindLane (login, null, laneName).lane;
			var revisions = Utils.WebService.GetRevisions (login, null, laneName, 50, 0).Revisions;
			var revisionWorks = revisions.Select (r => Utils.WebService.GetRevisionWorkForLane (login, lane.id, r.id, -1).RevisionWork).ToList ();
		
			return getRevisionName (revisions, revisionWorks.Find (wl => validRevision (login, wl)).First ().revision_id);
		}

		string getRevisionName (List<DBRevision> revisions, int revision_id) {
			return revisions.Find (r => r.id == revision_id).revision;
		}

		bool validRevision (WebServiceLogin login, List<DBRevisionWork> revisionWorkList) {
			return revisionWorkList.Any (r => 
				Utils.WebService.GetViewLaneData (login, r.lane_id, "", r.host_id, "", r.revision_id, "").WorkViews.Any (w => 
					w.command.Equals ("upload-to-storage") && w.State == DBState.Success));
		}
	}
}

