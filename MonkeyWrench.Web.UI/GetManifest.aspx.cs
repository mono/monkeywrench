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

namespace MonkeyWrench.Web.UI
{

	public partial class GetManifest : System.Web.UI.Page
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

			var lane = Request.QueryString ["lane"];
			var revision = Request.QueryString ["revision"];
			var baseURL = Request.QueryString ["url"] ?? "http://storage.bos.internalx.com";
			var storagePref = Request.QueryString ["prefer"];
			if (!string.IsNullOrEmpty(storagePref) && (storagePref.ToLower () == "azure")) {
				baseURL = "https://bosstoragemirror.blob.core.windows.net";
			}

			var step =  10;
			var limit =  200;

			revision = string.IsNullOrEmpty(revision) ? getLatestRevision (webServiceLogin, lane, step, 0, limit) : revision;

			if (revision != "") {
				handleGetManifest (baseURL, lane, revision, storagePref);
			} else {
				Response.Write ("No Valid Revisions");
			}
		}

		void handleGetManifest (string baseURL, string laneName, string revision, string storagePref) {
			Response.AppendHeader ("Access-Control-Allow-Origin", "*");
			Response.AppendHeader ("Content-Type", "text/plain");

			HttpWebResponse response = makeHttpRequest (getManifestUrl (baseURL, laneName, revision));
			if (response.StatusCode != HttpStatusCode.OK) {
				// Default to NAS
				if (storagePref != "NAS") {
					response = makeHttpRequest (getManifestUrl ("http://storage.bos.internalx.com", laneName, revision));
					if (response.StatusCode != HttpStatusCode.OK) {
						Response.Write ("Can't find manifest");
						return;
					}
				}
				Response.Write ("Can't find manifest");
				return;
			}
			using (var reader = new StreamReader (response.GetResponseStream ())) {
				Response.Write (reader.ReadToEnd ());
			}
		}

		HttpWebResponse makeHttpRequest (string url) {
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create (url);
			return (HttpWebResponse)request.GetResponse ();
		}

		string getManifestUrl (string host, string laneName, string revision) {
			return String.Format ("{0}/{1}/{2}/{3}/manifest", host, laneName, revision.Substring (0, 2), revision);
		}

		string getLatestRevision (WebServiceLogin login, string laneName, int step, int offset, int limit){
			var lane = Utils.LocalWebService.FindLane (login, null, laneName).lane;
			var revisions = Utils.LocalWebService.GetRevisions (login, null, laneName, step, offset).Revisions;
			var revisionWorks = revisions.Select (r => Utils.LocalWebService.GetRevisionWorkForLane (login, lane.id, r.id, -1).RevisionWork).ToList ();
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
			                             Utils.LocalWebService.GetViewLaneData (login, r.lane_id, "", r.host_id, "", r.revision_id, "").WorkViews.Any (w => 
			                                                                                                                      w.command.Contains ("upload-to-storage") && w.State == DBState.Success));
		}
	}
}

