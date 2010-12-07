/*
 * DB.cs
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
using System.IO;
using System.Text;

using Npgsql;
using NpgsqlTypes;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;

namespace MonkeyWrench
{
	public class DB : IDisposable, IDB
	{
		NpgsqlConnection dbcon;
		LargeObjectManager manager;
		TimeSpan db_time_difference;

		public LargeObjectManager Manager
		{
			get
			{
				if (manager == null)
					manager = new LargeObjectManager (dbcon);
				return manager;
			}
		}

		/// <summary>
		/// Creates a command with a default timeout of 300 seconds.
		/// </summary>
		/// <returns></returns>
		public IDbCommand CreateCommand ()
		{
			return CreateCommand (TimeSpan.FromSeconds (300));
		}

		/// <summary>
		/// Creates a command with the specified timeout. A timeout of 0 means infinity.
		/// </summary>
		/// <param name="Timeout"></param>
		/// <returns></returns>
		public IDbCommand CreateCommand (TimeSpan Timeout)
		{
			NpgsqlCommand result = dbcon.CreateCommand ();
			result.CommandTimeout = (int) Timeout.TotalSeconds;
			return result;
		}

		public IDbTransaction BeginTransaction ()
		{
			return dbcon.BeginTransaction ();
		}

		public DB ()
		{
			Connect ();
		}

		public DB (bool Connect)
		{
			if (Connect)
				this.Connect ();
		}

		public static void CreateParameter (IDbCommand cmd, string name, object value)
		{
			DBRecord.CreateParameter (cmd, name, value);
		}

		private void Connect ()
		{
			try {
				string connectionString;

				connectionString = "Server=" + Configuration.DatabaseHost + ";";
				connectionString += "Database=builder;User ID=builder;";

				if (!string.IsNullOrEmpty (Configuration.DatabasePort))
					connectionString += "Port=" + Configuration.DatabasePort + ";";

				dbcon = new NpgsqlConnection (connectionString);

				Logger.Log (2, "Database connection string: {0}", connectionString);

				dbcon.Open ();

				object db_now_obj = ExecuteScalar ("SELECT now();");
				DateTime db_now;
				DateTime machine_now = DateTime.Now;
				const string format = "yyyy/MM/dd HH:mm:ss.ffff";

				if (db_now_obj is DateTime) {
					db_now = (DateTime) db_now_obj;
				} else {
					Logger.Log ("now () function return value of type: {0}", db_now_obj == null ? "null" : db_now_obj.GetType ().FullName);
					db_now = machine_now;
				}

				db_time_difference = db_now - machine_now;

				Logger.Log (2, "DB now: {0}, current machine's now: {1}, adjusted now: {3}, diff: {2} ms", db_now.ToString (format), machine_now.ToString (format), db_time_difference.TotalMilliseconds, Now.ToString (format));
			} catch {
				if (dbcon != null) {
					dbcon.Dispose ();
					dbcon = null;
				}
				throw;
			}
		}

		public void Dispose ()
		{
			if (dbcon != null) {
				dbcon.Close ();
				dbcon = null;
			}
		}

		private class DBFileStream : Stream
		{
			IDbTransaction transaction = null;
			LargeObject obj;

			public DBFileStream (DBFile file, DB db)
			{
				try {
					transaction = db.dbcon.BeginTransaction ();
					obj = db.Manager.Open (file.file_id.Value);
				} catch {
					if (transaction != null) {
						transaction.Rollback ();
						transaction = null;
					}
				}
			}

			protected override void Dispose (bool disposing)
			{
				base.Dispose (disposing);

				if (transaction != null) {
					transaction.Rollback ();
					transaction = null;
				}
			}

			public override int Read (byte [] buffer, int offset, int count)
			{
				return obj.Read (buffer, offset, count);
			}

			public override void Write (byte [] buffer, int offset, int count)
			{
				obj.Write (buffer, offset, count);
			}

			public override long Seek (long offset, SeekOrigin origin)
			{
				throw new NotImplementedException ();
			}

			public override void SetLength (long value)
			{
				throw new NotImplementedException ();
			}

			public override void Flush ()
			{
				// nop
			}

			public override long Position
			{
				get
				{
					return obj.Tell ();
				}
				set
				{
					throw new NotImplementedException ();
				}
			}

			public override bool CanRead
			{
				get { return true; }
			}

			public override bool CanWrite
			{
				get { return true; }
			}

			public override bool CanSeek
			{
				get { return false; }
			}

			public override long Length
			{
				get { return obj.Size (); }
			}
		}

		public Stream Download (DBWorkFile file)
		{
			return new DBFileStream (DBFile_Extensions.Create (this, file.file_id), this);
		}

		public Stream Download (DBFile file)
		{
			if (file.file_id.HasValue) {
				return new DBFileStream (file, this);
			} else {
				return new System.IO.Compression.GZipStream (new FileStream (DBFile_Extensions.GetFullPath (file.md5), FileMode.Open, FileAccess.Read), System.IO.Compression.CompressionMode.Decompress);
			}
		}

		public Stream Download (DBWorkFileView file)
		{
			return new DBFileStream (DBFile_Extensions.Create (this, file.file_id), this);
		}

		public int GetSize (int file_id)
		{
			using (IDbCommand cmd = CreateCommand ()) {
				object o = ExecuteScalar ("SELECT file_id FROM File where File.id = " + file_id.ToString ());

				if (!(o is int))
					throw new Exception ("File_id doesn't exist.");

				return GetLargeObjectSize ((int) o);
			}
		}

		public int GetLargeObjectSize (int oid)
		{
			Console.WriteLine ("GetLargeObjectSize ({0})", oid);
			using (IDbTransaction transaction = BeginTransaction ()) {
				int result;
				LargeObject obj = Manager.Open (oid);
				result = obj.Size ();
				obj.Close ();
				return result;
			}
		}

		public DBFile UploadString (string contents, string extension, bool hidden)
		{
			string tmpfile = null;
			try {
				tmpfile = Path.GetTempFileName ();
				File.WriteAllText (tmpfile, contents);
				return Upload (tmpfile, extension, hidden, null);
			} finally {
				try {
					File.Delete (tmpfile);
				} catch {
					// ignore exceptions
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Filename"></param>
		/// <returns></returns>
		public DBFile Upload (string filename, string extension, bool hidden, string compressed_mime)
		{
			string md5;

			// first check if the file is already in the database
			using (FileStream st = new FileStream (filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				md5 = FileUtilities.CalculateMD5 (st);
			}

			return Upload (md5, filename, filename, extension, hidden, compressed_mime);
		}
		
		public DBFile Upload (string md5, string path_to_contents, string filename, string extension, bool hidden, string compressed_mime)
		{
			IDbTransaction transaction = null;
			LargeObjectManager manager;
			LargeObject obj;
			int? oid;
			DBFile result;
			long filesize;
			string gzFilename = null;

			try {
				filesize = new FileInfo (path_to_contents).Length;
				if (filesize > 1024 * 1024 * 100)
					throw new Exception ("Max file size is 100 MB");

				using (IDbCommand cmd = CreateCommand ()) {
					cmd.CommandText = "SELECT * FROM File WHERE md5 = '" + md5 + "'";
					using (IDataReader reader = cmd.ExecuteReader ()) {
						if (reader.Read ())
							return new DBFile (reader);
					}
				}

				//Console.WriteLine ("Uploading {0} {1} with compressed mime: {2}", Filename, md5, compressed_mime);

				// The file is not in the database
				// Note: there is a race condition here,
				// the same file might get added to the db before we do it here.
				// not quite sure how to deal with that except retrying the above if the insert below fails.

				if (compressed_mime == MimeTypes.GZ) {
					gzFilename = path_to_contents;
				} else {
					gzFilename = FileUtilities.GZCompress (path_to_contents);
					compressed_mime = MimeTypes.GZ;
				}

				transaction = BeginTransaction ();

				if (Configuration.StoreFilesInDB) {
					manager = new LargeObjectManager (this.dbcon);
					oid = manager.Create (LargeObjectManager.READWRITE);
					obj = manager.Open (oid.Value, LargeObjectManager.READWRITE);

					using (FileStream st = new FileStream (gzFilename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
						byte [] buffer = new byte [1024];
						int read = -1;
						while (read != 0) {
							read = st.Read (buffer, 0, buffer.Length);
							obj.Write (buffer, 0, read);
						}
					}
					obj.Close ();
				} else {
					oid = null;
					string fn = FileUtilities.CreateFilename (md5, true, true);

					File.Copy (gzFilename, fn, true);
					Logger.Log ("Saved file to: {0}", fn);
				}

				result = new DBFile ();
				result.file_id = oid;
				result.filename = Path.GetFileName (filename);
				result.md5 = md5;
				result.size = (int) filesize;
				result.hidden = hidden;
				switch (extension.ToLower ()) {
				case ".log":
				case ".stdout":
				case ".stderr":
					result.mime = MimeTypes.LOG;
					break;
				case ".txt":
					result.mime = MimeTypes.TXT;
					break;
				case ".htm":
				case ".html":
					result.mime = MimeTypes.HTML;
					break;
				case ".png":
					result.mime = MimeTypes.PNG;
					break;
				case ".jpg":
					result.mime = MimeTypes.JPG;
					break;
				case ".bmp":
					result.mime = MimeTypes.BMP;
					break;
				case ".tar":
					result.mime = MimeTypes.TAR;
					break;
				case ".bz":
					result.mime = MimeTypes.BZ;
					break;
				case ".bz2":
					result.mime = MimeTypes.BZ2;
					break;
				case ".zip":
					result.mime = MimeTypes.ZIP; ;
					break;
				case ".gz":
					result.mime = MimeTypes.GZ;
					break;
				case ".xpi":
					result.mime = MimeTypes.XPI;
					break;
				case ".crx":
					result.mime = MimeTypes.CRX;
					break;
				default:
					result.mime = MimeTypes.OCTET_STREAM;
					break;
				}
				result.compressed_mime = compressed_mime;
				result.Save (this);

				transaction.Commit ();
				transaction = null;

				return result;
			} finally {
				FileUtilities.TryDeleteFile (gzFilename);

				if (transaction != null)
					transaction.Rollback ();
			}
		}

		public int ExecuteNonQuery (string sql)
		{
			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = sql;
				return cmd.ExecuteNonQuery ();
			}
		}

		public object ExecuteScalar (string sql)
		{
			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = sql;
				return cmd.ExecuteScalar ();
			}
		}

		public DBLane CloneLane (int lane_id, string new_name, bool copy_files)
		{
			DBLane result = null;
			DBLane master = DBLane_Extensions.Create (this, lane_id);

			if (this.LookupLane (new_name, false) != null)
				throw new Exception (string.Format ("The lane '{0}' already exists.", new_name));

			try {
				using (IDbTransaction transaction = BeginTransaction ()) {
					result = new DBLane ();
					result.lane = new_name;
					result.max_revision = master.max_revision;
					result.min_revision = master.min_revision;
					result.repository = master.repository;
					result.source_control = master.source_control;
					result.parent_lane_id = master.parent_lane_id;
					result.Save (this);

					foreach (DBLanefile filemaster in master.GetFiles (this)) {
						int fid;

						if (copy_files) {
							DBLanefile clone = new DBLanefile ();
							clone.contents = filemaster.contents;
							clone.mime = filemaster.mime;
							clone.name = filemaster.name;
							clone.Save (this);
							fid = clone.id;
						} else {
							fid = filemaster.id;
						}

						DBLanefiles lane_files = new DBLanefiles ();
						lane_files.lane_id = result.id;
						lane_files.lanefile_id = fid;
						lane_files.Save (this);
					}

					foreach (DBCommand cmdmaster in master.GetCommands (this)) {
						DBCommand clone = new DBCommand ();
						clone.lane_id = result.id;
						clone.alwaysexecute = cmdmaster.alwaysexecute;
						clone.arguments = cmdmaster.arguments;
						clone.command = cmdmaster.command;
						clone.filename = cmdmaster.filename;
						clone.nonfatal = cmdmaster.nonfatal;
						clone.sequence = cmdmaster.sequence;
						clone.timeout = cmdmaster.timeout;
						clone.working_directory = cmdmaster.working_directory;
						clone.upload_files = cmdmaster.upload_files;
						clone.Save (this);
					}

					foreach (DBHostLaneView hostlanemaster in master.GetHosts (this)) {
						DBHostLane clone = new DBHostLane ();
						clone.enabled = false;
						clone.lane_id = result.id;
						clone.host_id = hostlanemaster.host_id;
						clone.Save (this);
					}

					foreach (DBEnvironmentVariable env in master.GetEnvironmentVariables (this)) {
						DBEnvironmentVariable clone = new DBEnvironmentVariable ();
						clone.host_id = env.host_id;
						clone.lane_id = result.id;
						clone.name = env.name;
						clone.value = env.value;
						clone.Save (this);
					}

					transaction.Commit ();
				}
			} catch {
				result = null;
				throw;
			}

			return result;
		}

		public DBLane LookupLane (string lane, bool throwOnError)
		{
			DBLane result = null;
			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Lane WHERE lane = @lane";
				DB.CreateParameter (cmd, "lane", lane);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ()) {
						if (!throwOnError)
							return null;
						throw new Exception (string.Format ("Could not find the lane '{0}'.", lane));
					}
					result = new DBLane (reader);
					if (reader.Read ()) {
						if (!throwOnError)
							return null;
						throw new Exception (string.Format ("Found more than one lane named '{0}'.", lane));
					}
				}
			}
			return result;
		}

		public DBLane LookupLane (string lane)
		{
			return LookupLane (lane, true);
		}

		public DBHost LookupHost (string host)
		{
			return LookupHost (host, true);
		}

		public DBHost LookupHost (string host, bool throwOnError)
		{
			DBHost result = null;
			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Host WHERE host = @host";
				DB.CreateParameter (cmd, "host", host);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ()) {
						if (!throwOnError)
							return null;
						throw new Exception (string.Format ("Could not find the host '{0}'.", host));
					}
					result = new DBHost (reader);
					if (reader.Read ()) {
						if (!throwOnError)
							return null;
						throw new Exception (string.Format ("Found more than one host named '{0}'.", host));
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Returns all the lanes in the database.
		/// </summary>
		/// <returns></returns>
		public List<DBLane> GetAllLanes ()
		{
			List<DBLane> result = new List<DBLane> ();

			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Lane ORDER BY lane";
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (new DBLane (reader));
					}
				}
			}

			return result;
		}

		public List<DBHost> GetHosts ()
		{
			List<DBHost> result = new List<DBHost> ();

			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Host ORDER BY host";
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (new DBHost (reader));
					}
				}
			}

			return result;
		}

		public List<DBHostLane> GetAllHostLanes ()
		{
			List<DBHostLane> result = new List<DBHostLane> ();

			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM HostLane ORDER BY host_id, lane_id";
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (new DBHostLane (reader));
					}
				}
			}

			return result;
		}

		public List<DBHost> GetHostsForLane (int lane_id)
		{
			List<DBHost> result = new List<DBHost> ();

			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT *, HostLane.lane_id AS lane_id FROM Host INNER JOIN HostLane ON Host.id = HostLane.host_id WHERE lane_id = @lane_id";
				DB.CreateParameter (cmd, "lane_id", lane_id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (new DBHost (reader));
					}
				}
			}

			return result;
		}

		public List<DBLane> GetLanesForHost (int host_id, bool only_enabled)
		{
			List<DBLane> result = new List<DBLane> ();

			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT *, HostLane.host_id AS host_id, HostLane.enabled AS lane_enabled FROM Lane INNER JOIN HostLane ON Lane.id = HostLane.lane_id WHERE host_id = @host_id ";
				if (only_enabled)
					cmd.CommandText += " AND HostLane.enabled = true;";
				DB.CreateParameter (cmd, "host_id", host_id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (new DBLane (reader));
					}
				}
			}

			return result;
		}
		/// <summary>
		/// Returns all the lanes for which there are revisions in the database
		/// </summary>
		/// <returns></returns>
		public List<string> GetLanes ()
		{
			List<string> result = new List<string> ();

			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT DISTINCT lane FROM Revision";
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (reader.GetString (0));
				}
			}

			return result;
		}

		public List<DBCommand> GetCommands (int lane_id)
		{
			List<DBCommand> result = new List<DBCommand> ();

			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Command ";
				if (lane_id > 0) {
					cmd.CommandText += "WHERE lane_id = @lane_id ";
					DB.CreateParameter (cmd, "lane_id", lane_id);
				}
				cmd.CommandText += "ORDER BY sequence ASC";
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBCommand (reader));
				}
			}

			return result;
		}

		public Dictionary<string, DBRevision> GetDBRevisions (int lane_id)
		{
			Dictionary<string, DBRevision> result = new Dictionary<string, DBRevision> ();

			foreach (DBRevision rev in GetDBRevisions (lane_id, 0)) {
				result.Add (rev.revision, rev);
			}

			return result;
		}

		public List<DBRevision> GetDBRevisions (int lane_id, int limit)
		{
			return GetDBRevisions (lane_id, limit, 0);
		}

		public List<DBRevision> GetDBRevisions (int lane_id, int limit, int offset)
		{
			List<DBRevision> result = new List<DBRevision> ();

			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Revision WHERE lane_id = @lane_id ORDER BY date DESC";
				if (limit > 0)
					cmd.CommandText += " LIMIT " + limit.ToString ();
				if (offset > 0)
					cmd.CommandText += " OFFSET " + offset.ToString ();
				DB.CreateParameter (cmd, "lane_id", lane_id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (new DBRevision (reader));
					}
				}
			}

			return result;
		}

		public List<int> GetRevisions (string lane, int limit)
		{
			List<int> result = new List<int> ();

			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT DISTINCT CAST (revision as int) FROM revisions WHERE lane = @lane ORDER BY revision DESC";
				if (limit > 0)
					cmd.CommandText += " LIMIT " + limit.ToString ();
				DB.CreateParameter (cmd, "lane", lane);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (reader.GetInt32 (0));
				}
			}

			return result;
		}

		public void ClearAllWork (int lane_id, int host_id)
		{
			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "UPDATE Work SET state = 0, summary = '' " +
						"WHERE lane_id = @lane_id " +
							"AND host_id = @host_id;";
				DB.CreateParameter (cmd, "lane_id", lane_id);
				DB.CreateParameter (cmd, "host_id", host_id);
				cmd.ExecuteNonQuery ();
			}
		}

		public void ClearWork (int lane_id, int revision_id, int host_id)
		{
			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = @"
UPDATE 
	Work SET state = DEFAULT, summary = DEFAULT, starttime = DEFAULT, endtime = DEFAULT, duration = DEFAULT, logfile = DEFAULT, host_id = DEFAULT
WHERE
	Work.revisionwork_id IN 
		(SELECT	RevisionWork.id 
			FROM RevisionWork
			WHERE RevisionWork.host_id = @host_id AND RevisionWork.lane_id = @lane_id AND RevisionWork.revision_id = @revision_id);

UPDATE 
	RevisionWork SET state = DEFAULT, lock_expires = DEFAULT, completed = DEFAULT, workhost_id = DEFAULT
WHERE 
		lane_id = @lane_id
	AND revision_id = @revision_id 
	AND host_id = @host_id;
";
				DB.CreateParameter (cmd, "lane_id", lane_id);
				DB.CreateParameter (cmd, "revision_id", revision_id);
				DB.CreateParameter (cmd, "host_id", host_id);
				cmd.ExecuteNonQuery ();
			}
		}

		/// <summary>
		/// Deletes all the files related to the work in the revision 'revision_id' of lane 'lane' on the host 'host'.
		/// </summary>
		/// <param name="host"></param>
		/// <param name="lane"></param>
		/// <param name="revision_id"></param>
		public void DeleteFiles (int host_id, int lane_id, int revision_id)
		{
			using (IDbTransaction transaction = BeginTransaction ()) {
				using (IDbCommand cmd = CreateCommand ()) {
					cmd.CommandText = @"
SELECT WorkFile.id AS id
	INTO TEMP WorkFile_delete_tmpfile 
	FROM WorkFile 
	INNER JOIN Work ON Work.id = WorkFile.work_id
	INNER JOIN RevisionWork ON RevisionWork.id = Work.revisionwork_id
	WHERE
		RevisionWork.lane_id = @lane_id AND
		RevisionWork.host_id = @host_id AND
		RevisionWork.revision_id = @revision_id;
		
DELETE FROM WorkFile
WHERE id IN (select * from WorkFile_delete_tmpfile);

	DROP TABLE WorkFile_delete_tmpfile;
";
					DB.CreateParameter (cmd, "lane_id", lane_id);
					DB.CreateParameter (cmd, "host_id", host_id);
					DB.CreateParameter (cmd, "revision_id", revision_id);
					cmd.ExecuteNonQuery ();
					transaction.Commit ();
				}
			}
		}

		public void DeleteAllWork (int lane_id, int host_id)
		{
			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "DELETE FROM Work WHERE lane_id = @lane_id AND host_id = @host_id;";
				DB.CreateParameter (cmd, "lane_id", lane_id);
				DB.CreateParameter (cmd, "host_id", host_id);
				cmd.ExecuteNonQuery ();
			}
			//TODO: Directory.Delete(Configuration.GetDataRevisionDir(lane, revision), true);
		}

		public void DeleteWork (int lane_id, int revision_id, int host_id)
		{
			using (IDbCommand cmd = CreateCommand ()) {
				//				cmd.CommandText = "DELETE FROM Work WHERE lane_id = @lane_id AND revision_id = @revision_id AND host_id = @host_id;";
				cmd.CommandText = @"
DELETE FROM Work 
WHERE Work.revisionwork_id = 
	(SELECT id 
	 FROM RevisionWork 
	 WHERE		lane_id = @lane_id 
			AND revision_id = @revision_id 
			AND host_id = @host_id
	);
UPDATE RevisionWork SET state = 10 WHERE lane_id = @lane_id AND host_id = @host_id AND revision_id = @revision_id;";
				DB.CreateParameter (cmd, "lane_id", lane_id);
				DB.CreateParameter (cmd, "revision_id", revision_id);
				DB.CreateParameter (cmd, "host_id", host_id);
				cmd.ExecuteNonQuery ();
			}
			//TODO: Directory.Delete(Configuration.GetDataRevisionDir(lane, revision), true);
		}

		public DBRevision GetRevision (string lane, int revision)
		{
			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT * from revisions where lane = @lane AND revision = @revision";
				DB.CreateParameter (cmd, "lane", lane);
				DB.CreateParameter (cmd, "revision", revision.ToString ());
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						return null;
					if (reader.IsDBNull (0))
						return null;

					DBRevision rev = new DBRevision ();
					rev.Load (reader);
					return rev;
				}
			}
		}

		public int GetLastRevision (string lane)
		{
			using (IDbCommand cmd = CreateCommand ()) {
				DBLane l = LookupLane (lane);
				cmd.CommandText = "SELECT max (CAST (revision AS int)) FROM Revision WHERE lane_id = @lane_id";
				DB.CreateParameter (cmd, "lane_id", l.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						return 0;
					if (reader.IsDBNull (0))
						return 0;

					return reader.GetInt32 (0);
				}
			}
		}

		public List<DBWorkView2> GetWork (DBRevisionWork revisionwork)
		{
			List<DBWorkView2> result = new List<DBWorkView2> ();

			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM WorkView2 WHERE revisionwork_id = @revisionwork_id ORDER BY sequence";
				DB.CreateParameter (cmd, "revisionwork_id", revisionwork.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBWorkView2 (reader));
				}
			}
			return result;
		}

		public bool HasWork (int lane_id, int revision_id, int host_id)
		{
			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT Count (*) FROM Work WHERE lane_id = @lane_id AND revision_id = @revision_id AND host_id = @host_id";
				DB.CreateParameter (cmd, "lane_id", lane_id);
				DB.CreateParameter (cmd, "revision_id", revision_id);
				DB.CreateParameter (cmd, "host_id", host_id);
				return (int) cmd.ExecuteScalar () != 0;
			}
		}

		public DBWork GetNextStep (string lane)
		{
			DBWork result = null;

			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM steps WHERE lane = @lane AND (state = 0 OR state = 1) ORDER BY revision DESC, sequence LIMIT 1";
				DB.CreateParameter (cmd, "lane", lane);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						if (result != null)
							throw new Exception ("Got more than one step");
						result = new DBWork ();
						result.Load (reader);
					}
				}
			}

			return result;
		}

		public DBHostLane GetHostLane (int host_id, int lane_id)
		{
			DBHostLane result;
			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM HostLane WHERE lane_id = @lane_id AND host_id = @host_id;";
				DB.CreateParameter (cmd, "host_id", host_id);
				DB.CreateParameter (cmd, "lane_id", lane_id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						return null;
					result = new DBHostLane (reader);
					if (reader.Read ())
						throw new Exception (string.Format ("Found more than one HostLane with host_id {0} and lane_id {1}", host_id, lane_id));
				}
			}
			return result;
		}

		/// <summary>
		/// Checks if the specified RevisionWork is the latest.
		/// </summary>
		/// <param name="current"></param>
		/// <returns></returns>
		public bool IsLatestRevisionWork (DBRevisionWork current)
		{
			using (IDbCommand cmd = CreateCommand ()) {
				cmd.CommandText = @"
SELECT 
	RevisionWork.id
FROM 
	RevisionWork
INNER JOIN 
	Revision ON RevisionWork.revision_id = Revision.id
WHERE 	
	    lock_expires < now () AND
	    RevisionWork.host_id = @host_id 
	AND RevisionWork.lane_id = @lane_id
	AND RevisionWork.completed = false
ORDER BY Revision.date DESC
LIMIT 1
;";
				DB.CreateParameter (cmd, "host_id", current.host_id);
				DB.CreateParameter (cmd, "lane_id", current.lane_id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ()) {
						Logger.Log ("IsLatestRevisionWork: No result.");
						return true;
					}

					if (reader.GetInt32 (0) <= current.id)
						return true;

					Logger.Log ("IsLatestRevisionWork: Latest id: {0}, current id: {1}", reader.GetInt32 (0), current.id);
					return false;
				}
			}
		}

		/// <summary>
		/// Will return a locked revision work.
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="host"></param>
		/// <returns></returns>
		public DBRevisionWork GetRevisionWork (DBLane lane, DBHost host, DBHost workhost)
		{
			DBRevisionWork result = null;

			using (IDbCommand cmd = CreateCommand ()) {
				// sorting by RevisionWork.workhost_id ensures that we'll get 
				// revisionwork which has been started at the top of the list.
				cmd.CommandText = @"
SELECT 
	RevisionWork.*
FROM 
	RevisionWork
INNER JOIN 
	Revision ON RevisionWork.revision_id = Revision.id
WHERE 
        RevisionWork.host_id = @host_id 
	AND (RevisionWork.workhost_id = @workhost_id OR RevisionWork.workhost_id IS NULL)
	AND RevisionWork.lane_id = @lane_id
	AND RevisionWork.state <> @dependencynotfulfilled AND RevisionWork.state <> 10
	AND RevisionWork.completed = false
ORDER BY RevisionWork.workhost_id IS NULL ASC, Revision.date DESC
LIMIT 1
;";
				DB.CreateParameter (cmd, "host_id", host.id);
				DB.CreateParameter (cmd, "lane_id", lane.id);
				DB.CreateParameter (cmd, "workhost_id", workhost.id);
				DB.CreateParameter (cmd, "dependencynotfulfilled", (int) DBState.DependencyNotFulfilled);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						result = new DBRevisionWork (reader);
				}
			}

			return result;
		}

		/// <summary>
		/// The current date/time in the database.
		/// This is used to minimize date/time differences between 
		/// </summary>
		public DateTime Now
		{
			get
			{
				return DateTime.Now.Add (db_time_difference);
			}
		}
	}
}