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
using System.Data;
using System.Data.Common;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Builder;

public partial class ViewLaneFileHistory : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		int id;
		DBLanefile file;

		if (Master.Login == null) {
			Response.Redirect ("index.aspx");
			return;
		}

		if (!IsPostBack) {
			if (int.TryParse (Request ["id"], out id)) {
				using (IDbCommand cmd = Master.DB.Connection.CreateCommand ()) {
					cmd.CommandText = "SELECT * FROM LaneFile WHERE original_id = @lane_id;";
					DB.CreateParameter (cmd, "lane_id", id);
					tblFiles.Rows.Add (Utils.CreateTableHeaderRow ("Date changed", "Actions"));
					using (IDataReader reader = cmd.ExecuteReader ()) {
						while (reader.Read ()) {
							file = new DBLanefile (reader);
							tblFiles.Rows.Add (Utils.CreateTableRow (file.changed_date.Value.ToString ("yyyy/MM/dd HH:mm:ss"),
									string.Format ("<a href='EditLaneFile.aspx?file_id={0}'>View</a>", file.id)));
						}
					}
				}
			}
		}
	}
}
