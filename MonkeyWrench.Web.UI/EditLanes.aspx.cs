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
						Response.Redirect ("EditLane.aspx?lane_id=" + tmp.ToString (), false);
						return;
					} catch (Exception ex) {
						lblMessage.Text = Utils.FormatException (ex);
					}
					break;
				case "remove":
					if (!int.TryParse (Request ["lane_id"], out lane_id))
						break;
					Response.Redirect ("Delete.aspx?action=delete-lane&lane_id=" + lane_id.ToString (), false);
					return;
				case "add":
					try {
						Master.WebService.AddLane (Master.WebServiceLogin, Request ["lane"]);
						Response.Redirect ("EditLanes.aspx", false);
						return;
					} catch (Exception ex) {
						lblMessage.Text = Utils.FormatException (ex);
					}
					break;
				default:
					// do nothing
					break;
				}
			}
		} else if (!string.IsNullOrEmpty (Request ["txtLane"])) {
			try {
				Master.WebService.AddLane (Master.WebServiceLogin, Request ["txtlane"]);
				Response.Redirect ("EditLanes.aspx", false);
				return;
			} catch (Exception ex) {
				lblMessage.Text = Utils.FormatException (ex);
			}
		}

		GetLanesResponse response = Master.WebService.GetLanes (Master.WebServiceLogin);

		TableRow row;
		foreach (DBLane lane in response.Lanes) {
			row = new TableRow ();
			row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='EditLane.aspx?lane_id={0}'>{1}</a>", lane.id, lane.lane)));
			row.Cells.Add (Utils.CreateTableCell (
				string.Format ("<a href='EditLanes.aspx?lane_id={0}&amp;action=remove'>Delete</a> ", lane.id) +
				string.Format ("<a href='javascript:cloneLane ({0}, \"{1}\");'>Clone</a>", lane.id, lane.lane)));
			tblLanes.Rows.Add (row);
		}
	}
}
