/*
 * WebServices.asmx.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2010 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Services;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;

namespace MonkeyWrench.WebServices
{
	static class Upload
	{
		private static TcpListener listener;
		private static object lockobj = new object ();
		private static AsyncCallback accept_cb;
		private static int counter;

		public static int GetListenPort ()
		{
			if (listener == null)
				StartListener ();

			return ((IPEndPoint) listener.LocalEndpoint).Port;
		}

		private static void StartListener ()
		{
			lock (lockobj) {
				if (listener != null)
					return;

				accept_cb = new AsyncCallback (OnAccept);

				listener = new TcpListener (new IPEndPoint (IPAddress.Any, Configuration.UploadPort));
				listener.Start ();
				listener.BeginAcceptTcpClient (accept_cb, null);

				Logger.Log ("WebService successfully started upload listener on port {0}", ((IPEndPoint) listener.LocalEndpoint).Port);
			}
		}

		private static void OnAccept (IAsyncResult ares)
		{
			TcpClient accepted = null;
			try {
				accepted = listener.EndAcceptTcpClient (ares);
			} catch {
			} finally {
				try {
					// listen again
					listener.BeginAcceptTcpClient (accept_cb, null);
				} catch (Exception ex) {
					if (accepted != null) {
						Logger.Log ("Upload.OnAccept (): {0}", ex);
						accepted.Close ();
						throw;
					}
				}
			}

			if (accepted == null)
				return;

			ThreadPool.QueueUserWorkItem (ExecuteRequest, accepted);
		}

		private static string ReadString (BinaryReader reader, byte [] buffer, byte length)
		{
			reader.Read (buffer, 0, length);
			return UTF8Encoding.UTF8.GetString (buffer, 0, length);
		}

		private static void ExecuteRequest (object state)
		{
			NetworkStream stream = null;
			byte [] buffer;
			int id = counter++;
			string tmpfile = null;
			TcpClient client = null;
			BinaryReader reader = null;
			BinaryWriter writer = null;
			string remote_ip;
			WebServiceLogin login = new WebServiceLogin ();

			try {
				buffer = new byte [1024];

				client = (TcpClient) state;
				stream = client.GetStream ();
				stream.ReadTimeout = (int) TimeSpan.FromMinutes (5).TotalMilliseconds;
				stream.WriteTimeout = stream.ReadTimeout;
				reader = new BinaryReader (stream);
				writer = new BinaryWriter (stream);
				remote_ip = ((IPEndPoint) client.Client.RemoteEndPoint).Address.ToString ();

				/* Format:
				 * Field                   Length in bytes    Description
				 * version                       1                  1
				 * name_length                   1
				 * name                       <name_length>
				 * password_length               1
				 * password                 <password_length>
				 * work_id                       4                 The Work.id field in the database
				 * file_count                    2
				 * reserved                      8
				 *  
				 * < file #1>
				 * marker                       12               'MonkeyWrench'
				 * md5                          16
				 * flags                         1                  1 = compressed, 2 = hidden  
				 * filename_length               1
				 * filename               <filename_length> 
				 * <client waits for answer, either type 2 or 4, type 2: server already has data, type 4: server does not have data>        
				 * [type: 4] compressed_mime_length        1	
				 * [type: 4] compressed_mime   <compressed_mime_length>
				 * [type: 4] content_length      4
				 * [type: 4] content       <content_length>
				 * [type: 4] <client waits for response, type 2>
				 * 
				 * < file #2 >
				 * ...
				 * 
				 * The response data has this format:
				 * version                        1
				 * type                           1                  1 = everything OK, 2 = file received OK, 3 = error, 4 = send file
				 *  depending on type, here are the subsequent fields:
				 * [type: 3] message_length       2
				 * [type: 3] message          <message length>
				 * 
				 */
				byte version = reader.ReadByte ();
				byte name_length = reader.ReadByte ();
				login.User = ReadString (reader, buffer, name_length);
				byte password_length = reader.ReadByte ();
				login.Password = ReadString (reader, buffer, password_length);
				login.Ip4 = remote_ip;
				int work_id = reader.ReadInt32 ();
				ushort file_count = reader.ReadUInt16 ();
				reader.ReadInt64 ();

				Logger.Log ("Upload.ExecuteRequest (): {0} version: {1} work_id: {2} file count: {3} remote ip: {4}", id, version, work_id, file_count, client.Client.RemoteEndPoint.ToString ());

				using (DB db = new DB ()) {
					Authentication.VerifyUserInRole (remote_ip, db, login, Roles.BuildBot, true);
					for (ushort i = 0; i < file_count; i++) {
						byte [] md5 = new byte [16];
						byte flags;
						bool hidden;
						bool compressed;
						byte filename_length;
						byte compressed_mime_length;
						int content_length;
						string filename;
						string compressed_mime;
						string marker;
						
						marker = ReadString (reader, buffer, 12);
						if (marker != "MonkeyWrench")
							throw new Exception (string.Format ("Didn't get marker 'MonkeyWrench' at start of file, got '{0}'", marker));

						reader.Read (md5, 0, 16);

						flags = reader.ReadByte ();
						filename_length = reader.ReadByte ();

						filename = ReadString (reader, buffer, filename_length);

						hidden = (flags & 0x2) == 0x2;
						compressed = (flags & 0x1) == 0x1;

						Logger.Log ("Upload.ExecuteRequest (): {0} file #{1}: filename: '{2}' ", id, i + 1, filename);

						DBFile file = DBFile_Extensions.Find (db, FileUtilities.MD5BytesToString (md5));
						if (file == null) {
							Logger.Log ("Upload.ExecuteRequest (): {0} file #{1} must be sent, sending 'send file' response", id, i + 1);
							// Write 'send file'
							writer.Write ((byte) 1); // version
							writer.Write ((byte) 4); // type (4 = send file)
							writer.Flush ();

							compressed_mime_length = reader.ReadByte ();
							compressed_mime = ReadString (reader, buffer, compressed_mime_length);
							content_length = reader.ReadInt32 ();

							Logger.Log ("Upload.ExecuteRequest (): {0} file #{1} content_length: {2} compressed_mime: '{3}' reading...", id, i + 1, content_length, compressed_mime);
							
							int bytes_left = content_length;
							tmpfile = Path.GetTempFileName ();
							using (FileStream fs = new FileStream (tmpfile, FileMode.Open, FileAccess.Write, FileShare.Read)) {
								while (bytes_left > 0) {
									int to_read = Math.Min (bytes_left, buffer.Length);
									int read = reader.Read (buffer, 0, to_read);
									if (read == 0)
										throw new Exception (string.Format ("Failed to read {0} bytes, {1} bytes left", content_length, bytes_left));
									fs.Write (buffer, 0, read);
									bytes_left -= read;
								}
							}

							Logger.Log ("Upload.ExecuteRequest (): {0} file #{1} received, uploading '{2}' to database", id, i + 1, tmpfile);
							file = db.Upload (FileUtilities.MD5BytesToString (md5), tmpfile, filename, Path.GetExtension (filename), hidden, compressed_mime);
						} else {
							Logger.Log ("Upload.ExecuteRequest (): {0} file #{1} already in database, not uploading", id, i + 1);
						}

						DBWork work = DBWork_Extensions.Create (db, work_id);
						work.AddFile (db, file, filename, hidden);

						// Write 'file recieved OK'
						writer.Write ((byte) 1); // version
						writer.Write ((byte) 2); // type (2 = file received OK)
						writer.Flush ();
						Logger.Log ("Upload.ExecuteRequest (): {0} {1} uploaded successfully", id, filename);
					}
				}

				// Write 'everything OK'
				writer.Write ((byte) 1); // version
				writer.Write ((byte) 1); // type (1 = everything OK)
				writer.Flush ();
				Logger.Log ("Upload.ExecuteRequest (): {0} completed", id);
			} catch (Exception ex) {
				try {
					string msg = ex.ToString ();
					byte [] msg_buffer = UTF8Encoding.UTF8.GetBytes (msg);
					writer.Write ((byte) 1); // version
					writer.Write ((byte) 3); // type (3 = error)
					writer.Write ((ushort) Math.Min (msg_buffer.Length, ushort.MaxValue)); // message_length
					writer.Write (msg_buffer, 0, Math.Min (msg_buffer.Length, ushort.MaxValue)); // message
					stream.Flush ();
				} catch (Exception ex2) {
					Logger.Log ("Upload.ExecuteRequest (): {0} Failed to send exception to client: {1}", id, ex2.Message);
				}
				Logger.Log ("Upload.ExecuteRequest (): {0} {1}", id, ex);
			} finally {
				if (tmpfile != null)
					FileUtilities.TryDeleteFile (tmpfile);
				try {
					client.Close ();
				} catch {
					// Ignore 
				}
			}
		}
	}

}