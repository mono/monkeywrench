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
using log4net;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.Web.WebServices;

public partial class ReportCommit : System.Web.UI.Page
{
	private static readonly ILog log = LogManager.GetLogger (typeof (ReportCommit));

	protected void Page_Load (object sender, EventArgs e)
	{
		if (Request.UserHostAddress != "130.57.169.27" && Request.UserHostAddress != "130.57.21.45") {
			log.WarnFormat ("{0} tried to send a file, ignored.", Request.UserHostAddress);
			Response.StatusCode = 403;
			return;
		}

		HttpPostedFile xml;
		xml = Request.Files ["xml"];

		if (xml == null) {
			log.Warn ("Didn't get a file called 'xml'");
			Response.StatusCode = 400;
			return;
		}

		string outdir = Configuration.GetSchedulerCommitsDirectory ();
		string outfile = Path.Combine (outdir, string.Format ("commit-{0}.xml", DateTime.Now.ToString ("yyyy-MM-dd-HH-mm-ss")));

		if (!Directory.Exists (outdir))
			Directory.CreateDirectory (outdir);
		
		log.InfoFormat ("ReportCommit.aspx: Got 'xml' from {2} with size {0} bytes, writing to '{1}'", xml.ContentLength, outfile, Request.UserHostAddress);

		byte [] buffer = new byte [1024];
		int read;
		using (FileStream writer = new FileStream (outfile, FileMode.CreateNew, FileAccess.Write, FileShare.None, buffer.Length)) {
			while (0 < (read = xml.InputStream.Read (buffer, 0, buffer.Length))) {
				writer.Write (buffer, 0, read);
			}
		}

		WebServices.ExecuteSchedulerAsync ();
		Response.StatusCode = 204;
	}
}
