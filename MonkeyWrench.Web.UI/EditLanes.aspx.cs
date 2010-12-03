/*
 * EditLanes.aspx.cs
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

public partial class EditLanes : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		lblMessage.Text = "";
		lblMessage.Visible = false;

		if (!IsPostBack) {
			string action = Request ["action"];
			int lane_id;

			if (!string.IsNullOrEmpty (action)) {
				switch (action) {
				case "clone":
					if (!int.TryParse (Request ["lane_id"], out lane_id))
						break;
					if (string.IsNullOrEmpty (Request ["lane"]))
						break;
					try {
						int tmp;
						tmp = Master.WebService.CloneLane (Master.WebServiceLogin, lane_id, Request ["lane"], false);
						Response.Redirect ("EditLane.aspx?lane_id=" + tmp.ToString ());
						return;
					} catch (Exception ex) {
						lblMessage.Text = ex.Message;
					}
					break;
				case "remove":
					if (!int.TryParse (Request ["lane_id"], out lane_id))
						break;
					Response.Redirect ("Delete.aspx?lane_id=" + lane_id.ToString ());
					return;
				case "add":
					try {
						Master.WebService.AddLane (Master.WebServiceLogin, Request ["lane"]);
					} catch (Exception ex) {
						lblMessage.Text = ex.Message;
					}
					break;
				default:
					// do nothing
					break;
				}
			}

			GetLanesResponse response = Master.WebService.GetLanes (Master.WebServiceLogin);

			TableHeaderRow header = new TableHeaderRow ();
			TableHeaderCell cell = new TableHeaderCell ();
			TableRow row;
			cell.Text = "Lanes";
			cell.ColumnSpan = Authentication.IsInRole (response, MonkeyWrench.DataClasses.Logic.Roles.Administrator) ? 3 : 2;
			header.Cells.Add (cell);
			tblLanes.Rows.Add (header);
			foreach (DBLane lane in response.Lanes) {
				row = new TableRow ();
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='EditLane.aspx?lane_id={0}'>{1}</a>", lane.id, lane.lane)));
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='EditLanes.aspx?lane_id={0}&amp;action=remove'>Delete</a>", lane.id)));
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript:cloneLane ({0}, \"{1}\");'>Clone</a>", lane.id, lane.lane)));
				tblLanes.Rows.Add (row);
			}
			row = new TableRow ();
			row.Cells.Add (Utils.CreateTableCell ("<input type='text' value='lane' id='txtLane'></input>"));
			row.Cells.Add (Utils.CreateTableCell ("<a href='javascript:addLane ()'>Add</a>"));
			if (Authentication.IsInRole (response, MonkeyWrench.DataClasses.Logic.Roles.Administrator))
				row.Cells.Add (Utils.CreateTableCell ("-"));
			tblLanes.Rows.Add (row);
		}

		if (lblMessage.Text != string.Empty)
			lblMessage.Visible = true;
	}
}
