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
using System.Text;
using System.Web;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;

namespace MonkeyWrench.WebServices
{
	public partial class Download : System.Web.UI.Page
	{
		private static string GetFullPath (string md5)
		{
			string result = Configuration.GetFilesDirectory ();
			string name = md5;

			if (!result.EndsWith (Path.DirectorySeparatorChar.ToString ()))
				result += Path.DirectorySeparatorChar;
			
			do {
				result += name [0];
				name = name.Substring (1);

				if (Directory.Exists (result))
					result += Path.DirectorySeparatorChar;
			} while (!string.IsNullOrEmpty (name)) ;

			if (File.Exists (result))
				return result;

			if (File.Exists (result + ".gz"))
				return result + ".gz";

			// we now have the directory of the file
			result = Path.Combine (result, md5);

			if (File.Exists (result))
				return result;

			if (File.Exists (result + ".gz"))
				return result + ".gz";

			return null;
		}

		protected void Page_Load (object sender, EventArgs e)
		{
			try {
				DBWorkFileView view = null;
				DBFile file = null;
				string mime;
				string filename;
				string compressed_mime;
				int workfile_id;
				string md5;

				DateTime start = DateTime.Now;

				int.TryParse (Request ["workfile_id"], out workfile_id);
				md5 = Request ["md5"];

				using (DB db = new DB ()) {
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

					Response.ContentType = mime;
					Response.AppendHeader ("Content-Disposition", "filename=" + Path.GetFileName (filename));

					Stream str = null;
					string file_md5 = null;
					if (view != null) {
						if (view.file_file_id == null) {
							file_md5 = view.md5;
						} else {
							str = db.Download (view);
						}
					} else {
						if (file.file_id == null) {
							file_md5 = file.md5;
						} else {
							str = db.Download (file);
						}
					}

					if (file_md5 == null && str == null) {
						throw new HttpException (404, "Could not find the file.");
					} else if (file_md5 != null) {

						string fullpath = GetFullPath (file_md5);

						if (fullpath == null)
							throw new HttpException (404, "Could not find the file.");

						if (fullpath.EndsWith (".gz"))
							Response.AppendHeader ("Content-Encoding", "gzip");

						Response.WriteFile (fullpath);
					} else {

						if (compressed_mime == "application/x-gzip")
							Response.AppendHeader ("Content-Encoding", "gzip");

						using (str) {
							Response.AppendHeader ("Content-Length", str.Length.ToString ());

							byte [] buffer = new byte [1024];
							int read;
							int total = 0;

							read = str.Read (buffer, 0, buffer.Length);
							total += read;
							while (read > 0) {
								Response.OutputStream.Write (buffer, 0, read);
								read = str.Read (buffer, 0, buffer.Length);
								total += read;
								Response.Flush ();
							}
						}
					}
				}
			} catch (Exception ex) {
				Console.WriteLine ("Download failed:");
				Console.WriteLine (ex);
				throw;
			}
		}
	}
}
