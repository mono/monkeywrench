
namespace MonkeyWrench.Web.UI
{
	using System;
	using System.IO;
	using System.Net;
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
			var storagePref = Request.QueryString ["prefer"];
			if (storagePref && storagePref.ToLower () == "azure") {
				baseURL = "https://bosstoragemirror.blob.core.windows.net";
			}
			var updateRequest = false;
			var step =  10;
			var limit =  200;

			var revision = getLatestRevision (webServiceLogin, laneName, step, 0, limit);

			Action handleGetLatest = () => {
//				var homePage = Page.ResolveUrl ("~/index.aspx");
				var URL = revision != "" ? String.Format ("{0}/{1}/{2}/{3}/manifest", baseURL, laneName, revision.Substring (0, 2), revision) : null;
//				Response.AppendHeader ("Access-Control-Allow-Origin", "*");
//				Response.Redirect (URL);
				if (URL) {
					HttpWebResponse response = HttpWebRequest.Create (URL).GetResponse ();
					if (response.StatusCode != 200) {
						// Default to NAS
						if (!URL.Contains ("storage.bos")) {
							URL = String.Format ("{0}/{1}/{2}/{3}/manifest", "http://storage.bos.internalx.com", laneName, revision.Substring (0, 2), revision);
							response = HttpWebRequest.Create (URL).GetResponse ();
							if (response.StatusCode != 200) {
								Response.Write ("Can't find manifest");
								return;
							}	
						}
						Response.Write ("Can't find manifest");
						return; 
					}
					StreamReader reader = new StreamReader (response.GetResponseStream ());
					string manifest = reader.ReadToEnd ();
					reader.Close ();
					Response.Write (manifest);
				} else {
					Response.Write ("No valid revisions");
				}
			};

//			Action handleUpdate = () => {
//				Response.Write("");
//			};
//
//			if (updateRequest) {
//				handleUpdate ();
//			} else {
//				handleGetLatest ();
//			}
		}

		string getLatestRevision (WebServiceLogin login, string laneName, int step, int offset, int limit){
			var lane = Utils.WebService.FindLane (login, null, laneName).lane;
			var revisions = Utils.WebService.GetRevisions (login, null, laneName, step, offset).Revisions;
			var revisionWorks = revisions.Select (r => Utils.WebService.GetRevisionWorkForLane (login, lane.id, r.id, -1).RevisionWork).ToList ();
			var validRevisions = revisionWorks.Find (wl => validRevision (login, wl));

			if (validRevisions != null) {
				return getRevisionName (revisions, validRevisions.First ().revision_id);
			} else if (offset < limit) {
				return getLatestRevision (login, laneName, step, offset + step, limit);
			} else {
				return "";
			}
		}

		string getRevisionName (List<DBRevision> revisions, int revision_id) {
			return revisions.Find (r => r.id == revision_id).revision;
		}

		bool validRevision (WebServiceLogin login, List<DBRevisionWork> revisionWorkList) {
			return revisionWorkList.Any (r => 
				Utils.WebService.GetViewLaneData (login, r.lane_id, "", r.host_id, "", r.revision_id, "").WorkViews.Any (w => 
					w.command.Contains ("upload-to-storage") && w.State == DBState.Success));
		}
	}
}

