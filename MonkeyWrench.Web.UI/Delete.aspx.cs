using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class Delete : System.Web.UI.Page
{
	int? lane_id;
	int? host_id;

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		try {
			if (!IsPostBack && string.IsNullOrEmpty (txtReturnTo.Value) && Request.UrlReferrer != null)
				txtReturnTo.Value = Request.UrlReferrer.ToString ();

			if (!Utils.IsInRole (MonkeyWrench.DataClasses.Logic.Roles.Administrator))
				Redirect ();

			int tmp;
			if (int.TryParse (Request ["lane_id"], out tmp)) {
				lane_id = tmp;
				FindLaneResponse lane = Master.WebService.FindLane (Master.WebServiceLogin, lane_id, null);

				lblMessage.Text = string.Format ("Are you sure you want to delete the lane '{0}' (ID: {1})?", lane.lane.lane, lane.lane.id);
			} else if (int.TryParse (Request ["host_id"], out tmp)) {
				host_id = tmp;
				FindHostResponse host = Master.WebService.FindHost (Master.WebServiceLogin, host_id, null);

				lblMessage.Text = string.Format ("Are you sure you want to delete the host '{0}' (ID: {1})?", host.Host.host, host.Host.id);
			} else {
				lblMessage.Text = "Nothing to confirm. Click Cancel to go back.";
				cmdConfirm.Enabled = false;
			}
		} catch {
			throw;
		}
	}

	protected void cmdCancel_Click (object sender, EventArgs e)
	{
		Redirect ();
	}

	protected void cmdConfirm_Click (object sender, EventArgs e)
	{
		try {
			if (lane_id.HasValue)
				Master.WebService.DeleteLane (Master.WebServiceLogin, lane_id.Value);
			else if (host_id.HasValue)
				Master.WebService.DeleteHost (Master.WebServiceLogin, host_id.Value);

			Redirect ();
		} catch {
			throw;
		}
	}

	private void Redirect ()
	{
		Response.Redirect (string.IsNullOrEmpty (txtReturnTo.Value) ? "index.aspx" : txtReturnTo.Value);
	}
}
