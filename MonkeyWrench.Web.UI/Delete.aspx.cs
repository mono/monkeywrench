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
	string action;
	int lane_id;
	int host_id;

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		try {
			if (!IsPostBack && string.IsNullOrEmpty (txtReturnTo.Value) && Request.UrlReferrer != null)
				txtReturnTo.Value = Request.UrlReferrer.ToString ();

			action = Request ["action"];
			if (string.IsNullOrEmpty (action)) {
				lblMessage.Text = "Nothing to ask?!?";
				return;
			}

			switch (action) {
			case "delete-lane": {
				if (!int.TryParse (Request ["lane_id"], out lane_id)) {
					lblMessage.Text = "You need to specify a lane to delete.";
					return;
				}

				FindLaneResponse lane = Master.WebService.FindLane (Master.WebServiceLogin, lane_id, null);

				lblMessage.Text = string.Format ("Are you sure you want to delete the lane '{0}' (ID: {1})?", lane.lane.lane, lane.lane.id);
				cmdConfirm.Enabled = true;
				break;
			}
			case "delete-host": {
				if (!int.TryParse (Request ["host_id"], out host_id)) {
					lblMessage.Text = "You need to specify a host to delete.";
					return;
				}

				FindHostResponse host = Master.WebService.FindHost (Master.WebServiceLogin, host_id, null);

				lblMessage.Text = string.Format ("Are you sure you want to delete the host '{0}' (ID: {1})?", host.Host.host, host.Host.id);
				cmdConfirm.Enabled = true;
				break;
			}
			case "delete-all-work-for-host": {
				if (!int.TryParse (Request ["host_id"], out host_id)) {
					lblMessage.Text = "You need to specify a host whose work should be deleted.";
					return;
				}
				FindHostResponse host = Master.WebService.FindHost (Master.WebServiceLogin, host_id, null);

				lblMessage.Text = string.Format ("Are you sure you want to delete all the work for the host '{0}' (ID: {1})?", host.Host.host, host.Host.id);
				cmdConfirm.Enabled = true;
				break;
			}
			case "delete-all-work-for-lane": {
				if (!int.TryParse (Request ["lane_id"], out lane_id)) {
					lblMessage.Text = "You need to specify a lane whose work should be deleted.";
					return;
				}
				FindLaneResponse lane = Master.WebService.FindLane (Master.WebServiceLogin, lane_id, null);

				lblMessage.Text = string.Format ("Are you sure you want to delete all the work for the lane '{0}' (ID: {1})?", lane.lane.lane, lane.lane.id);
				cmdConfirm.Enabled = true;
				break;
				}
			case "delete-all-revisions-for-lane": {
				if (!int.TryParse (Request ["lane_id"], out lane_id)) {
					lblMessage.Text = "You need to specify a lane whose revisions should be deleted.";
					return;
				}
				FindLaneResponse lane = Master.WebService.FindLane (Master.WebServiceLogin, lane_id, null);

				lblMessage.Text = string.Format ("Are you sure you want to delete all the revisions for the lane '{0}' (ID: {1})?", lane.lane.lane, lane.lane.id);
				cmdConfirm.Enabled = true;
				break;
			}
			case "clear-all-work-for-host": {
				if (!int.TryParse (Request ["host_id"], out host_id)) {
					lblMessage.Text = "You need to specify a host whose work should be cleared.";
					return;
				}
				FindHostResponse host = Master.WebService.FindHost (Master.WebServiceLogin, host_id, null);

				lblMessage.Text = string.Format ("Are you sure you want to clear all the work for the host '{0}' (ID: {1})?", host.Host.host, host.Host.id);
				cmdConfirm.Enabled = true;
				break;
			}
			case "clear-all-work-for-lane": {
				if (!int.TryParse (Request ["lane_id"], out lane_id)) {
					lblMessage.Text = "You need to specify a lane whose work should be cleared.";
					return;
				}
				FindLaneResponse lane = Master.WebService.FindLane (Master.WebServiceLogin, lane_id, null);

				lblMessage.Text = string.Format ("Are you sure you want to clear all the work for the lane '{0}' (ID: {1})?", lane.lane.lane, lane.lane.id);
				cmdConfirm.Enabled = true;
				break;
			}
			default:
				lblMessage.Text = "Nothing to confirm. Click Cancel to go back.";
				break;
			}
		} catch (Exception ex) {
			cmdConfirm.Enabled = false;
			cmdConfirm.Visible = false;
			lblMessage.Text = Utils.FormatException (ex, true);
		}
	}

	protected void cmdCancel_Click (object sender, EventArgs e)
	{
		Redirect ();
	}

	protected void cmdConfirm_Click (object sender, EventArgs e)
	{
		WebServiceResponse rsp = null;

		try {
			switch (action) {
			case "delete-lane":
				Master.WebService.DeleteLane (Master.WebServiceLogin, lane_id);
				break;
			case "delete-host":
				Master.WebService.DeleteHost (Master.WebServiceLogin, host_id);
				break;
			case "delete-all-work-for-host":
				rsp = Master.WebService.DeleteAllWorkForHost (Master.WebServiceLogin, host_id);
				break;
			case "delete-all-work-for-lane":
				rsp = Master.WebService.DeleteAllWorkForLane (Master.WebServiceLogin, lane_id);
				break;
			case "delete-all-revisions-for-lane":
				rsp = Master.WebService.DeleteAllRevisionsForLane (Master.WebServiceLogin, lane_id);
				break;
			case "clear-all-work-for-host":
				rsp = Master.WebService.ClearAllWorkForHost (Master.WebServiceLogin, host_id);
				break;
			case "clear-all-work-for-lane":
				rsp = Master.WebService.ClearAllWorkForLane (Master.WebServiceLogin, lane_id);
				break;
			default:
				lblMessage.Text = "Invalid action";
				return;
			}

			if (rsp != null && rsp.Exception != null) {
				cmdConfirm.Enabled = false;
				lblMessage.Text = Utils.FormatException (rsp.Exception.Message);
			} else {
				Redirect ();
			}
		} catch (Exception ex) {
			cmdConfirm.Enabled = false;
			cmdConfirm.Visible = false;
			lblMessage.Text = Utils.FormatException (ex, true);
		}
	}

	private void Redirect ()
	{
		Response.Redirect (string.IsNullOrEmpty (txtReturnTo.Value) ? "index.aspx" : txtReturnTo.Value, false);
	}
}
