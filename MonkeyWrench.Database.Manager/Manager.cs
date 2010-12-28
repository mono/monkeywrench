/*
 * Manager.cs
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
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Text;

using Npgsql;
using NpgsqlTypes;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Database.Manager
{
	class Manager
	{
		public static int Main (string [] args)
		{
			int result = 0;

			try {
				if (!Configuration.LoadConfiguration (args))
					return 1;

				if (Configuration.CompressFiles)
					result += CompressFiles ();

				if (Configuration.MoveFilesToDatabase)
					result += MoveFilesToDatabase ();

				if (Configuration.MoveFilesToFileSystem)
					result += MoveFilesToFileSystem ();

				return result;
			} catch (Exception ex) {
				Console.WriteLine ();
				Console.WriteLine ("Unhandled exception:");
				Console.WriteLine (ex);
				return 1;
			}
		}

		public static int CompressFiles ()
		{
			byte [] buffer = new byte [1024];
			int read;
			long saved_space = 0;

			using (DB db = new DB (true)) {
				using (DB db_save = new DB (true)) {
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = @"
SELECT File.*
FROM File
WHERE (File.compressed_mime = '' OR File.compressed_mime IS NULL) 
	AND File.size <> 0 
	AND File.id IN 
		(SELECT WorkFile.file_id FROM WorkFile WHERE WorkFile.file_id = File.id)
LIMIT 10 
";
						using (IDataReader reader = cmd.ExecuteReader ()) {
							while (reader.Read ()) {
								DBFile file = new DBFile (reader);
								long srclength;
								long destlength = -1;
								string tmpfile = Path.GetTempFileName ();
								string tmpfilegz;

								Console.Write ("Downloading {0} = {1} with size {2}... ", file.id, file.filename, file.size);

								using (Stream stream_reader = db_save.Download (file)) {
									using (FileStream stream_writer = new FileStream (tmpfile, FileMode.Create, FileAccess.Write)) {
										while (0 < (read = stream_reader.Read (buffer, 0, buffer.Length))) {
											stream_writer.Write (buffer, 0, read);
										}
									}
								}

								srclength = new FileInfo (tmpfile).Length;
								Console.Write ("Compressing file {0} with size {1}... ", tmpfile, srclength);

								tmpfilegz = FileUtilities.GZCompress (tmpfile);

								if (tmpfilegz == null) {
									Console.WriteLine ("Compression didn't succeed.");
								} else {
									destlength = new FileInfo (tmpfilegz).Length;
									Console.WriteLine ("Success, compressed size: {0} ({1}%)", destlength, 100 * (double) destlength / (double) srclength);

									using (IDbTransaction transaction = db_save.BeginTransaction ()) {
										// Upload the compressed file. 
										// Npgsql doesn't seem to have a way to truncate a large object,
										// so we just create a new large object and delete the old one.
										int file_id = file.file_id.Value;
										int gzfile_id = db_save.Manager.Create (LargeObjectManager.READWRITE);
										LargeObject gzfile = db_save.Manager.Open (gzfile_id, LargeObjectManager.READWRITE);

										using (FileStream st = new FileStream (tmpfilegz, FileMode.Open, FileAccess.Read, FileShare.Read)) {
											while (0 < (read = st.Read (buffer, 0, buffer.Length)))
												gzfile.Write (buffer, 0, read);
										}
										gzfile.Close ();

										// Save to our File record
										file.file_id = gzfile_id;
										file.compressed_mime = "application/x-gzip";
										file.Save (db_save);

										// Delete the old large object
										db_save.Manager.Delete (file_id);

										transaction.Commit ();

										saved_space += (srclength - destlength);
									}
								}

								if (File.Exists (tmpfilegz)) {
									try {
										File.Delete (tmpfilegz);
									} catch {
										// Ignore
									}
								}
								if (File.Exists (tmpfile)) {
									try {
										File.Delete (tmpfile);
									} catch {
										// Ignore
									}
								}
							}
						}
						//}
					}
				}
			}

			Console.WriteLine ("Saved {0} bytes.", saved_space);

			return 0;
		}

		public static int MoveFilesToDatabase ()
		{
			Console.Error.WriteLine ("MoveFilesToDatabase: Not implemented.");
			return 1;
		}

		public static int MoveFilesToFileSystem ()
		{
			long moved_bytes = 0;

			LogWithTime ("MoveFilesToFileSystem: [START]");

			using (DB db = new DB ()) {
				using (DB download_db = new DB ()) {
					while (true) {
						using (IDbCommand cmd = db.CreateCommand ()) {
							// execute this in chunks to avoid huge data transfers and slowdowns.
							cmd.CommandText = "SELECT * FROM File WHERE NOT file_id IS NULL LIMIT 100";
							using (IDataReader reader = cmd.ExecuteReader ()) {
								if (!reader.Read ())
									break;

								do {
									DBFile file = new DBFile (reader);
									byte [] buffer = new byte [1024];
									int oid = file.file_id.Value;
									int read;
									string fn = FileUtilities.CreateFilename (file.md5, file.compressed_mime == MimeTypes.GZ, true);
									using (FileStream writer = new FileStream (fn, FileMode.Create, FileAccess.Write, FileShare.Read)) {
										using (Stream str = download_db.Download (file)) {
											while ((read = str.Read (buffer, 0, buffer.Length)) != 0)
												writer.Write (buffer, 0, read);
										}
									}

									IDbTransaction transaction = download_db.BeginTransaction ();
									download_db.Manager.Delete (oid);
									file.file_id = null;
									file.Save (download_db);
									transaction.Commit ();

									moved_bytes += file.size;
									LogWithTime ("MoveFilesToFileSystem: Moved oid {0} to {1} ({2} bytes, {3} total bytes moved)", oid, fn, file.size, moved_bytes);
								} while (reader.Read ());
							}
						}
					}

					while (true) {
						using (IDbCommand cmd = db.CreateCommand ()) {
							// execute this in chunks to avoid huge data transfers and slowdowns.
							cmd.CommandText = "SELECT * FROM Revision WHERE (diff_file_id IS NULL AND NOT diff = '') OR (log_file_id IS NULL AND NOT log = '') LIMIT 100";
							using (IDataReader reader = cmd.ExecuteReader ()) {
								if (!reader.Read ())
									break;

								do {
									DBRevision revision = new DBRevision (reader);
									string tmpfile = null;

									if (!string.IsNullOrEmpty (revision.diff)) {
										int length = 0;
										if (revision.diff_file_id == null) {
											try {
												length = revision.diff.Length;
												tmpfile = Path.GetTempFileName ();
												File.WriteAllText (tmpfile, revision.diff);
												DBFile diff = download_db.Upload (tmpfile, ".log", false, null);
												revision.diff_file_id = diff.id;
												revision.diff = null;
											} finally {
												try {
													if (File.Exists (tmpfile))
														File.Delete (tmpfile);
												} catch {
													// ignore exceptions here
												}
											}
											moved_bytes += length;
											LogWithTime ("MoveFilesToFileSystem: Moved revision {0}'s diff to db/filesystem ({1} bytes, {2} total bytes moved)", revision.id, length, moved_bytes);
										}
									}

									if (!string.IsNullOrEmpty (revision.log)) {
										int length = 0;
										if (revision.log_file_id == null) {
											try {
												length = revision.log.Length;
												tmpfile = Path.GetTempFileName ();
												File.WriteAllText (tmpfile, revision.log);
												DBFile log = download_db.Upload (tmpfile, ".log", false, null);
												revision.log_file_id = log.id;
												revision.log = null;
											} finally {
												try {
													if (File.Exists (tmpfile))
														File.Delete (tmpfile);
												} catch {
													// ignore exceptions here
												}
											}
											moved_bytes += length;
											LogWithTime ("MoveFilesToFileSystem: Moved revision {0}'s log to db/filesystem ({1} bytes, {2} total bytes moved)", revision.id, length, moved_bytes);
										}
										revision.log = null;
									}

									revision.Save (download_db);
								} while (reader.Read ());
							}
						}
					}
				}
			}

			LogWithTime ("MoveFilesToFileSystem: [Done]");

			return 0;
		}

		private static void LogWithTime (string message, params object [] args)
		{
			Logger.Log (message, args);
		}
	}
}

