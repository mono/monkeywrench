/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */


using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;

namespace Builder
{
	public partial class DBRevisionWork : DBRecord
	{
		public const string TableName = "RevisionWork";

		public DBRevisionWork ()
		{
		}

		public DBRevisionWork (DB db, int id)
			: base (db, id)
		{
		}

		public DBRevisionWork (IDataReader reader)
			: base (reader)
		{
		}

		public DBState State
		{
			get { return (DBState) state; }
			set { state = (int) value; }
		}

		public static int GetCount (DB db, DBLane lane, DBHost host)
		{
			object result;

			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT Count(*) FROM RevisionWork WHERE lane_id = @lane_id AND host_id = @host_id;";
				DB.CreateParameter (cmd, "lane_id", lane.id);
				DB.CreateParameter (cmd, "host_id", host.id);
				result = cmd.ExecuteScalar ();
				if (result is int)
					return (int) result;
				else if (result is long)
					return (int) (long) result;
				return 0;
			}
		}

		/// <summary>
		/// Calculates a new state if the current state is NotDone
		/// </summary>
		/// <param name="db"></param>
		/// <returns></returns>
		public DBState EnsureState (DB db)
		{
			State = EnsureState (db, id, State);
			return State;
		}

		/// <summary>
		/// Calculates a new state if the current state is NotDone.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="id"></param>
		/// <param name="current"></param>
		/// <returns></returns>
		public static DBState EnsureState (DB db, int id, DBState current)
		{
			DBState actual;
			bool completed;

			if (current != DBState.NotDone)
				return current;

			actual = CalculateState (db, id, out completed);

			if (actual != current)
				SaveState (db, id, actual, completed);

			return actual;
		}

		/// <summary>
		/// Calculates the state. Always hits the db.
		/// </summary>
		/// <param name="db"></param>
		/// <returns></returns>
		public DBState CalculateState (DB db)
		{
			bool completed;

			State = CalculateState (db, id, out completed);
			this.completed = completed;

			return State;
		}

		/// <summary>
		/// Calculates the state. Always hits the db.
		/// </summary>
		/// <param name="db"></param>
		/// <returns></returns>
		public static DBState CalculateState (DB db, int id, out bool completed)
		{
			List<DBState> states = new List<DBState> ();
			List<bool> nonfatal = new List<bool> ();

			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT state, Command.nonfatal FROM Work INNER JOIN Command ON Work.command_id = Command.id WHERE revisionwork_id = @revisionwork_id ORDER BY sequence";
				DB.CreateParameter (cmd, "revisionwork_id", id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						states.Add ((DBState) reader.GetInt32 (reader.GetOrdinal ("state")));
						nonfatal.Add (reader.GetBoolean (reader.GetOrdinal ("nonfatal")));
					}
				}
			}

			bool failed = false;
			completed = states.Count != 0 && ForAll (states, nonfatal,
				delegate (DBState state, bool nf)
				{
					failed |= !nf && (state == DBState.Failed || state == DBState.Timeout || state == DBState.Aborted);
					if (state != DBState.NotDone && state != DBState.Executing && state != DBState.Paused)
						return true;
					if (state == DBState.NotDone && failed)
						return true;
					return false;
				});

			// any Work.State == dependency not fulfilled => DependencyNotFulfilled
			if (ForAny (states, nonfatal, delegate (DBState state, bool nf) { return state == DBState.DependencyNotFulfilled; }))
				return DBState.DependencyNotFulfilled;

			// any Work.State == paused => Paused
			if (ForAny (states, nonfatal, delegate (DBState state, bool nf) { return state == DBState.Paused; }))
				return DBState.Paused;

			// all Work.State == queued => Queued
			if (ForAll (states, nonfatal, delegate (DBState state, bool nf) { return state == DBState.NotDone; }))
				return DBState.NotDone;

			// all Work.State == success => Success
			if (ForAll (states, nonfatal, delegate (DBState state, bool nf) { return state == DBState.Success; }))
				return DBState.Success;

			// any fatal (Work.State == failed || Work.state == aborted) => Failed
			if (ForAny (states, nonfatal, delegate (DBState state, bool nf) { return !nf && (state == DBState.Failed || state == DBState.Timeout); }))
				return DBState.Failed;

			// any fatal Work.State == timeout => Timeout
			if (ForAny (states, nonfatal, delegate (DBState state, bool nf) { return !nf && state == DBState.Timeout; }))
				return DBState.Timeout;

			// any nonfatal (Work.State == failed || Work.State == timeout || Work.State == aborted) => Issues
			if (ForAny (states, nonfatal, delegate (DBState state, bool nf) { return nf && (state == DBState.Failed || state == DBState.Timeout || state == DBState.Aborted); }))
				return DBState.Issues;

			// any Work.State == executing => Executing
			if (ForAny (states, nonfatal, delegate (DBState state, bool nf) { return state == DBState.Executing; }))
				return DBState.Executing;

			return DBState.Executing;
		}

		private delegate bool IsMatch (DBState states, bool nonfatal);

		private static bool ForAll (List<DBState> states, List<bool> nonfatal, IsMatch pred)
		{
			for (int i = 0; i < states.Count; i++) {
				if (!(pred (states [i], nonfatal [i])))
					return false;
			}
			return true;
		}

		private static bool ForAny (List<DBState> states, List<bool> nonfatal, IsMatch pred)
		{
			for (int i = 0; i < states.Count; i++) {
				if (pred (states [i], nonfatal [i]))
					return true;
			}
			return false;
		}

		public static void UpdateStateAll (DB db)
		{
			List<DBRevisionWork> ids = new List<DBRevisionWork> ();

			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = @"
SELECT RevisionWork.*
FROM RevisionWork;";
				/*INNER JOIN revisionwork ON revisionwork.id = work.revisionwork_id 
				WHERE work.state != 0 AND revisionwork.completed = false AND revisionwork.state = 0 
				GROUP BY work.revision_id;";*/
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						ids.Add (new DBRevisionWork (reader));
				}
			}

			for (int i = 0; i < ids.Count; i++) {
				DBRevisionWork rw = ids [i];
				rw.UpdateState (db);
				Console.WriteLine ("Done {0}/{1} = {2} % host_id {3} lane_id {4} revision_id {5}", i + 1, ids.Count, 100.0 * (double) (i + 1) / (double) ids.Count, rw.host_id, rw.lane_id, rw.revision_id);
			}
		}

		public void UpdateState (DB db)
		{
			CalculateState (db);
			SaveState (db, id, State, completed);
		}

		private static void SaveState (DB db, int id, DBState state, bool completed)
		{
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "UPDATE RevisionWork SET state = @state, completed = @completed WHERE id = @id;";
				DB.CreateParameter (cmd, "state", (int) state);
				DB.CreateParameter (cmd, "completed", completed);
				DB.CreateParameter (cmd, "id", id);
				cmd.ExecuteNonQuery ();
			}
		}

		/// <summary>
		/// Finds pending steps for the current revision
		/// All returned steps will have the same sequence number.
		/// </summary>
		/// <returns></returns>
		public List<DBWorkView2> GetNextWork (DB db, DBLane lane, DBHost host, DBRevision revision)
		{
			List<DBWorkView2> result = new List<DBWorkView2> (); ;

			if (revision == null)
				throw new ArgumentNullException ("revision");

			if (lane == null)
				throw new ArgumentNullException ("lane");

			if (host == null)
				throw new ArgumentNullException ("host");

			FilterPendingWork (db, db.GetWork (this), result);

			if (result.Count == 0 && !this.completed) {
				this.completed = true;
				UpdateState (db);
			}

			return result;
		}

		/// <summary>
		/// Puts pending steps in result
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="revision"></param>
		/// <returns></returns>
		private void FilterPendingWork (DB db, List<DBWorkView2> steps, List<DBWorkView2> result)
		{
			bool failed_revision = false;

			for (int i = 0; i < steps.Count; i++) {
				if (steps [i].State == DBState.NotDone || steps [i].State == DBState.Executing) {
					// After a failed and fatal step, don't add any steps which aren't marked as alwaysexecute.
					if (failed_revision && !steps [i].alwaysexecute) {
						DBWork.SetState (db, steps [i].id, steps [i].State, DBState.Skipped);
						continue;
					}

					// if we already have steps, don't add steps with higher sequence numbers
					if (result.Count > 0 && result [0].sequence != steps [i].sequence)
						continue;

					result.Add (steps [i]);
				} else if (steps [i].State == DBState.Paused) {
					// Don't add any steps after a paused step
					break;
				} else {
					if (!steps [i].nonfatal && (steps [i].State == DBState.Failed || steps [i].State == DBState.Timeout || steps [i].State == DBState.Aborted)) {
						failed_revision = true;
					}
				}
			}
		}

		public static DBRevisionWork Find (DB db, DBLane lane, DBHost host, DBRevision revision)
		{
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM RevisionWork WHERE lane_id = @lane_id AND host_id = @host_id AND revision_id = @revision_id;";
				DB.CreateParameter (cmd, "host_id", host.id);
				DB.CreateParameter (cmd, "lane_id", lane.id);
				DB.CreateParameter (cmd, "revision_id", revision.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						return new DBRevisionWork (reader);
					return null;
				}
			}
		}


		/// <summary>
		/// Sets workhost to the specified host and saves it to the db.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="host"></param>
		public bool SetWorkHost (DB db, DBHost host)
		{
			object result;
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = @"
UPDATE RevisionWork SET workhost_id = @workhost_id WHERE id = @id AND workhost_id IS NULL;
SELECT workhost_id FROM RevisionWork where id = @id AND workhost_id = @workhost_id;
";
				DB.CreateParameter (cmd, "workhost_id", host.id);
				DB.CreateParameter (cmd, "id", id);

				result = cmd.ExecuteScalar ();
				if (result != null && (result is int || result is long)) {
					workhost_id = host.id;
					return true;
				}
				return false;
			}
		}

		public static bool IsSuccess (DB db, int lane_id, DBRevision revision)
		{
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = @"
SELECT RevisionWork.id
FROM RevisionWork 
WHERE lane_id = @lane_id AND state = @success AND revision_id = @revision_id
LIMIT 1;
";
				DB.CreateParameter (cmd, "lane_id", lane_id);
				DB.CreateParameter (cmd, "revision_id", revision.id);
				DB.CreateParameter (cmd, "success", (int) DBState.Success);

				object obj = cmd.ExecuteScalar ();
				return obj != null && !(obj is DBNull);
			}
		}

		public static bool IsSuccessWithFile (DB db, int lane_id, DBRevision revision, string filename)
		{
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = @"
SELECT RevisionWork.id
FROM RevisionWork
INNER JOIN Work ON RevisionWork.id = Work.revisionwork_id
INNER JOIN WorkFile ON Work.id = WorkFile.work_id
INNER JOIN Revision ON Revision.id = RevisionWork.revision_id
WHERE RevisionWork.lane_id = @lane_id AND RevisionWork.state = @success AND Revision.revision = @revision AND WorkFile.filename = @filename
LIMIT 1;
";
				DB.CreateParameter (cmd, "lane_id", lane_id);
				DB.CreateParameter (cmd, "revision", revision.revision); // Don't join with id here, if the revision comes from another lane, it might have a different id
				DB.CreateParameter (cmd, "success", (int) DBState.Success);
				DB.CreateParameter (cmd, "filename", filename);

				object obj = cmd.ExecuteScalar ();
				bool result = obj != null && !(obj is DBNull);

				// Console.WriteLine ("IsSuccessWithFile ({0}, {1}, '{2}') => {3}", lane_id, revision.id, filename, result);

				return result;
			}
		}
	}
}

