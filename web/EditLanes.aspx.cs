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

		if (Master.Login == null) {
			Response.Redirect ("index.aspx");
			return;
		}

		if (!IsPostBack) {
			string action = Request ["action"];
			int lane_id;

			DB db = Master.DB;

			if (!string.IsNullOrEmpty (action)) {
				switch (action) {
				case "clone":
					if (!int.TryParse (Request ["lane_id"], out lane_id))
						break;
					if (string.IsNullOrEmpty (Request ["lane"]))
						break;
					try {
						DBLane tmp = db.CloneLane (lane_id, Request ["lane"]);
						Response.Redirect ("EditLane.aspx?lane_id=" + tmp.id.ToString ());
						return;
					} catch (Exception ex) {
						lblMessage.Text = ex.Message;
					}
					break;
				case "remove":
					if (!int.TryParse (Request ["lane_id"], out lane_id))
						break;
					DBLane.Delete (db, lane_id);
					Response.Redirect ("EditLanes.aspx");
					return;
				case "add":
					string lane = Request ["lane"];
					bool valid;
					if (string.IsNullOrEmpty (lane)) {
						valid = false;
						lblMessage.Text = "You have to provide a name for the lane.";
					} else {
						valid = true;
						for (int i = 0; i < lane.Length; i++) {
							if (char.IsLetterOrDigit (lane [i])) {
								continue;
							} else if (lane [i] == '-' || lane [i] == '_' || lane [i] == '.') {
								continue;
							} else {
								lblMessage.Text = string.Format ("The character '{0}' isn't valid.", lane [i]);
								valid = false;
								break;
							}
						}
						if (valid && db.LookupLane (lane, false) != null) {
							lblMessage.Text = string.Format ("The lane '{0}' already exists.", lane);
							valid = false;
						}
					}
					if (valid) {
						DBLane dblane = new DBLane ();
						dblane.lane = lane;
						dblane.source_control = "svn";
						dblane.Save (db);
						Response.Redirect (string.Format ("EditLane.aspx?lane_id={0}", dblane.id));
						return;
					}
					break;
				default:
					// do nothing
					break;
				}
			}

			TableHeaderRow header = new TableHeaderRow ();
			TableHeaderCell cell = new TableHeaderCell ();
			TableRow row;
			cell.Text = "Lanes";
			cell.ColumnSpan = Master.Login != null ? 3 : 2;
			header.Cells.Add (cell);
			tblLanes.Rows.Add (header);
			foreach (DBLane lane in db.GetAllLanes ()) {
				row = new TableRow ();
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='EditLane.aspx?lane_id={0}'>{1}</a>", lane.id, lane.lane)));
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='EditLanes.aspx?lane_id={0}&amp;action=remove'>Delete</a>", lane.id)));
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript:cloneLane ({0}, \"{1}\");'>Clone</a>", lane.id, lane.lane)));
				tblLanes.Rows.Add (row);
			}
			row = new TableRow ();
			row.Cells.Add (Utils.CreateTableCell ("<input type='text' value='lane' id='txtLane'></input>"));
			row.Cells.Add (Utils.CreateTableCell ("<a href='javascript:addLane ()'>Add</a>"));
			if (Master.Login != null)
				row.Cells.Add (Utils.CreateTableCell ("-"));
			tblLanes.Rows.Add (row);
		}

		if (lblMessage.Text != string.Empty)
			lblMessage.Visible = true;
	}
}
