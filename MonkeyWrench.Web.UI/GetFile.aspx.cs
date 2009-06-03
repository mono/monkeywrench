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

				Response.WriteFile (tmpfile);
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
