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

			var lane = Utils.LocalWebService.FindLaneWithDependencies (Master.WebServiceLogin, lane_id, null);
			var text = new System.Text.StringBuilder ();

			text.AppendFormat ("Are you sure you want to delete the lane '{0}' (ID: {1}) Count: {2}?<br/>", lane.lane.lane, lane.lane.id, lane.dependencies == null ? "N/A" : lane.dependencies.Count.ToString ());
			if (lane.dependencies != null && lane.dependencies.Count > 0) {
				text.AppendFormat ("<br/>There are {0} other lane(s) depending on this lane:<br/>", lane.dependencies.Count);
				foreach (var dl in lane.dependencies) {
					text.AppendFormat ("<a href=EditLane.aspx?lane_id={0}>{1}</a><br/>", dl.id, dl.lane);
				}
				text.AppendFormat ("<br/>These dependencies will also be removed.<br/>");
			}
			lblMessage.Text = text.ToString ();
			cmdConfirm.Enabled = true;
			break;
		}
		case "delete-host": {
			if (!int.TryParse (Request ["host_id"], out host_id)) {
				lblMessage.Text = "You need to specify a host to delete.";
				return;
			}

			FindHostResponse host = Utils.LocalWebService.FindHost (Master.WebServiceLogin, host_id, null);

			lblMessage.Text = string.Format ("Are you sure you want to delete the host '{0}' (ID: {1})?", host.Host.host, host.Host.id);
			cmdConfirm.Enabled = true;
			break;
		}
		case "delete-all-work-for-host": {
			if (!int.TryParse (Request ["host_id"], out host_id)) {
				lblMessage.Text = "You need to specify a host whose work should be deleted.";
				return;
			}
			FindHostResponse host = Utils.LocalWebService.FindHost (Master.WebServiceLogin, host_id, null);

			lblMessage.Text = string.Format ("Are you sure you want to delete all the work for the host '{0}' (ID: {1})?", host.Host.host, host.Host.id);
			cmdConfirm.Enabled = true;
			break;
		}
		case "delete-all-work-for-lane": {
			if (!int.TryParse (Request ["lane_id"], out lane_id)) {
				lblMessage.Text = "You need to specify a lane whose work should be deleted.";
				return;
			}
			FindLaneResponse lane = Utils.LocalWebService.FindLane (Master.WebServiceLogin, lane_id, null);

			lblMessage.Text = string.Format ("Are you sure you want to delete all the work for the lane '{0}' (ID: {1})?", lane.lane.lane, lane.lane.id);
			cmdConfirm.Enabled = true;
			break;
			}
		case "delete-all-revisions-for-lane": {
			if (!int.TryParse (Request ["lane_id"], out lane_id)) {
				lblMessage.Text = "You need to specify a lane whose revisions should be deleted.";
				return;
			}
			FindLaneResponse lane = Utils.LocalWebService.FindLane (Master.WebServiceLogin, lane_id, null);

			lblMessage.Text = string.Format ("Are you sure you want to delete all the revisions for the lane '{0}' (ID: {1})?", lane.lane.lane, lane.lane.id);
			cmdConfirm.Enabled = true;
			break;
		}
		case "clear-all-work-for-host": {
			if (!int.TryParse (Request ["host_id"], out host_id)) {
				lblMessage.Text = "You need to specify a host whose work should be cleared.";
				return;
			}
			FindHostResponse host = Utils.LocalWebService.FindHost (Master.WebServiceLogin, host_id, null);

			lblMessage.Text = string.Format ("Are you sure you want to clear all the work for the host '{0}' (ID: {1})?", host.Host.host, host.Host.id);
			cmdConfirm.Enabled = true;
			break;
		}
		case "clear-all-work-for-lane": {
			if (!int.TryParse (Request ["lane_id"], out lane_id)) {
				lblMessage.Text = "You need to specify a lane whose work should be cleared.";
				return;
			}
			FindLaneResponse lane = Utils.LocalWebService.FindLane (Master.WebServiceLogin, lane_id, null);

			lblMessage.Text = string.Format ("Are you sure you want to clear all the work for the lane '{0}' (ID: {1})?", lane.lane.lane, lane.lane.id);
			cmdConfirm.Enabled = true;
			break;
		}
		default:
			lblMessage.Text = "Nothing to confirm. Click Cancel to go back.";
			break;
		}
	}

	protected void cmdCancel_Click (object sender, EventArgs e)
	{
		Redirect ();
	}

	protected void cmdConfirm_Click (object sender, EventArgs e)
	{
		WebServiceResponse rsp = null;
		switch (action) {
		case "delete-lane":
			Utils.LocalWebService.DeleteLane (Master.WebServiceLogin, lane_id);
			break;
		case "delete-host":
			Utils.LocalWebService.DeleteHost (Master.WebServiceLogin, host_id);
			break;
		case "delete-all-work-for-host":
			rsp = Utils.LocalWebService.DeleteAllWorkForHost (Master.WebServiceLogin, host_id);
			break;
		case "delete-all-work-for-lane":
			rsp = Utils.LocalWebService.DeleteAllWorkForLane (Master.WebServiceLogin, lane_id);
			break;
		case "delete-all-revisions-for-lane":
			rsp = Utils.LocalWebService.DeleteAllRevisionsForLane (Master.WebServiceLogin, lane_id);
			break;
		case "clear-all-work-for-host":
			rsp = Utils.LocalWebService.ClearAllWorkForHost (Master.WebServiceLogin, host_id);
			break;
		case "clear-all-work-for-lane":
			rsp = Utils.LocalWebService.ClearAllWorkForLane (Master.WebServiceLogin, lane_id);
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
	}

	private void Redirect ()
	{
		Response.Redirect (string.IsNullOrEmpty (txtReturnTo.Value) ? "index.aspx" : txtReturnTo.Value, false);
	}
}
