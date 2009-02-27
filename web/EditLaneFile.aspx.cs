/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Builder;

public partial class EditLaneFile : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		int id;

		if (Master.Login == null) {
			Response.Redirect ("index.aspx");
			return;
		}

		if (!IsPostBack) {
			if (int.TryParse (Request ["file_id"], out id)) {
				DBLanefile file = new DBLanefile (Master.DB, id);
				txtEditor.Text = file.contents;

				foreach (DBLane lane in DBLanefile.GetLanesForFile (Master.DB, file)) {
					lstLanes.Items.Add (lane.lane);
				}

			} else {
				txtEditor.Text = "Invalid file id.";
			}
		}
	}

	protected void cmdCancel_Click (object sender, EventArgs e)
	{
		Response.Redirect ("EditLane.aspx?lane_id=" + Request ["lane_id"]);
	}

	protected void cmdSave_Click (object sender, EventArgs e)
	{
		try {
			string file_id = Request ["file_id"];
			int id;
			DBLanefile file;

			if (int.TryParse (file_id, out id)) {
				file = new DBLanefile (Master.DB, id);
				file.contents = txtEditor.Text;
				file.Save (Master.DB);
			}

			Response.Redirect ("EditLane.aspx?lane_id=" + Request ["lane_id"]);
		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}
}
