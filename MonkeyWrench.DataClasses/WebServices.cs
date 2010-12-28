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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

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
				} catch (OutOfMemoryException oom) {
					Logger.Log ("Could not {0}: {1}, will not retry, this is a fatal exception.", message, oom.Message);
					throw;
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

		private static string ReadString (BinaryReader reader, byte [] buffer, ushort length)
		{
			reader.Read (buffer, 0, length);
			return UTF8Encoding.UTF8.GetString (buffer, 0, length);
		}

		private void ReadResponse (BinaryReader reader, out byte version, out byte type)
		{
			ushort message_length;
			string message;
			byte [] buffer;

			version = reader.ReadByte ();
			type = reader.ReadByte ();

			if (version != 1)
				throw new ApplicationException (string.Format ("Got unexpected response version from server, expected version = 1, got version = {0} and type = {1}", version, type));

			switch (type) {
			case 3:
				/* error */
				message_length = reader.ReadUInt16 ();
				buffer = new byte [message_length];
				message = ReadString (reader, buffer, message_length);
				throw new ApplicationException (string.Format ("Got error from server: {0}", message));
			}
		}

		private void WriteStringByte (BinaryWriter writer, string str)
		{
			byte [] data = UTF8Encoding.UTF8.GetBytes (str);
			writer.Write ((byte) data.Length);
			writer.Write (data, 0, data.Length);
		}

		public void UploadFilesSafe (DBWork work, string [] filenames, bool [] hidden_values)
		{
			Queue<string> files = new Queue<string> (filenames);
			Queue<bool> hiddens;

			if (hidden_values != null) {
				hiddens = new Queue<bool> (hidden_values);
			} else {
				hiddens = new Queue<bool> (new bool [filenames.Length]);
			}

			if (filenames.Length != hiddens.Length)
				throw new ArgumentOutOfRangeException ("filenames/hidden");

			ExecuteSafe ("Upload files safe", () =>
			{
				byte version;
				byte type;
				int port;
				string gz = null;
				BinaryReader reader = null;
				BinaryWriter writer = null;
				NetworkStream stream = null;
				byte [] buffer = new byte [1024];
				TcpClient client = null;

				Logger.Log ("UploadFilesSafe () trying to upload {0} files...", files.Count);

				port = this.GetUploadPort ();

				try {
					client = new TcpClient ();
					client.Connect (new Uri (this.Url, UriKind.Absolute).Host, port);
					stream = client.GetStream ();
					stream.ReadTimeout = (int) TimeSpan.FromMinutes (5).TotalMilliseconds;
					stream.WriteTimeout = stream.ReadTimeout;
					reader = new BinaryReader (stream);
					writer = new BinaryWriter (stream);

					writer.Write ((byte) 1); // version
					WriteStringByte (writer, WebServiceLogin.User); // name_length + name
					WriteStringByte (writer, WebServiceLogin.Password); // password_length + password
					writer.Write ((int) work.id); // work_id
					writer.Write ((ushort) filenames.Length); // file_count
					writer.Write ((ulong) 0); // reserved

					while (files.Count > 0) {
						string path_to_contents = files.Peek ();
						string filename = Path.GetFileName (path_to_contents);
						string compressed_mime;
						bool hidden = hiddens.Peek ();
						byte [] md5;
						FileInfo fi;

						Logger.Log (2, "WebServices.UploadFilesSafe (): uploading '{0}' to port {1}", filename, port);

						using (FileStream fs = new FileStream (path_to_contents, FileMode.Open, FileAccess.Read, FileShare.Read))
							md5 = FileUtilities.CalculateMD5_Bytes (fs);

						writer.Write ("MonkeyWrench".ToCharArray ()); // marker
						writer.Write (md5, 0, md5.Length); // md5
						writer.Write ((byte) (hidden ? 3 : 1)); // flags
						WriteStringByte (writer, filename); // filename_length + filename
						writer.Flush ();

						ReadResponse (reader, out version, out type);

						Logger.Log (2, "WebServices.UploadFilesSafe (): uploading '{0}', got response type {1}", filename, type);

						// 1 = everything OK, 2 = file received OK, 3 = error, 4 = send file
						switch (type) {
						case 2:
							/* No need to send file */
							break;
						case 4:
							/* Send file */
							int read;
							int total = 0;

							// try to compress the file
							string original_contents = path_to_contents;
							try {
								gz = FileUtilities.GZCompress (path_to_contents);
								if (gz != null) {
									path_to_contents = gz;
									compressed_mime = MimeTypes.GZ;
								} else {
									path_to_contents = filename;
									compressed_mime = null;
								}
								Logger.Log (2, "Compressed {0} to {1}.", original_contents, gz);
							} catch (Exception ex) {
								path_to_contents = filename;
								compressed_mime = null;
								Logger.Log ("Could not compress the file {0}: {1}, uploading uncompressed.", filename, ex.Message);
							}

							fi = new FileInfo (path_to_contents);

							long length = fi.Length;
							if (length > 1024 * 1024 * 100) {
								files.Dequeue ();
								hiddens.Dequeue ();
								throw new ApplicationException (string.Format ("Not uploading {0} ({2}): filesize is > 100MB (it is: {1} MB)", path_to_contents, length / (1024.0 * 1024.0), filename));
							}

							if (compressed_mime == null) {
								writer.Write ((byte) 0);
							} else {
								WriteStringByte (writer, compressed_mime); // compressed_mime_length + compressed_mime
							}
							writer.Write ((uint) fi.Length); // content_length

							using (FileStream fs = new FileStream (path_to_contents, FileMode.Open, FileAccess.Read, FileShare.Read)) {
								while ((read = fs.Read (buffer, 0, buffer.Length)) != 0) {
									total += read;
									writer.Write (buffer, 0, read);
								}
								writer.Flush ();
							}
							Logger.Log (2, "WebServices.UploadFilesSafe (): uploaded '{0}', {1} bytes", filename, total);

							ReadResponse (reader, out version, out type);
							if (type == 2) {
								Logger.Log ("WebServices.UploadFilesSafe (): uploaded '{0}' successfully", filename);
							}
							break;
						}

						files.Dequeue ();
						hiddens.Dequeue ();
					}
					ReadResponse (reader, out version, out type);
					if (type == 4) {
						Logger.Log ("WebServices.UploadFilesSafe (): all files uploaded successfully");
					}
				} finally {
					FileUtilities.TryDeleteFile (gz);
					try {
						stream.Close ();
						reader.Close ();
						writer.Close ();
					} catch {
						// Ignore
					}
					try {
						client.Close ();
					} catch {
						// Ignore
					}
				}
			});

		}

		/// <summary>
		/// Uploads the file (compressed if possible) and in case of failures retries until it succeeds.
		/// </summary>
		/// <param name="work"></param>
		/// <param name="filename"></param>
		/// <param name="hidden"></param>
		[Obsolete ()]
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
			} catch (Exception ex) {
				Logger.Log ("Could not upload {0}: {1}", filename, ex.Message);
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
