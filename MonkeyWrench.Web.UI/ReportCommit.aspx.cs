/*
 * ReportCommit.aspx.cs
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
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.Web.WebServices;

public partial class ReportCommit : System.Web.UI.Page
{
	protected void Page_Load (object sender, EventArgs e)
	{
		HttpPostedFile xml;
		xml = Request.Files ["xml"];

		if (Request.UserHostAddress != "130.57.169.27" && Request.UserHostAddress != "130.57.21.45") {
			Logger.Log ("ReportCommit.aspx: {0} tried to send a file, ignored.", Request.UserHostAddress);
			return;
		}

		if (xml != null) {
			string outdir = Configuration.GetSchedulerCommitsDirectory ();
			string outfile = Path.Combine (outdir, string.Format ("commit-{0}.xml", DateTime.Now.ToString ("yyyy-MM-dd-HH-mm-ss")));

			if (xml.ContentLength > 1024 * 100) {
				Logger.Log ("ReportCommit.aspx: {0} tried to send oversized file (> {1} bytes.", Request.UserHostAddress, 1024 * 100);
				return;
			}

			if (!Directory.Exists (outdir))
				Directory.CreateDirectory (outdir);
			
			Logger.Log ("ReportCommit.aspx: Got 'xml' from {2} with size {0} bytes, writing to '{1}'", xml.ContentLength, outfile, Request.UserHostAddress);

			byte [] buffer = new byte [1024];
			int read;
			using (FileStream writer = new FileStream (outfile, FileMode.CreateNew, FileAccess.Write, FileShare.None, buffer.Length)) {
				while (0 < (read = xml.InputStream.Read (buffer, 0, buffer.Length))) {
					writer.Write (buffer, 0, read);
				}
			}

			WebServices.ExecuteSchedulerAsync ();
		} else {
			Logger.Log ("ReportCommit.aspx: Didn't get a file called 'xml'");
		}

		Response.Write ("OK\n");
	}
}
