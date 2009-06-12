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

				if (Directory.Exists (result)) {
					result += Path.DirectorySeparatorChar;
					if (File.Exists (Path.Combine (result, md5))) {
						return Path.Combine (result, md5);
					} else if (File.Exists (Path.Combine (result, md5) + ".gz")) {
						return Path.Combine (result, md5) + ".gz";
					}
				}
			} while (!string.IsNullOrEmpty (name));

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
				int workfile_id;
				int revision_id;
				bool diff;
				string md5;

				DateTime start = DateTime.Now;

				int.TryParse (Request ["revision_id"], out revision_id);
				int.TryParse (Request ["workfile_id"], out workfile_id);
				bool.TryParse (Request ["diff"], out diff);
				md5 = Request ["md5"];

				if (workfile_id != 0 || !string.IsNullOrEmpty (md5)) {
					DownloadWorkFile (workfile_id, md5);
				} else if (revision_id != 0) {
					DownloadRevisionLog (revision_id, diff);
				} else {
					throw new HttpException (404, "Nothing to download.");
				}

			} catch (Exception ex) {
				Console.WriteLine ("Download failed:");
				Console.WriteLine (ex);
				throw;
			}
		}

		private void DownloadStream (Stream str, string compressed_mime)
		{
			// access must be verified before calling this method (no verification is done here)
			Response.AppendHeader ("Content-Length", str.Length.ToString ());

			if (compressed_mime == MimeTypes.GZ)
				Response.AppendHeader ("Content-Encoding", "gzip");

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

		private void DownloadMd5 (string md5)
		{
			// access must be verified before calling this method (no verification is done here)
			string fullpath = GetFullPath (md5);

			if (fullpath == null)
				throw new HttpException (404, "Could not find the file.");

			using (FileStream str = new FileStream (fullpath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				DownloadStream (str, fullpath.EndsWith (".gz") ? MimeTypes.GZ : string.Empty);
			}
		}

		private void DownloadFile (DB db, int file_id)
		{
			// access must be verified before calling this method (no verification is done here)
			DownloadFile (db, DBFile_Extensions.Create (db, file_id));
		}
		private void DownloadFile (DB db, DBFile file)
		{
			// access must be verified before calling this method (no verification is done here)
			if (file.file_id == null) {
				DownloadMd5 (file.md5);
			} else {
				using (Stream str = db.Download (file)) {
					DownloadStream (str, file.compressed_mime);
				}
			}
		}

		private void DownloadRevisionLog (int revision_id, bool diff /* diff or log */)
		{
			DBRevision revision;

			using (DB db = new DB ()) {
				WebServiceLogin login = new WebServiceLogin ();
				login.Cookie = Request ["cookie"];
				login.User = Request ["user"];
				login.Ip4 = Request ["ip4"];

				revision = DBRevision_Extensions.Create (db, revision_id);

				// no access restricion on revision logs/diffs

				Response.ContentType = MimeTypes.TXT;

				if (revision == null) {
					Response.Write ("Revision not found.");
				} else {
					if (diff) {
						if (revision.diff_file_id.HasValue) {
							DownloadFile (db, revision.diff_file_id.Value);
						} else if (!string.IsNullOrEmpty (revision.diff)) {
							Response.Write (revision.diff);
						} else {
							Response.Write ("No diff yet.");
						}
					} else {
						if (revision.log_file_id.HasValue) {
							DownloadFile (db, revision.log_file_id.Value);
						} else if (!string.IsNullOrEmpty (revision.log)) {
							Response.Write (revision.log);
						} else {
							Response.Write ("No log yet.");
						}
					}
				}
			}
		}

		private void DownloadWorkFile (int workfile_id, string md5)
		{
			DBWorkFileView view = null;
			DBFile file = null;
			string mime;
			string filename;
			string compressed_mime;

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

				// any access rights verified, serve the file

				if (view != null) {
					if (view.file_file_id == null) {
						DownloadMd5 (view.md5);
					} else {
						using (Stream str = db.Download (view)) {
							DownloadStream (str, compressed_mime);
						}
					}
				} else {
					DownloadFile (db, file);
				}
			}
		}
	}
}
