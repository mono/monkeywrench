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
using System.Linq;
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

	public static string GetReportDateColor (bool enabled, DateTime? report_date)
	{
		var color = "black";
		if (enabled) {
			if (report_date != null) {
				var silence = DateTime.Now - report_date.Value;
				if (silence.TotalHours > 9) {
					color = "red";
				} else if (silence.TotalHours > 3) {
					color = "orange";
				} else {
					color = "green";
				}
			}
		} else {
			color = "gray";
		}

		return color;
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
						Utils.LocalWebService.AddHost (Master.WebServiceLogin, Request ["host"]);
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
			Utils.LocalWebService.AddHost (Master.WebServiceLogin, Request ["txtHost"]);
			Response.Redirect ("EditHosts.aspx");
			return;
		}

		GetHostsResponse response = Utils.LocalWebService.GetHosts (Master.WebServiceLogin);
		GetBuildBotStatusResponse statuses = Utils.LocalWebService.GetBuildBotStatus (Master.WebServiceLogin); // TODO: make only 1 call
		TableRow row;

		foreach (DBHost host in response.Hosts) {
			DBBuildBotStatus status = null;
			foreach (var tmp in statuses.Status) {
				if (tmp.host_id == host.id) {
					status = tmp;
					break;
				}
			}

			var color = GetReportDateColor (host.enabled, status != null ? status.report_date : (DateTime?) null);

			row = new TableRow ();
			row.Cells.Add (Utils.CreateTableCell (string.Format ("<a style='color:{2}' href='EditHost.aspx?host_id={0}'>{1}</a>", host.id, host.host, color)));
			row.Cells.Add (Utils.CreateTableCell (
				string.Format ("<a style='color:{1}' href='EditHosts.aspx?host_id={0}&amp;action=remove'>Delete</a> ", host.id, color) +
				string.Format ("<a style='color:{1}' href='ViewHostHistory.aspx?host_id={0}'>View history</a>", host.id, color)));
			row.Cells.Add (Utils.CreateTableCell (host.description));
			row.Cells.Add (Utils.CreateTableCell (host.architecture));
			row.Cells.Add (Utils.CreateTableCell (status == null ? "-" : status.report_date.ToString ("yyyy/MM/dd HH:mm:ss UTC")));

			row.Style.Add (HtmlTextWriterStyle.Color, color);
			tblHosts.Rows.Add (row);
		}
	}
}
