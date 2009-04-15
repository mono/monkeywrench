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

public partial class ViewHtmlReport : System.Web.UI.Page
{
	DBLoginView login;

	protected void Page_Load (object sender, EventArgs e)
	{
		new_implementation ();
	}

	private void new_implementation ()
	{
		DBWorkFileView view;
		DBFile file;
		string filename = Request ["filename"];
		string uncompressed_filename = null;
		string line;
		int workfile_id;
		int read;
		byte [] buffer;

		string [] find;
		string [] replace;

		if (!int.TryParse (Request ["workfile_id"], out workfile_id))
			return;

		find = new string [] { 
				"img src=\"", 
				"img src='", 
				"a href=\"", 
				"a href='" };
		replace = new string []{
				string.Format ("img src=\"ViewHtmlReport.aspx?workfile_id={0}&amp;filename=", workfile_id), 
				string.Format ("img src='ViewHtmlReport.aspx?workfile_id={0}&amp;filename=", workfile_id),
				string.Format ("a href=\"ViewHtmlReport.aspx?workfile_id={0}&amp;filename=", workfile_id),
				string.Format ("a href='ViewHtmlReport.aspx?workfile_id={0}&amp;filename=", workfile_id)};

		using (DB db = new DB (true)) {

			login = Authentication.GetLogin (db, Request, Response);
			view = DBWorkFileView.Find (db, workfile_id);

			if (view == null)
				throw new HttpException (404, "File not found.");

			if (string.IsNullOrEmpty (filename)) {

				Response.ContentType = "text/html";

				if (view.@internal && login == null) {
					Response.Redirect ("Login.aspx", false);
					return;
				}

				try {
					if (view.compressed_mime == "application/x-gzip") {
						// We need to uncompress the file to be able to redirect img src.
						uncompressed_filename = FileManager.GZUncompress (db, view);
					}

					using (Stream stream = uncompressed_filename == null ? db.Download (view) : new FileStream (uncompressed_filename, FileMode.Open, FileAccess.Read)) {
						using (StreamReader reader = new StreamReader (stream)) {
							while (null != (line = reader.ReadLine ())) {
								for (int i = 0; i < find.Length; i++)
									line = line.Replace (find [i], replace [i]);
								
								// undo any changes for relative links
								line = line.Replace (string.Format ("ViewHtmlReport.aspx?workfile_id={0}&amp;filename=#", workfile_id), "#");

								Response.Write (line);
								Response.Write ('\n');
								Response.Flush ();
							}
						}
					}
				} finally {
					try {
						if (uncompressed_filename != null && File.Exists (uncompressed_filename)) {
							File.Delete (uncompressed_filename);
						}
					} catch {
						// Ignore any exceptions.
					}
				}
			} else {
				file = DBWork.GetFile (db, view.work_id, filename, false);

				if (file == null)
					throw new HttpException (404, "File not found.");

				Response.ContentType = file.mime;

				if (file.compressed_mime == "application/x-gzip")
					Response.AppendHeader ("Content-Encoding", "gzip");

				using (Stream stream = db.Download (file)) {
					buffer = new byte [1024];
					while (0 != (read = stream.Read (buffer, 0, buffer.Length))) {
						Response.OutputStream.Write (buffer, 0, read);
						Response.Flush ();
					}
				}
			}
		}
	}
}
