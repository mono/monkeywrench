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
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Web;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;

namespace MonkeyWrench.WebServices
{
	public partial class Download : System.Web.UI.Page
	{
		

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
				Console.Error.WriteLine ("Download failed:");
				Console.Error.WriteLine (ex);
				throw;
			}
		}

		private void DownloadStream (Stream str, string compressed_mime)
		{
			// access must be verified before calling this method (no verification is done here)
			if (compressed_mime == MimeTypes.GZ) {
				string AcceptEncoding = Request.Headers["Accept-Encoding"];
				if (!string.IsNullOrEmpty (AcceptEncoding) && AcceptEncoding.Contains ("gzip")) {
					Response.AppendHeader ("Content-Encoding", "gzip");
				} else {
					str = new GZipStream (str, CompressionMode.Decompress);
				}
			}
			
			try {
				Response.AppendHeader ("Content-Length", str.Length.ToString ());
			} catch (NotSupportedException ex)  {
				// GZipStreams don't usually know their length, just ignore
			}

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
			string fullpath = DBFile_Extensions.GetFullPath (md5);

			if (fullpath == null)
				throw new HttpException (404, "Could not find the file.");

			if (fullpath.EndsWith (".gz")) {
				string AcceptEncoding = Request.Headers["Accept-Encoding"];
				if (!string.IsNullOrEmpty(AcceptEncoding) && AcceptEncoding.Contains("gzip")) {
					Response.AppendHeader ("Content-Encoding", "gzip");
				} else {
					// need to decompress this stream...
					using (Stream str = File.OpenRead (fullpath)) {
						DownloadStream (str, MimeTypes.GZ);
					}
					return;
				}
			}
			
			Response.TransmitFile (fullpath);
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

		private WebServiceLogin CreateLogin ()
		{
			WebServiceLogin login = new WebServiceLogin ();

			login.Cookie = Request ["cookie"];
			if (string.IsNullOrEmpty (login.Cookie)) {
				if (Request.Cookies ["cookie"] != null) {
					login.Cookie = Request.Cookies ["cookie"].Value;
				}
			}

			login.User = Request ["user"];
			if (string.IsNullOrEmpty (login.User)) {
				if (Request.Cookies ["user"] != null) {
					login.User = Request.Cookies ["user"].Value;
				}
			}

			login.Ip4 = Request ["ip4"];
			if (string.IsNullOrEmpty (login.Ip4)) {
				login.Ip4 = Utilities.GetExternalIP (Request);
			}
			
			return login;
		}

		private void DownloadRevisionLog (int revision_id, bool diff /* diff or log */)
		{
			DBRevision revision;
			DBLane lane;

			using (DB db = new DB ()) {
				WebServiceLogin login = CreateLogin ();

				revision = DBRevision_Extensions.Create (db, revision_id);

				// no access restricion on revision logs/diffs

				Response.ContentType = MimeTypes.TXT;

				if (revision == null) {
					Response.Write ("Revision not found.");
				} else {
					if (diff) {
						lane = DBLane_Extensions.Create (db, revision.lane_id);
						if (lane.source_control == "git") {
							using (Process git = new Process ()) {
								git.StartInfo.RedirectStandardOutput = true;
								git.StartInfo.RedirectStandardError = true;
								git.StartInfo.UseShellExecute = false;
								git.StartInfo.FileName = "git";
								git.StartInfo.Arguments = "diff --no-color --no-prefix " + revision.revision + " " + revision.revision + "~";
								git.StartInfo.WorkingDirectory = Configuration.GetSchedulerRepositoryCacheDirectory (lane.repository);
								git.OutputDataReceived += (object sender, DataReceivedEventArgs ea) =>
								{
									Response.Write (ea.Data);
									Response.Write ('\n');
								};
								git.ErrorDataReceived += (object sender, DataReceivedEventArgs ea) =>
								{
									Response.Write (ea.Data);
									Response.Write ('\n');
								};
								Logger.Log ("Executing: '{0} {1}' in {2}", git.StartInfo.FileName, git.StartInfo.Arguments, git.StartInfo.WorkingDirectory);
								git.Start ();
								git.BeginErrorReadLine ();
								git.BeginOutputReadLine ();
								if (!git.WaitForExit (1000 * 60 * 5 /* 5 minutes */)) {
									git.Kill ();
									Response.Write ("Error: git diff didn't finish in 5 minutes, aborting.\n");
								}
							}
						} else {
							if (revision.diff_file_id.HasValue) {
								DownloadFile (db, revision.diff_file_id.Value);
							} else if (!string.IsNullOrEmpty (revision.diff)) {
								Response.Write (revision.diff);
							} else {
								Response.Write ("No diff yet.");
							}
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
			string filename;
			string mime;
			string compressed_mime;

			using (DB db = new DB ()) {
				WebServiceLogin login = CreateLogin ();

				filename = Request ["filename"];

				if (!string.IsNullOrEmpty (md5)) { // md5 lookup needs admin rights
					Authentication.VerifyUserInRole (Context, db, login, Roles.Administrator, false);
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
						Authentication.VerifyUserInRole (Context, db, login, Roles.Administrator, false);

					if (!string.IsNullOrEmpty (filename)) {
						file = DBWork_Extensions.GetFile (db, view.work_id, filename, false);
						if (file == null)
							throw new HttpException (404, string.Format ("Could not find the filename '{0}'", filename));

						mime = file.mime;
						compressed_mime = file.compressed_mime;
						md5 = file.md5;

						view = null;
					} else {
						mime = view.mime;
						filename = view.filename;
						compressed_mime = view.compressed_mime;
					}
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
