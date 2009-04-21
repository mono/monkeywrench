/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Builder;

public partial class GetDependentFile : System.Web.UI.Page
{
	DBLoginView login;

	private void PrintTiming (DateTime start, string msg)
	{
		// Console.WriteLine ("GetDependentFile {0,5} ms, {1}", (int) (DateTime.Now - start).TotalMilliseconds, msg);
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		DBWorkFileView view;
		DateTime start = DateTime.Now;
		string filename;
		string revision;
		string lane;
		string host;

		using (DB db = new DB (true)) {
			PrintTiming (start, "Connected to db");

			PrintTiming (start, "Got view");
			login = Authentication.GetLogin (db, Request, Response);

			filename = Request ["filename"];
			revision = Request ["revision"];
			host = Request ["host"];
			lane = Request ["lane"];


			if (lane == null)
				throw new ApplicationException (string.Format ("Could not find the lane ('{0}' or {1})", Request ["lane"], Request ["lane_id"]));

			view = DBWorkFileView.Find (db, filename, lane, revision, host);

			if (view == null)
				throw new HttpException (404, "Could not find the file.");

			if (view.@internal && login == null) {
				Response.Redirect ("Login.aspx", false);
				Page.Visible = false;
				return;
			}

			PrintTiming (start, "Logged in");

			Response.ContentType = view.mime;
			Response.AppendHeader ("Content-Disposition", "filename=" + Path.GetFileName (view.filename));

			string fn = null;
			FileStream fs = null;
			Stream str = null;

			try {
				string accept_encoding = Request.Headers ["Accept-Encoding"];

				if (view.compressed_mime == "application/x-gzip" && (accept_encoding == null || !accept_encoding.Contains ("gzip") )) {
					fn = FileManager.GZUncompress (db, view);
					str = new FileStream (fn, FileMode.Open, FileAccess.Read, FileShare.Read);
				} else {
					str = db.Download (view);
					if (view.compressed_mime == "application/x-gzip")
						Response.AppendHeader ("Content-Encoding", "gzip");
				}

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
			} finally {
				if (str != null)
					str.Close ();
				if (fs != null)
					fs.Close ();
				try {
					File.Delete (fn);
				} catch {
					// Ignore
				}
			}
		}
	}
}
