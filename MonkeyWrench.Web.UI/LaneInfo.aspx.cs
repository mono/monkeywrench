
namespace MonkeyWrench.Web.UI
{
	using System;
	using System.Collections.Generic;
	using System.Web;
	using System.Web.UI;
	using System.Linq;
	using Newtonsoft.Json;

	using MonkeyWrench.DataClasses;
	using MonkeyWrench.DataClasses.Logic;
	using MonkeyWrench.Web.WebServices;

	public partial class LaneInfo : System.Web.UI.Page
	{
		private new Master Master
		{
			get { return base.Master as Master; } 
		}

		private WebServiceLogin webServiceLogin;
		private GetLanesResponse lanesResponse;

		string BranchFromRevision (string revision) {
			return String.IsNullOrEmpty (revision) ? "remotes/origin/master" : revision;
		}

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e); 
			webServiceLogin = Authentication.CreateLogin (Request);

			lanesResponse = Utils.WebService.GetLanes (webServiceLogin);

			var reposInformation = lanesResponse.Lanes.ToDictionary (
				l => l.lane, 
				l => new { 
					branch     = BranchFromRevision (l.max_revision),
					repository = l.repository
				});

			Response.AppendHeader("Access-Control-Allow-Origin", "*");
			Response.Write (JsonConvert.SerializeObject (reposInformation, Formatting.Indented));
		}
	}
}

