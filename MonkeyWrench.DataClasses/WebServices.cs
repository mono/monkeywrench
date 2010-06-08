/*
 * WebServices.cs
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
using System.IO;
using System.Net;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;

namespace MonkeyWrench.Web.WebServices
{
	public partial class WebServices
	{
		public WebServiceLogin WebServiceLogin;

		private static string CreatePage (string page)
		{
			if (Configuration.WebServiceUrl.EndsWith ("/")) {
				return Configuration.WebServiceUrl + page;
			} else {
				return Configuration.WebServiceUrl + "/" + page;
			}
		}

		public void CreateLogin (string user, string password)
		{
			WebServiceLogin = new WebServiceLogin ();
			WebServiceLogin.User = user;
			WebServiceLogin.Password = password;
		}

		public static WebServices Create ()
		{
			WebServices result = new WebServices ();
			result.Url = CreatePage ("WebServices.asmx");
			return result;
		}

		public string CreateWebServiceDownloadUrl (int workfile_id)
		{
			return CreateWebServiceDownloadUrl (workfile_id, WebServiceLogin, false);
		}

		public string CreateWebServiceDownloadRevisionUrl (int revision_id, bool diff)
		{
			return CreateWebServiceDownloadRevisionUrl (revision_id, diff, WebServiceLogin);
		}

		public static string CreateWebServiceDownloadUrl (int workfile_id, WebServiceLogin login, bool redirect)
		{
			string uri = CreatePage ("Download.aspx");
			uri += "?";
			uri += "workfile_id=" + workfile_id.ToString ();
			if (!redirect) {
				uri += "&cookie=" + login.Cookie;
				uri += "&ip4=" + login.Ip4;
				uri += "&user=" + login.User;
			}
			return uri;

		}

		public static string CreateWebServiceDownloadRevisionUrl (int revision_id, bool diff, WebServiceLogin login)
		{
			string uri = CreatePage ("Download.aspx");
			uri += "?";
			uri += "cookie=" + login.Cookie;
			uri += "&ip4=" + login.Ip4;
			uri += "&user=" + login.User;
			uri += "&revision_id=" + revision_id.ToString ();
			uri += "&diff=" + (diff ? "true" : "false");
			return uri;
		}

		/// <summary>
		/// This method will uncompress the data too (if required)
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static string DownloadString (string url)
		{
			string tmp = null;
			try {
				tmp = Path.GetTempFileName ();
				using (WebClient wc = new WebClient ()) {
					wc.Headers.Add ("Accept-Encoding", "gzip");
					wc.DownloadFile (url, tmp);

					if (wc.ResponseHeaders ["Content-Encoding"] == "gzip") {
						FileUtilities.GZUncompress (tmp);
					}
					return File.ReadAllText (tmp);
				}
			} finally {
				try {
					File.Delete (tmp);
				} catch {
				}
			}
		}

		private void DownloadFile (DBWorkFile file, string directory)
		{
			string filename = Path.Combine (directory, file.filename);

			if (!Directory.Exists (directory))
				Directory.CreateDirectory (directory);

			using (WebClient web = new WebClient ()) {
				web.Headers.Add ("Accept-Encoding", "gzip");
				web.DownloadFile (CreateWebServiceDownloadUrl (file.id), filename);

				if (web.ResponseHeaders ["Content-Encoding"] == "gzip")
					FileUtilities.GZUncompress (filename);
			}
		}

		private void ExecuteSafe (string message, Action action)
		{
			ExecuteSafe<object> (message, delegate ()
			{
				action ();
				return null;
			});
		}

		/// <summary>
		/// Executes a delegate, retrying for ConnectionRetryDuration minutes.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="message"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		private T ExecuteSafe<T> (string message, Func<T> action)
		{
			DateTime start = DateTime.Now;

			do {
				// try to upload the file
				try {
					return action ();
				} catch (Exception ex) {
					if ((DateTime.Now - start).TotalMinutes < Configuration.ConnectionRetryDuration) {
						Logger.Log ("Could not {0}: {1}, retrying in 1 minute.", message, ex.Message);
						System.Threading.Thread.Sleep (TimeSpan.FromMinutes (1));
						continue;
					} else {
						Logger.Log ("Could not {0}: {1}. Reached max retry duration ({2} minutes), won't try again.", message, ex.Message, Configuration.ConnectionRetryDuration);
						Logger.Log (ex.ToString ());
						throw;
					}
				}
			} while (true);
		}

		public void DownloadFileSafe (DBWorkFile file, string directory)
		{
			ExecuteSafe ("download workfile", delegate ()
			{
				DownloadFile (file, directory);
			});
		}

		public DBState GetWorkStateSafe (DBWork work)
		{
			return ExecuteSafe ("get work state", delegate ()
				{
					return GetWorkState (WebServiceLogin, work);
				});
		}

		public ReportBuildStateResponse ReportBuildStateSafe (DBWork work)
		{
			return ExecuteSafe ("report build state", delegate ()
			{
				return this.ReportBuildState (WebServiceLogin, work);
			});
		}

		/// <summary>
		/// Uploads the file (compressed if possible) and in case of failures retries until it succeeds.
		/// </summary>
		/// <param name="work"></param>
		/// <param name="filename"></param>
		/// <param name="hidden"></param>
		public void UploadFileSafe (DBWork work, string filename, bool hidden)
		{
			string gz = null;
			DateTime start = DateTime.Now;
			string file_to_upload = null;
			string compressed_mime = null;

			try { // try to upload

				// try to compress the file
				try {
					gz = FileUtilities.GZCompress (filename);
					if (gz != null) {
						file_to_upload = gz;
						compressed_mime = MimeTypes.GZ;
					} else {
						file_to_upload = filename;
						compressed_mime = null;
					}
				} catch (Exception ex) {
					file_to_upload = filename;
					compressed_mime = null;
					Logger.Log ("Could not compress the file {0}: {1}, uploading uncompressed.", filename, ex.Message);
				}

				long length = new FileInfo (file_to_upload).Length;
				if (length > 1024 * 1024 * 100) {
					Logger.Log ("Not uploading {0} ({2}): filesize is > 100MB (it is: {1} MB)", file_to_upload, length / (1024.0 * 1024.0), filename);
					return;
				}

				// try to upload the file
				ExecuteSafe (string.Format ("upload the file {0}", filename), delegate ()
				{
					this.UploadCompressedFile (WebServiceLogin, work, Path.GetFileName (filename), File.ReadAllBytes (file_to_upload), hidden, compressed_mime);
				});

			} finally {
				// clean up
				try {
					// delete any files we may have created
					if (gz != null)
						File.Delete (gz);
				} catch {
					// ignore any exceptions
				}
			}
		}

		public static void ExecuteSchedulerAsync ()
		{
			WebServices WebService = Create ();
			WebService.CreateLogin (Configuration.SchedulerAccount, Configuration.SchedulerPassword);
			WebService.ExecuteScheduler (WebService.WebServiceLogin, Configuration.ForceFullUpdate);
		}
	}
}
