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

	public abstract partial class GetData : System.Web.UI.Page
	{
		private new Master Master {
			get { return base.Master as Master; }
		}

		protected abstract string Filename {
			get;
		}

		private WebServiceLogin webServiceLogin;

		static readonly string NAS_ROOT = "http://storage.bos.internalx.com";
		static readonly string AZURE_ROOT_1 = "https://bosstoragemirror.blob.core.windows.net/wrench";
		static readonly string AZURE_ROOT_2 = "https://bosstoragemirror.blob.core.windows.net";

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			webServiceLogin = Authentication.CreateLogin(Request);

			var lane = Request.QueryString["lane"];
			var revision = Request.QueryString["revision"];
			var storagePref = Request.QueryString["prefer"];
			var preferAzure = !string.IsNullOrEmpty(storagePref) && (storagePref.ToLower() == "azure");

			var baseUrls = preferAzure ? new string[] { AZURE_ROOT_1, AZURE_ROOT_2, NAS_ROOT } : new string[] { NAS_ROOT };

			var step = 10;
			var limit = 200;

			revision = string.IsNullOrEmpty(revision) ? getLatestRevision(webServiceLogin, lane, step, 0, limit) : revision;

			if (revision != "") {
				writeOutput(baseUrls, lane, revision, storagePref);
			} else {
				throw new HttpException(404, "No Valid Revisions");
			}
		}

		void writeOutput(string[] baseUrls, string laneName, string revision, string storagePref)
		{
			Response.AppendHeader("Access-Control-Allow-Origin", "*");
			Response.AppendHeader("Content-Type", "application/json");

			foreach (var url in baseUrls) {
				HttpWebResponse response = makeHttpRequest(getUrl(url, laneName, revision));
				if (response.StatusCode == HttpStatusCode.OK) {
					using (var reader = new StreamReader(response.GetResponseStream())) {
						Response.Write(reader.ReadToEnd());
					}
					return;
				}
			}

			throw new HttpException(404, String.Format("Can't find {0}", Filename));
		}

		HttpWebResponse makeHttpRequest(string url)
		{
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
			try {
				// If we get an exception, we want to return the failed response back up the chain.
				return (HttpWebResponse)request.GetResponse();
			}
			catch (WebException exception) {
				var response = exception.Response as HttpWebResponse;
				if (response == null)
					throw;
				return response;
			}
		}

		string getUrl(string host, string laneName, string revision)
		{
			return String.Format("{0}/{1}/{2}/{3}/{4}", host, laneName, revision.Substring(0, 2), revision, Filename);
		}

		string getLatestRevision(WebServiceLogin login, string laneName, int step, int offset, int limit)
		{
			var lane = Utils.LocalWebService.FindLane(login, null, laneName).lane;
			var revisions = Utils.LocalWebService.GetRevisions(login, null, laneName, step, offset).Revisions;
			var revisionWorks = revisions.Select(r => Utils.LocalWebService.GetRevisionWorkForLane(login, lane.id, r.id, -1).RevisionWork).ToList();
			var validRevisions = revisionWorks.Find(wl => isValidRevision(login, wl));

			if (validRevisions != null) {
				return getRevisionName(revisions, validRevisions.First().revision_id);
			} else if (offset < limit) {
				return getLatestRevision(login, laneName, step, offset + step, limit);
			} else {
				return "";
			}
		}

		string getRevisionName(List<DBRevision> revisions, int revision_id)
		{
			return revisions.Find(r => r.id == revision_id).revision;
		}

		bool isValid(DBWorkView2 w)
		{
			bool hasUploadStep = w.command.Contains("upload-to-storage");
			bool isSuccessful  = (w.State == DBState.Success);
			bool hasContent    = (w.summary != null && w.summary.Contains(Filename));

			return new bool [] { hasUploadStep, isSuccessful, hasContent }.All(i => i == true);
		}

		bool isValidRevision(WebServiceLogin login, List<DBRevisionWork> revisionWorkList)
		{
			Func<DBRevisionWork, bool> validRevisions = r =>
				Utils.LocalWebService.GetViewLaneData(login, r.lane_id, "", r.host_id, "", r.revision_id, "").WorkViews.Any(isValid);

			return revisionWorkList.Any(validRevisions);
		}
	}
}

