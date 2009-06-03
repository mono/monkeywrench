/*
 * GetFile.aspx.cs
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
using System.IO;
using System.Web;
using System.Net;

using MonkeyWrench.Web.WebServices;

public partial class GetFile : System.Web.UI.Page
{
	protected void Page_Load (object sender, EventArgs e)
	{
		int id;

		if (!int.TryParse (Request ["id"], out id))
			throw new HttpException ("Invalid id");

		string tmpfile = null;
		try {
			tmpfile = Path.GetTempFileName ();
			using (WebClient web = new WebClient ()) {
				web.DownloadFile (Utils.CreateWebServiceDownloadUrl (Request, id), tmpfile);

				Response.ContentType = web.ResponseHeaders ["Content-Type"];
				Response.AppendHeader ("Content-Disposition", web.ResponseHeaders ["Content-Disposition"]);
				Response.AppendHeader ("Content-Encoding", web.ResponseHeaders ["Content-Encoding"]);

				byte [] buffer = new byte [1024];
				int read;
				using (FileStream fs = new FileStream (tmpfile, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					using (StreamReader reader = new StreamReader (fs)) {
						while ((read = fs.Read (buffer, 0, buffer.Length)) != 0) {
							Response.OutputStream.Write (buffer, 0, read);
						}
					}
				}
				// This would be the best I guess, but I get intermittent failures with 
				// " [error] command failed: failed to send file (file data)" 
				// in apache's error log
				// Response.WriteFile (tmpfile);
				Response.Flush ();
				Response.Close ();
			}
		} finally {
			try {
				File.Delete (tmpfile);
			} catch {
			}
		}
	}
}
