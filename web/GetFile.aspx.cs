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
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Builder;

public partial class GetFile : System.Web.UI.Page
{
	DBLoginView login;

	private void PrintTiming (DateTime start, string msg)
	{
		// Console.WriteLine ("GetFile {0,5} ms, {1}", (int) (DateTime.Now - start).TotalMilliseconds, msg);
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		DBWorkFileView view;
		int id;
		DateTime start = DateTime.Now;

		if (!int.TryParse (Request ["id"], out id))
			throw new HttpException ("Invalid id");

		using (DB db = new DB (true)) {
			PrintTiming (start, "Connected to db");
			view = DBWorkFileView.Find (db, id);

			if (view == null)
				throw new HttpException (404, "Could not find the file.");

			PrintTiming (start, "Got view");
			login = Authentication.GetLogin (db, Request, Response);

			if (view.@internal && login == null) {
				Response.Redirect ("Login.aspx", false);
				Page.Visible = false;
				return;
			}
			PrintTiming (start, "Logged in");

			Response.ContentType = view.mime;
			Response.AppendHeader ("Content-Disposition",  "filename=" + Path.GetFileName (view.filename));

			if (view.compressed_mime == "application/x-gzip")
				Response.AppendHeader ("Content-Encoding", "gzip");

			using (Stream str = db.Download (view)) {
				PrintTiming (start, "Downloaded file");
				Response.AppendHeader ("Content-Length", str.Length.ToString ());
				PrintTiming (start, string.Format ("File has size: {0}", str.Length));

				byte [] buffer = new byte [1024];
				int read;
				int total = 0;

				read = str.Read (buffer, 0, buffer.Length);
				total += read;
				while (read > 0) {
					PrintTiming (start, string.Format ("Read {0} more bytes, total {1} bytes.", read, total));
					Response.OutputStream.Write (buffer, 0, read);
					read = str.Read (buffer, 0, buffer.Length);
					total += read;
					Response.Flush ();
				}
			}
		}
	}
}
