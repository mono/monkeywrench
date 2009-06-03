/*
 * ViewLaneFileHistory.aspx.cs
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

public partial class ViewLaneFileHistory : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		int id;

		if (!Utils.IsInRole (MonkeyWrench.DataClasses.Logic.Roles.Administrator)) {
			Response.Redirect ("index.aspx");
			return;
		}

		if (!IsPostBack) {
			if (int.TryParse (Request ["id"], out id)) {
				GetViewLaneFileHistoryDataResponse response;
				response = Master.WebService.GetViewLaneFileHistoryData (Master.WebServiceLogin, id);

				tblFiles.Rows.Add (Utils.CreateTableHeaderRow ("Date changed", "Actions"));
				foreach (DBLanefile file in response.Lanefiles) {
					tblFiles.Rows.Add (Utils.CreateTableRow (
						file.changed_date.Value.ToString ("yyyy/MM/dd HH:mm:ss"),
						string.Format ("<a href='EditLaneFile.aspx?file_id={0}'>View</a>", file.id)));
				}
			}
		}
	}
}
