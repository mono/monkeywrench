
namespace MonkeyWrench.Web.UI
{
	using System;
	using System.Web;
	using System.Web.UI;
	using System.Web.UI.WebControls;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Newtonsoft.Json;

	using MonkeyWrench.DataClasses;
	using MonkeyWrench.DataClasses.Logic;
	using MonkeyWrench.Web.WebServices;

	public partial class Json : System.Web.UI.Page
	{
		private new Master Master
		{
			get { return base.Master as Master; }
		}

		private WebServiceLogin web_service_login;

		private GetHostStatusResponse hoststatusresponse;

		protected override void OnLoad (EventArgs e)
		{
			if (Request.QueryString ["username"] != null) {
				try {
					if (!Authentication.Login (Request.QueryString ["username"], Request.QueryString ["pw"], Request, Response)) {
						return;
					}
				} catch (Exception) {
					return;
				}
			}

			base.OnLoad (e);

			web_service_login = Utilities.CreateWebServiceLogin (Context.Request);
			hoststatusresponse = Utils.WebService.GetHostStatus (web_service_login);


			var node_information = new Dictionary<string, object> {
				{ "inactiveNodes", GetInactiveNodes(web_service_login, hoststatusresponse) },
				{ "activeNodes", GetActiveNodes(web_service_login, hoststatusresponse) },
				{ "downNodes", GetDownNodes(web_service_login, hoststatusresponse) },
				// { "pendingJobs", "asdf" }
			};

			Response.Write (JsonConvert.SerializeObject (node_information, Formatting.Indented));
		}

		private List<string> GetActiveNodes (WebServiceLogin web_service_login, GetHostStatusResponse hoststatusresponse) {
			var working = new List<string> ();

			for (int i = 0; i < hoststatusresponse.HostStatus.Count; i++) {
				var status = hoststatusresponse.HostStatus [i];
				var idle = string.IsNullOrEmpty (status.lane);

				if (!idle && !NodeIsDead(status)) {
					working.Add (status.host);
				}
			}
			return working;
		}

		private List<string> GetInactiveNodes (WebServiceLogin web_service_login, GetHostStatusResponse hoststatusresponse) {
			var idles = new List<string> ();

			for (int i = 0; i < hoststatusresponse.HostStatus.Count; i++) {
				var status = hoststatusresponse.HostStatus [i];
				var idle = string.IsNullOrEmpty (status.lane);

				if (idle && !NodeIsDead(status)) {
					idles.Add (status.host);
				}
			}
			return idles;
		}

		private List<string> GetDownNodes (WebServiceLogin web_service_login, GetHostStatusResponse hoststatusresponse) {
			var down = new List<string> ();

			for (int i = 0; i < hoststatusresponse.HostStatus.Count; i++) {
				var status = hoststatusresponse.HostStatus [i];

				if (NodeIsDead(status)) {
					down.Add (status.host);
				}
			}
			return down;
		}

		private bool NodeIsDead (DBHostStatusView status) {
			if (status.report_date != null) {
				var silence = DateTime.Now - status.report_date;
				return silence.TotalHours >= 3;
			}
		}

	}
}
