/*
 * index.aspx.cs
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
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class SelectLanes : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{

		try {
			GetLanesResponse data;

			string lanes_str = null;
			string lane_ids_str = null;

			string [] lanes = null;
			int? [] lane_ids = null;

			HttpCookie cookie;
			cookie = Request.Cookies ["index:lane"];
			if (cookie != null) {
				lanes_str = HttpUtility.UrlDecode (cookie.Value);
			}

			cookie = Request.Cookies ["index:lane_id"];
			if (cookie != null) {
				lane_ids_str = HttpUtility.UrlDecode (cookie.Value);
			}

			if (!string.IsNullOrEmpty (lanes_str))
				lanes = lanes_str.Split (';');
			if (!string.IsNullOrEmpty (lane_ids_str)) {
				string [] tmp = lane_ids_str.Split (';');
				lane_ids = new int? [tmp.Length];
				for (int i = 0; i < tmp.Length; i++) {
					lane_ids [i] = Utils.TryParseInt32 (tmp [i]);
				}
			}

			data = Master.WebService.GetLanes (Master.WebServiceLogin);

			foreach (DBLane lane in data.Lanes) {
				if (lane.parent_lane_id.HasValue)
					continue;

				CheckBox check = new CheckBox ();
				check.ID = "chk" + lane.id.ToString ();
				if (lane_ids != null) {
					for (int i = 0; i < lane_ids.Length; i++) {
						if (lane_ids [i] == lane.id) {
							check.Checked = true;
							break;
						}
					}
				}
				if (!check.Checked && lanes != null) {
					for (int i = 0; i < lanes.Length; i++) {
						if (lanes [i] == lane.lane) {
							check.Checked = true;
							break;
						}
					}
				}
				tblLanes.Rows.AddAt (tblLanes.Rows.Count - 1, Utils.CreateTableRow (Utils.CreateTableCell (lane.lane), Utils.CreateTableCell (check)));
			}

		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}

	protected void cmdOK_OnClick (object sender, EventArgs e)
	{
		List<string> lane_ids_str = new List<string> ();
	
		for (int i = 0; i < tblLanes.Rows.Count; i++) {
			TableRow row = tblLanes.Rows [i];
			CheckBox box;
			TableCell cell;
			
			if (row.Cells.Count == 2 && row.Cells [1].Controls.Count >= 1) {
				cell = row.Cells [1].Controls [0] as TableCell;
				if (cell != null && cell.Controls.Count >= 1) {
					box = cell.Controls [0] as CheckBox;
			
					if (box != null && box.Checked) {
						lane_ids_str.Add (box.ID.Replace ("chk", ""));
					}
				}
			}
		}

		string lane_ids = string.Join (";", lane_ids_str.ToArray ());
		
		Response.Redirect ("index.aspx?lane_id=" + lane_ids, false);
	}
}
