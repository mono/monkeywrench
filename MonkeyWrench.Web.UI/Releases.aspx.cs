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

public partial class Releases : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		WebServiceResponse rsp;
		GetReleasesResponse response;
		int release_id;

		string action = Request ["action"];

		response = Utils.LocalWebService.GetReleases (Master.WebServiceLogin);

		if (response.Exception != null) {
			lblMessage.Text = response.Exception.Message;
			return;
		}

		if (!string.IsNullOrEmpty (action)) {
			switch (action) {
			case "delete":
				if (int.TryParse (Request ["release_id"], out release_id)) {
					rsp = Utils.LocalWebService.DeleteRelease (Master.WebServiceLogin, release_id);
					if (rsp.Exception != null) {
						lblMessage.Text = rsp.Exception.Message;
					} else {
						Response.Redirect ("Releases.aspx", false);
						return;
					}
				} else {
					lblMessage.Text = "Invalid release";
				}
				break;
			case "download":
				if (string.IsNullOrEmpty (Request ["release_id"]))
					break;

				if (int.TryParse (Request ["release_id"], out release_id)) {
					foreach (DBRelease release in response.Releases) {
						if (release.id != release_id)
							continue;

						Response.ContentType = "application/zip";
						Response.AddHeader ("Content-Disposition", "attachment; filename=" + release.filename);
						Response.TransmitFile (Path.Combine (Configuration.GetReleaseDirectory (), release.filename));
						return;
					}
				} else if (Request ["release_id"] == "latest") {
					Version latest = new Version ();
					DBRelease release_latest = null;

					foreach (DBRelease release in response.Releases) {
						Version v = new Version (release.version);
						if (v <= latest)
							continue;
						release_latest = release;
					}

					if (release_latest == null) {
						Response.StatusCode = 404;
						Response.Status = "Not found";
						Response.StatusDescription = "No latest release exists";
						return;
					}

					Response.ContentType = "application/zip";
					Response.AddHeader ("Content-Disposition", "attachment; filename=" + release_latest.filename);
					Response.TransmitFile (Path.Combine (Configuration.GetReleaseDirectory (), release_latest.filename));
					return;
				} else  {
					lblMessage.Text = "Invalid release";
				}
				break;
			}
		}

		foreach (DBRelease release in response.Releases) {
			TableRow row = new TableRow ();
			row.Cells.Add (Utils.CreateTableCell (release.version));
			row.Cells.Add (Utils.CreateTableCell (release.revision));
			row.Cells.Add (Utils.CreateTableCell (release.description));
			row.Cells.Add (Utils.CreateTableCell (release.filename));
			row.Cells.Add (Utils.CreateTableCell (
				string.Format ("<a href='Releases.aspx?action=delete&amp;release_id={0}'>Delete</a>", release.id) + " " +
				string.Format ("<a href='Releases.aspx?action=download&amp;release_id={0}'>Download</a>", release.id)
				));
			tblStatus.Rows.Add (row);
		}
	}
}