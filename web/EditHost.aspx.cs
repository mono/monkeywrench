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

public partial class EditHost : System.Web.UI.Page
{
	DBHost host;

	private DBHost LoadHost (DB db)
	{
		string strhost = Request ["host"];
		string idhost = Request ["host_id"];
		int id;

		if (!string.IsNullOrEmpty (strhost))
			return db.LookupHost (HttpUtility.UrlDecode (strhost));

		if (!string.IsNullOrEmpty (idhost) && int.TryParse (idhost, out id))
			return new DBHost (db, id);

		return null;
	}

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		TableRow row;
		string html;
		string page;
		List<DBHostLaneView> views;
		List<string> current_lanes = new List<string> ();
		List<DBLane> all_lanes;

		try {
			DB db = Master.DB;

			if (Master.Login == null){
				Response.Redirect ("index.aspx", false);
				return;
			}

			string disable = Request ["disablelane"];
			string enable = Request ["enablelane"];
			string remove = Request ["removelane"];
			string add = Request ["addlane"];
			int id;
			bool redirect = false;

			host = LoadHost (db);
			if (host == null) {
				Response.Redirect ("EditHosts.aspx");
				return;
			}

			if (!IsPostBack) {
				if (!string.IsNullOrEmpty (disable) && int.TryParse (disable, out id)) {
					host.EnableLane (db, id, false);
					redirect = true;
				}

				if (!string.IsNullOrEmpty (enable) && int.TryParse (enable, out id)) {
					host.EnableLane (db, id, true);
					redirect = true;
				}

				if (!string.IsNullOrEmpty (remove) && int.TryParse (remove, out id)) {
					host.RemoveLane (db, id);
					redirect = true;
				}
				if (!string.IsNullOrEmpty (add) && int.TryParse (add, out id)) {
					host.AddLane (db, id);
					redirect = true;
				}
				if (redirect) {
					Response.Redirect ("EditHost.aspx?host_id=" + host.id.ToString (), false);
					return;
				}

				txtID.Text = host.id.ToString ();
				txtArchitecture.Text = host.architecture;
				txtDescription.Text = host.description;
				txtHost.Text = host.host;
				chkEnabled.Checked = host.enabled;
				cmbQueueManagement.SelectedIndex = host.queuemanagement;
			}

			views = host.GetLanes (db);
			foreach (DBHostLaneView view in views) {
				string ed = view.enabled ? "enabled" : "disabled";
				row = new TableRow ();
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='EditLane.aspx?lane_id={0}'>{1}</a>", view.lane_id, view.lane), view.enabled ? "enabled" : "disabled"));
				page = "EditHost.aspx?host_id=" + host.id.ToString ();
				html = "<a href='" + page + "&amp;removelane=" + view.lane_id.ToString () + "'>Remove</a> ";
				row.Cells.Add (Utils.CreateTableCell (html, ed));
				html = "<a href='" + page + "&amp;" + (view.enabled ? "disable" : "enable") + "lane=" + view.lane_id.ToString () + "'>" + (view.enabled ? "Disable" : "Enable") + "</a>";
				row.Cells.Add (Utils.CreateTableCell (html, ed));
				tblLanes.Rows.Add (row);
				current_lanes.Add (view.lane);
			}

			all_lanes = db.GetAllLanes ();
			if (all_lanes.Count != current_lanes.Count) {
				row = new TableRow ();
				html = "<select id='addhostlane'>";
				foreach (DBLane lane in all_lanes) {
					if (!current_lanes.Contains (lane.lane))
						html += "<option value='" + lane.id + "'>" + lane.lane + "</option>";
				}
				html += "</select>";
				row.Cells.Add (Utils.CreateTableCell (html));
				row.Cells.Add (Utils.CreateTableCell ("<a href='javascript:addLane()'>Add</a>"));
				row.Cells.Add (Utils.CreateTableCell ("-"));
				tblLanes.Rows.Add (row);
			}

		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}
	protected void cmdSave_Click (object sender, EventArgs e)
	{
		DB db = Master.DB;

		if (Master.Login == null) {
			Response.Redirect ("index.aspx");
			return;
		}
	
		if (host == null)
			host = LoadHost (db);
		host.host = txtHost.Text;
		host.architecture = txtArchitecture.Text;
		host.description = txtDescription.Text;
		host.queuemanagement = cmbQueueManagement.SelectedIndex;
		host.enabled = chkEnabled.Checked;
		host.Save (db);
	}
}
