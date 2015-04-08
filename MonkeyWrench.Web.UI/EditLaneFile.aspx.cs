/*
 * EditLaneFile.aspx.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class EditLaneFile : System.Web.UI.Page
{

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		int id;

		if (!IsPostBack) {

			if (int.TryParse (Request ["file_id"], out id)) {
				GetLaneFileForEditResponse response = Utils.LocalWebService.GetLaneFileForEdit (Master.WebServiceLogin, id);
				DBLanefile file = response.Lanefile;
				txtEditor.Text = file.contents;

				if (file.original_id != null)
					cmdSave.Visible = false; // Don't allow editing previous versions of a file

				foreach (DBLane lane in response.Lanes) {
					lstLanes.Items.Add (lane.lane);
				}

			} else {
				txtEditor.Text = "Invalid file id.";
			}
		}
	}

	protected void cmdCancel_Click (object sender, EventArgs e)
	{
		Response.Redirect ("EditLane.aspx?lane_id=" + Request ["lane_id"], false);
	}

	protected void cmdSave_Click (object sender, EventArgs e)
	{
		int id;
		if (int.TryParse (Request ["file_id"], out id)) {
			GetLaneFileForEditResponse response = Utils.LocalWebService.GetLaneFileForEdit (Master.WebServiceLogin, id);
			response.Lanefile.contents = txtEditor.Text;
			Utils.LocalWebService.EditLaneFile (Master.WebServiceLogin, response.Lanefile);

			if (Request.UrlReferrer != null && Request.UrlReferrer.LocalPath.Contains ("ViewLaneFileHistory.aspx")) {
				Response.Redirect ("ViewLaneFileHistory.aspx?id=" + Request ["file_id"], false);
			} else {
				Response.Redirect ("EditLane.aspx?lane_id=" + Request ["lane_id"], false);
			}
		} else {
			Response.Redirect ("EditLane.aspx?lane_id=" + Request ["lane_id"], false);
		}
	}
}
