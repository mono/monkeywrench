/*
 * EditHosts.aspx.cs
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

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class EditHosts : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		if (!IsPostBack) {
			string action = Request ["action"];
			int host_id;

			if (!string.IsNullOrEmpty (action)) {
				switch (action) {
				case "remove":
					if (!int.TryParse (Request ["host_id"], out host_id))
						break;

					Response.Redirect ("Delete.aspx?action=delete-host&host_id=" + host_id.ToString ());
					return;
				case "add":
					try {
						Master.WebService.AddHost (Master.WebServiceLogin, Request ["host"]);
						Response.Redirect ("EditHosts.aspx");
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
		} else if (!string.IsNullOrEmpty (Request ["txtHost"])) {
			try {
				Master.WebService.AddHost (Master.WebServiceLogin, Request ["txtHost"]);
				Response.Redirect ("EditHosts.aspx");
				return;
			} catch (Exception ex) {
				lblMessage.Text = Utils.FormatException (ex);
			}
		}

		GetHostsResponse response = Master.WebService.GetHosts (Master.WebServiceLogin);
		TableRow row;

		foreach (DBHost host in response.Hosts) {
			row = new TableRow ();
			row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='EditHost.aspx?host_id={0}'>{1}</a>", host.id, host.host)));
			row.Cells.Add (Utils.CreateTableCell (
				string.Format ("<a href='EditHosts.aspx?host_id={0}&amp;action=remove'>Delete</a> ", host.id) +
				string.Format ("<a href='ViewHostHistory.aspx?host_id={0}'>View history</a>", host.id)));
			row.Cells.Add (Utils.CreateTableCell (host.description));
			row.Cells.Add (Utils.CreateTableCell (host.architecture));
			tblHosts.Rows.Add (row);
		}
	}
}
