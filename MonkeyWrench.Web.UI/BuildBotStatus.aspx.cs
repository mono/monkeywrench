/*
 * BuildBotStatus.asmx.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2010 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class BuildBotStatus : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		GetBuildBotStatusResponse response;
		string action;

		using (DB db = new DB ()) {
			MonkeyWrench.WebServices.Authentication.VerifyUserInRole(Context, db, Master.WebServiceLogin, Roles.Administrator, false);
		}

		response = Utils.LocalWebService.GetBuildBotStatus (Master.WebServiceLogin);

		action = Request ["action"];
		if (!string.IsNullOrEmpty (action)) {
			switch (action) {
			case "select-release": {
				int release_id;
				int host_id;
				if (int.TryParse (Request ["release_id"], out release_id)) {
					if (int.TryParse (Request ["host_id"], out host_id)) {
						DBHost host = response.Hosts.Find ((v) => v.id == host_id);
						if (host == null) {
							lblMessage.Text = "Invalid host id";
						} else {
							host.release_id = release_id == 0 ? null : new int? (release_id);
							Utils.LocalWebService.EditHost (Master.WebServiceLogin, host);
							Response.Redirect ("BuildBotStatus.aspx", false);
							return;
						}
					} else {
						lblMessage.Text = "Invalid host";
					}
				} else {
					lblMessage.Text = "Invalid release";
				}

				break;
			}
			}
		}

		if (response.Exception != null) {
			lblMessage.Text = response.Exception.Message;
		} else {
			foreach (DBBuildBotStatus status in response.Status) {
				DBHost host = response.Hosts.Find ((v) => v.id == status.host_id);
				DBRelease configured_release = host.release_id.HasValue ? response.Releases.Find ((v) => v.id == host.release_id.Value) : null;
				TableRow row = new TableRow ();
				row.Cells.Add (Utils.CreateTableCell (host.host));
				row.Cells.Add (Utils.CreateTableCell (status.version));
				row.Cells.Add (Utils.CreateTableCell (status.report_date.ToString ("yyyy/MM/dd HH:mm:ss UTC")));
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript: selectBuildBotRelease (\"{0}\", {1}, \"{2}\")'>{0}</a>", configured_release == null ? "Manual" : configured_release.version, host.id, tblReleases.ClientID)));
				tblStatus.Rows.Add (row);
			}

			foreach (DBRelease release in response.Releases) {
				TableRow row = new TableRow ();
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript: executeBuildBotSelection ({0})'>Select</a>", release.id)));
				row.Cells.Add (Utils.CreateTableCell (release.version));
				row.Cells.Add (Utils.CreateTableCell (release.revision));
				row.Cells.Add (Utils.CreateTableCell (release.description));
				tblReleases.Rows.Add (row);
			}
			tblReleases.Attributes ["style"] = "display:none";
		}
	}
}
