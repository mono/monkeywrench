using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace MonkeyWrench.Test
{
	public abstract class VCTest : TestBase
	{
		#region Commit data
		protected class CommitObject
		{
			public string [] Files;
			public string [] Contents;
			public string Message;
			public string Author;
			public string EMail;
		}
		protected CommitObject [] Commits = new CommitObject [] {
			new CommitObject {Files = new string [] {"file1"}, Contents = new string [] {"commit#1"}, Message =  "Commit1", Author = "Mr. Foo Bar", EMail = "foo@bar.com"},
			new CommitObject {Files = new string [] {"file2"}, Contents = new string [] {"commit#2"}, Message =  "Commit2", Author = "Mr. Foo Bar", EMail = "foo@bar.com"},
			new CommitObject {Files = new string [] {"file3"}, Contents = new string [] {"commit#3"}, Message =  "Commit3", Author = "Mr. Foo Bar", EMail = "foo@bar.com"},
			new CommitObject {Files = new string [] {"file1"}, Contents = new string [] {"commit#4"}, Message =  "Commit4", Author = "Mr. Foo Bar", EMail = "foo@bar.com"},
			new CommitObject {Files = new string [] {"file5"}, Contents = new string [] {"commit#5"}, Message =  
@"2009-09-30  Rolf Bjarne Kvinge  <RKvinge@novell.com>

        * File.cs: This is a somewhat longer commit message.

".Replace ("\r", ""), Author = "Mr. Foo Bar", EMail = "foo@bar.com"},
			new CommitObject {Files = new string [] {"file6"}, Contents = new string [] {"commit#6"}, Message =  @"Commit6:
	      		
some whitespace in the beginning
and
	this
		is
			a
			    commit message that spans
		 several lines.
and with trailing whitespace too
	      		

	      		

".Replace ("\r", ""), Author = "Mr. Foo Bar", EMail = "foo@bar.com"},
		};
		#endregion

		public string GetTestRepositoryPath
		{
			get
			{
				return Path.Combine (Runner.TemporaryTestDirectory, Path.Combine (GetType ().Name, "Repository"));
			}
		}

		public void CreateTestRepository ()
		{
			string dir = GetTestRepositoryPath;

			Directory.CreateDirectory (dir);

			InitializeTestRepository (dir);
			for (int i = 0; i < Commits.Length; i++) {
				Commit (Commits [i].Files [0], Commits [i].Contents [0], Commits [i].Message, Commits [i].Author, Commits [i].EMail);
			}
		}


		protected void CommitInternal (string filename, string commit_msg, string author, string email)
		{
			string commit_msg_filename = null;
			try {
				commit_msg_filename = Path.GetTempFileName ();
				File.WriteAllText (commit_msg_filename, commit_msg);
				CommitInternalWithMessageFile (filename, commit_msg_filename, author, email);
			} finally {
				if (!string.IsNullOrEmpty (commit_msg_filename) && File.Exists (commit_msg_filename))
					File.Delete (commit_msg_filename);
			}
		}

		protected virtual void CommitInternalWithMessageFile (string filename, string commit_msg_filename, string author, string email)
		{
			throw new NotImplementedException ();
		}

		protected abstract void Commit (string filename, string contents, string commit_msg, string author, string email);

		protected abstract string GetVCType { get; }

		protected abstract void InitializeTestRepository (string path);

		[TestFixture]
		public void SchedulerTest ()
		{
			Database.Clean (db);
			CreateTestRepository ();

			db.ExecuteNonQuery (
				string.Format (
@"
INSERT INTO Host (host, enabled) VALUES ('test', true);
INSERT INTO Person (login, password, roles) VALUES ('test', 'hithere', 'BuildBot');
INSERT INTO Lane (lane, source_control, repository, min_revision, max_revision) VALUES ('scheduler-test', '{1}', 'file://{0}', '', '');
INSERT INTO HostLane (host_id, lane_id, enabled) VALUES ((SELECT id FROM Host WHERE host = 'test'), (SELECT id FROM Lane WHERE lane = 'scheduler-test'), true);
INSERT INTO Command (lane_id, command, sequence) VALUES ((SELECT id FROM Lane WHERE lane = 'scheduler-test'), 'test.sh', 0);
", GetTestRepositoryPath, GetVCType));

			Scheduler.Scheduler.ExecuteScheduler (true);

			DataTable revision = new DataTable ();
			DataTable revisionwork = new DataTable ();
			DataTable work = new DataTable ();
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Revision ORDER BY Date;";
				using (IDataReader reader = cmd.ExecuteReader ()) {
					revision.Load (reader);
				}
			}
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM RevisionWork INNER JOIN Revision ON Revision.id = RevisionWork.revision_id ORDER BY Revision.date;";
				using (IDataReader reader = cmd.ExecuteReader ()) {
					revisionwork.Load (reader);
				}
			}
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Work INNER JOIN RevisionWork ON RevisionWork.id = Work.revisionwork_id INNER JOIN Revision ON Revision.id = RevisionWork.revision_id ORDER BY Revision.date;";
				using (IDataReader reader = cmd.ExecuteReader ()) {
					work.Load (reader);
				}
			}

			/* check the number of commits */
			Check.AreEqual (Commits.Length, revision.Rows.Count, "Revision.Rows.Count");
			Check.AreEqual (Commits.Length, revisionwork.Rows.Count, "RevisionWork.Rows.Count");
			Check.AreEqual (Commits.Length, work.Rows.Count, "Work.Rows.Count");

			for (int i = 0; i < revision.Rows.Count; i++) {
				for (int j = i + 1; j < revision.Rows.Count; j++) {
					/* assert that the date in the commits aren't duplicated */
					Check.AreNotEqual ((DateTime) revision.Rows [i] ["date"], (DateTime) revision.Rows [j] ["date"], "Rows" + i.ToString () + "/" + j.ToString ());
				}

				/* Check that the author is the right one */
				Check.AreEqual (Commits [i].Author, (string) revision.Rows [i] ["author"], "Author Row #" + i.ToString ());

				/* Check the message contents */
				CheckFileContents ((int?) revision.Rows [i] ["log_file_id"], "Commit message #" + i.ToString (), Commits [i].Message, true /* I couldn't find a way to make git return the exact same log message as what was committed, it does whitespace transformations on it */);
			}

			/* Check the RevisionWork data */
			for (int i = 0; i < revisionwork.Rows.Count; i++) {
				Check.AreEqual (0, (int) revisionwork.Rows [i] ["state"], "RevisionWork.Rows[" + i.ToString () + "].state");
				Check.AreEqual (false, (bool) revisionwork.Rows [i] ["completed"], "RevisionWork.Rows[" + i.ToString () + "].completed");
				Check.AreEqual (new DateTime (2000, 1, 1), (DateTime) revisionwork.Rows [i] ["lock_expires"], "RevisionWork.Rows[" + i.ToString () + "].lock_expires");
				Check.AreEqual (new DateTime (2000, 1, 1), (DateTime) revisionwork.Rows [i] ["endtime"], "RevisionWork.Rows[" + i.ToString () + "].endtime");
				Check.IsDBNull (revisionwork.Rows [i] ["workhost_id"], "RevisionWork.Rows[" + i.ToString () + "].workhost_id");
			}

			/* Check the Work data */
			for (int i = 0; i < work.Rows.Count; i++) {
			}
		}
	}
}
