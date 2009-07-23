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

				if (Configuration.CleanLargeObjects)
					result += DeleteUnusedLargeObjects ();

				if (Configuration.CompressFiles)
					result += CompressFiles ();

				if (Configuration.ExecuteDeletionDirectives)
					result += ExecuteDeletionDirectives ();

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
					//using (IDbTransaction transaction = db_save.Connection.BeginTransaction ()) {
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

		public static int DeleteUnusedLargeObjects ()
		{
			long bytes_deleted = 0;
			long bytes_in_db = 0;
			long bytes_of_files = 0;
			int files_deleted = 0;
			int largeobjects_deleted = 0;

			try {
				Console.WriteLine ("DeleteUnusedLargeObjects ()");
				using (DB db = new DB (true)) {
					using (DB db_obj = new DB (true)) {
						int min_lo = (int) (long) db.ExecuteScalar ("SELECT min (loid) FROM pg_largeobject;");
						int max_lo = (int) (long) db.ExecuteScalar ("SELECT max (loid) FROM pg_largeobject;");
						int page_size = 1000;

						Console.WriteLine ("Min id: {0}, max id: {1}", min_lo, max_lo);

						for (int start = min_lo; start < max_lo; start += (page_size + 1)) {
							Console.WriteLine ("Checking: {0} <= loid <= {1}", start, start + page_size);

							using (IDbCommand cmd = db.CreateCommand ()) {
								cmd.CommandText = @"
SELECT 
	pg_largeobject.loid, 
	File.filename AS file_filename, File.size, File.file_id,
	WorkFile.id AS workfile_id, WorkFile.filename AS workfile_filename,
	RevisionWork.state, 
	Revision.revision,
	Lane.lane,
	Host.host
FROM pg_largeobject 
LEFT JOIN File ON File.file_id = pg_largeobject.loid 
LEFT JOIN WorkFile ON File.id = WorkFile.file_id 
LEFT JOIN Work ON WorkFile.work_id = Work.id
LEFT JOIN RevisionWork ON RevisionWork.id = Work.revisionwork_id
LEFT JOIN Revision ON RevisionWork.revision_id = Revision.id
LEFT JOIN Lane ON Lane.id = RevisionWork.lane_id
LEFT JOIN Host ON RevisionWork.host_id = Host.id
WHERE loid >= @start AND loid <= @end AND pageno = 0
ORDER BY loid;
";

								DB.CreateParameter (cmd, "start", start.ToString ());
								DB.CreateParameter (cmd, "end", (start + page_size).ToString ());

								using (IDbTransaction transaction = db_obj.BeginTransaction ()) {
									using (IDataReader reader = cmd.ExecuteReader ()) {
										int prev_loid = -1;
										int workfile_count = 0;
										int large_object_size = 0;

										int workfile_id_idx = reader.GetOrdinal ("workfile_id");
										int workfile_filename_idx = reader.GetOrdinal ("workfile_filename");
										int loid_idx = reader.GetOrdinal ("loid");
										int file_filename_idx = reader.GetOrdinal ("file_filename");
										int size_idx = reader.GetOrdinal ("size");
										int revision_idx = reader.GetOrdinal ("revision");
										int lane_idx = reader.GetOrdinal ("lane");
										int host_idx = reader.GetOrdinal ("host");

										while (reader.Read ()) {
											int loid = (int) reader.GetInt64 (loid_idx);

											if (prev_loid != -1 && prev_loid != loid) {
												if (workfile_count == 0) {
													Console.WriteLine ("- //TODO: Delete file and large object.");
													bytes_deleted += large_object_size;
													files_deleted++;
													largeobjects_deleted++;
												}
												Console.WriteLine ("- Total: {0} workfiles.", workfile_count);

												workfile_count = 0;

												prev_loid = -1;
											}

											if (prev_loid == -1) {
												prev_loid = loid;
												large_object_size = 0; // (int) (long) db_obj.ExecuteScalar ("SELECT Sum (length (data)) FROM pg_largeobject WHERE loid = " + loid.ToString ());
												bytes_in_db += large_object_size;
												Console.WriteLine ("Found large object with loid: {0} and size: {1} bytes", loid, large_object_size);
											}

											bool has_file = !reader.IsDBNull (file_filename_idx);
											bool has_workfile = !reader.IsDBNull (workfile_id_idx);

											if (!has_file) {
												Console.WriteLine ("- This large object does not have an associated file.");
												Console.WriteLine ("- // TODO: Delete this large object.");
												bytes_deleted += large_object_size;
												largeobjects_deleted++;
												prev_loid = -1;
												continue;
											} else {
												large_object_size = reader.GetInt32 (size_idx);
											}

											int size = reader.GetInt32 (size_idx);

											if (workfile_count == 0)
												Console.WriteLine ("- File.size: {0}", size);

											if (has_workfile) {
												int workfile_id = reader.GetInt32 (workfile_id_idx);
												string workfile_filename = reader.GetString (workfile_filename_idx);
												string revision = reader.GetString (revision_idx);
												string lane = reader.GetString (lane_idx);
												string host = reader.GetString (host_idx);

												bytes_of_files += large_object_size;
												workfile_count++;

												Console.WriteLine ("- WorkFile: id={0}, lane={1}, host={2}, revision={3}, filename={4}",
													workfile_id, lane, host, revision, workfile_filename);

											}
										}
									}
								}
							}


							Console.WriteLine ("Partial summary ({0}/{1} = {2}%)", start + page_size - min_lo, max_lo - min_lo, 100 * (start + page_size - min_lo) / (double) (max_lo - min_lo));
							Console.WriteLine ("bytes deleted: {0}, bytes in db (before deleting): {1}, bytes of files: {2}, saved due to md5: {3}, files deleted: {4}, large objects deleted: {5}",
								bytes_deleted, bytes_in_db, bytes_of_files, bytes_of_files - bytes_in_db, files_deleted, largeobjects_deleted);
						}
					}
				}
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
			} finally {
				Console.WriteLine ("Summary:");
				Console.WriteLine ("bytes deleted: {0}, bytes in db (before deleting): {1}, bytes of files: {2}, saved due to md5: {3}, files deleted: {4}, large objects deleted: {5}",
								bytes_deleted, bytes_in_db, bytes_of_files, bytes_of_files - bytes_in_db, files_deleted, largeobjects_deleted);
			}
			return 0;
		}

		public static int ExecuteDeletionDirectives ()
		{
			long space_recovered = 0;

			LogWithTime ("ExecuteDeletionDirectives [START]");
			using (DB db = new DB (true)) {
				List<DBLane> lanes = db.GetAllLanes ();
				foreach (DBLane lane in lanes) {
					LogWithTime ("ExecuteDeletionDirectives: Lane = {0} {1}", lane.id, lane.lane);
					List<DBLaneDeletionDirectiveView> directives = DBLaneDeletionDirectiveView_Extensions.Find (db, lane);
					foreach (DBLaneDeletionDirectiveView directive in directives) {
						LogWithTime ("ExecuteDeletionDirectives: Found directive: '{0}' Enabled: {1}, Condition: {2}, Filename: '{3}', MatchMode: {4}, X: {5}",
							directive.name, directive.enabled, directive.Condition, directive.filename, directive.MatchMode, directive.x);
						if (!directive.enabled)
							continue;

						string sql = @"
SELECT 
	WorkFile.id AS workfile_id,
	WorkFile.filename AS workfile_filename,
	File.id AS file_id,
	File.file_id AS file_file_id,
	File.md5,
	File.size,
	Revision.revision
FROM 
	WorkFile
INNER JOIN Work ON Work.id = WorkFile.work_id
INNER JOIN File ON WorkFile.file_id = File.id	
INNER JOIN RevisionWork ON RevisionWork.id = Work.revisionwork_id
INNER JOIN Revision ON Revision.id = RevisionWork.revision_id
WHERE
	RevisionWork.lane_id = @lane_id
	AND RevisionWork.completed = TRUE
";
						switch (directive.Condition) {
						case DBDeleteCondition.AfterXBuiltRevisions:
							sql += string.Format (@" 
AND Revision.id NOT IN (
	SELECT Revision.id
	FROM Revision
	INNER JOIN RevisionWork ON Revision.id = RevisionWork.revision_id
	WHERE 
		RevisionWork.lane_id = @lane_id 
		AND Revision.lane_id = @lane_id
		AND RevisionWork.completed = TRUE
	ORDER BY Revision.date DESC
	LIMIT {0}
)
", (int) directive.x);
							break;
						case DBDeleteCondition.AfterXDays:
							sql += string.Format (@"
AND Work.endtime + interval '{0} days' < now ();
", (int) directive.x);
							break;
						default:
							continue;
						}

						using (IDbCommand cmd = db.CreateCommand ()) {
							cmd.CommandText = sql;
							DB.CreateParameter (cmd, "lane_id", lane.id);
							using (IDataReader reader = cmd.ExecuteReader ()) {
								using (DB write_db = new DB (true)) {
									while (reader.Read ()) {
										int workfile_id = reader.GetInt32 (reader.GetOrdinal ("workfile_id"));
										string workfile_filename = reader.GetString (reader.GetOrdinal ("workfile_filename"));
										int file_file_id = reader.GetInt32 (reader.GetOrdinal ("file_file_id"));
										int file_id = reader.GetInt32 (reader.GetOrdinal ("file_id"));
										int size = reader.GetInt32 (reader.GetOrdinal ("size"));
										string md5 = reader.GetString (reader.GetOrdinal ("md5"));
										bool match = directive.IsFileNameMatch (workfile_filename);

										if (!match)
											continue;

										LogWithTime ("ExecuteDeletionDirectives:  >Processing: workfile_id: {0}, workfile_filename: '{1}', file_id: {2}, md5: {3}, match: {4}", workfile_id, workfile_filename, file_id, md5, match);

										// delete the work file
										DBRecord_Extensions.Delete (write_db, workfile_id, DBWorkFile.TableName);
										LogWithTime ("ExecuteDeletionDirectives:  >>WorkFile {0} deleted succesfully.", workfile_id);

										// try to delete the file too
										try {
											DBFile_Extensions.Delete (write_db, file_id, file_file_id);
											space_recovered += size;
											LogWithTime ("ExecuteDeletionDirectives:  >>File {0} deleted successfully. Recovered {1} bytes (total {2} bytes).", file_id, size, space_recovered);
										} catch (Exception ex) {
											LogWithTime ("ExecuteDeletionDirectives:  >>Could not delete File (since the File is used somewhere else, this is normal): {0}", ex.Message);
										}
									}
								}
							}
						}
					}
				}
			}

			LogWithTime ("ExecuteDeletionDirectives: Deleted {0} bytes ({1:#0.0} Kb, {2:#0.00} Mb, {3:#0.000} Gb, {4:#0.0000} Tb)",
				space_recovered, space_recovered / (double) 1024, space_recovered / (double) (1024 * 1024), space_recovered / (double) (1024 * 1024 * 1024), space_recovered / (double) (1024 * 1024 * 1024 * 1024L));
			LogWithTime ("ExecuteDeletionDirectives: [DONE]");

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
									byte [] buffer = new byte [1024];
									string tmpfile = null;

									if (!string.IsNullOrEmpty (revision.diff)) {
										int length = 0;
										if (revision.diff_file_id == null) {
											try {
												length = revision.diff.Length;
												tmpfile = Path.GetTempFileName ();
												File.WriteAllText (tmpfile, revision.diff);
												DBFile diff = download_db.Upload (tmpfile, ".log", false);
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
												DBFile log = download_db.Upload (tmpfile, ".log", false);
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

