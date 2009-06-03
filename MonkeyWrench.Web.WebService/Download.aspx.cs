/*
 * Download.aspx.cs
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

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;

namespace MonkeyWrench.WebServices
{
	public partial class Download : System.Web.UI.Page
	{
		private void PrintTiming (DateTime start, string msg)
		{
			Console.WriteLine ("GetFile {0,5} ms, {1}", (int) (DateTime.Now - start).TotalMilliseconds, msg);
		}

		protected void Page_Load (object sender, EventArgs e)
		{
			DBWorkFileView view = null;
			DBFile file = null;
			string mime;
			string filename;
			string compressed_mime;
			int workfile_id;
			string md5;

			DateTime start = DateTime.Now;

			Console.WriteLine ("Download");

			int.TryParse (Request ["workfile_id"], out workfile_id);
			md5 = Request ["md5"];

			Console.WriteLine ("Download 2");

			using (DB db = new DB ()) {
				PrintTiming (start, "Connected to db");

				WebServiceLogin login = new WebServiceLogin ();
				login.Cookie = Request ["cookie"];
				login.User = Request ["user"];
				login.Ip4 = Request ["ip4"];

				if (!string.IsNullOrEmpty (md5)) { // md5 lookup needs admin rights
					Authentication.VerifyUserInRole (Context, db, login, Roles.Administrator);
					file = DBFile_Extensions.Find (db, md5);

					if (file == null)
						throw new HttpException (404, "Could not find the file.");

					mime = file.mime;
					filename = file.filename;
					compressed_mime = file.compressed_mime;
				} else {
					view = DBWorkFileView_Extensions.Find (db, workfile_id);

					if (view == null)
						throw new HttpException (404, "Could not find the file.");

					if (view.@internal) // internal files need admin rights
						Authentication.VerifyUserInRole (Context, db, login, Roles.Administrator);

					mime = view.mime;
					filename = view.filename;
					compressed_mime = view.compressed_mime;
				}

				PrintTiming (start, "Got view/file");

				Response.ContentType = mime;
				Response.AppendHeader ("Content-Disposition", "filename=" + Path.GetFileName (filename));

				if (compressed_mime == "application/x-gzip")
					Response.AppendHeader ("Content-Encoding", "gzip");

				Stream str;
				if (view != null) {
					str = db.Download (view);
				} else {
					str = db.Download (file);
				}

				using (str) {
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
}
