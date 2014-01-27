/*
 * DBRevisionWork_Extensions.cs
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
using System.Linq;
using System.Text;

using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Database
{
	public static class DBRevisionWork_Extensions
	{
		public static DBRevisionWork Create (DB db, int id)
		{
			return DBRecord_Extensions.Create (db, new DBRevisionWork (), DBRevisionWork.TableName, id);
		}

		public static int GetCount (DB db, int lane_id, int host_id)
		{
			object result;

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT Count(*) FROM RevisionWork WHERE lane_id = @lane_id AND host_id = @host_id;";
				DB.CreateParameter (cmd, "lane_id", lane_id);
				DB.CreateParameter (cmd, "host_id", host_id);
				result = cmd.ExecuteScalar ();
				if (result is int)
					return (int) result;
				else if (result is long)
					return (int) (long) result;
				return 0;
			}
		}

		/// <summary>
		/// Returns a list of all the files this revisionwork has produced
		/// </summary>
		/// <param name="db"></param>
		/// <returns></returns>
		public static List<DBWorkFile> GetFiles (this DBRevisionWork rw, DB db)
		{
			List<DBWorkFile> result = new List<DBWorkFile> ();

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = @"
SELECT 
	WorkFile.* 
FROM WorkFile 
	INNER JOIN Work ON WorkFile.work_id = Work.id
	INNER JOIN RevisionWork ON Work.revisionwork_id = RevisionWork.id
WHERE
	RevisionWork.id = @id;
";
				DB.CreateParameter (cmd, "id", rw.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBWorkFile (reader));
				}
			}

			return result;
		}

		/// <summary>
		/// Calculates a new state if the current state is NotDone
		/// </summary>
		/// <param name="db"></param>
		/// <returns></returns>
		public static DBState EnsureState (this DBRevisionWork rw, DB db)
		{
			rw.State = EnsureState (db, rw.id, rw.State);
			return rw.State;
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
		public static DBState CalculateState (this DBRevisionWork rw, DB db)
		{
			bool completed;

			rw.State = CalculateState (db, rw.id, out completed);
			rw.completed = completed;

			return rw.State;
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

			using (IDbCommand cmd = db.CreateCommand ()) {
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

		public static void UpdateState (this DBRevisionWork rw, DB db)
		{
			rw.CalculateState (db);
			SaveState (db, rw.id, rw.State, rw.completed);
		}

		private static void SaveState (DB db, int id, DBState state, bool completed)
		{
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "UPDATE RevisionWork SET state = @state, completed = @completed WHERE id = @id;";
				DB.CreateParameter (cmd, "state", (int) state);
				DB.CreateParameter (cmd, "completed", completed);
				DB.CreateParameter (cmd, "id", id);
				cmd.ExecuteNonQuery ();
			}
		}

		/// <summary>
		/// Finds pending steps for the current revision
		/// </summary>
		/// <returns></returns>
		public static List<DBWorkView2> GetNextWork (this DBRevisionWork rw, DB db, DBLane lane, DBHost host, DBRevision revision, bool multiple_work)
		{
			List<DBWorkView2> result = new List<DBWorkView2> (); ;

			if (revision == null)
				throw new ArgumentNullException ("revision");

			if (lane == null)
				throw new ArgumentNullException ("lane");

			if (host == null)
				throw new ArgumentNullException ("host");

			rw.FilterPendingWork (db, db.GetWork (rw), result, multiple_work);

			if (result.Count == 0 && !rw.completed) {
				rw.completed = true;
				rw.UpdateState (db);
			}

			return result;
		}

		/// <summary>
		/// Puts pending steps in result
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="revision"></param>
		/// <returns></returns>
		private static void FilterPendingWork (this DBRevisionWork rw, DB db, List<DBWorkView2> steps, List<DBWorkView2> result, bool multiple_work)
		{
			bool failed_revision = false;

			for (int i = 0; i < steps.Count; i++) {
				if (steps [i].State == DBState.NotDone || steps [i].State == DBState.Executing) {
					// After a failed and fatal step, don't add any steps which aren't marked as alwaysexecute.
					if (failed_revision && !steps [i].alwaysexecute) {
						DBWork_Extensions.SetState (db, steps [i].id, steps [i].State, DBState.Skipped);
						continue;
					}

					// if we already have steps, don't add steps with higher sequence numbers
					if (!multiple_work && result.Count > 0 && result [0].sequence != steps [i].sequence)
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
			DBRevisionWork result;

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM RevisionWork WHERE lane_id = @lane_id AND revision_id = @revision_id ";
				if (host != null) {
					cmd.CommandText += " AND host_id = @host_id;";
					DB.CreateParameter (cmd, "host_id", host.id);
				}
				DB.CreateParameter (cmd, "lane_id", lane.id);
				DB.CreateParameter (cmd, "revision_id", revision.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						return null;

					result = new DBRevisionWork (reader);

					if (reader.Read ())
						throw new ApplicationException (string.Format ("Found more than one revision work for the specified lane/host/revision ({0}/{1}/{2})", lane.lane, host == null ? "null" : host.host, revision.revision));

					return result;
				}
			}
		}

		/// <summary>
		/// Sets workhost to the specified host and saves it to the db.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="host"></param>
		public static bool SetWorkHost (this DBRevisionWork rw, DB db, DBHost host)
		{
			object result;
			string update_cmd = string.Format (@"
UPDATE RevisionWork SET workhost_id = {0} WHERE id = {1} AND workhost_id IS NULL;
", host.id, rw.id);

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = update_cmd;

				var rv = cmd.ExecuteNonQuery ();

				if (rv != 1) {
					Logger.Log ("{0}: {1} (failed)", cmd.CommandText, rv);
					return false;
				}
			}

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = @"
SELECT workhost_id FROM RevisionWork where id = @id AND workhost_id = @workhost_id;
";
				DB.CreateParameter (cmd, "workhost_id", host.id);
				DB.CreateParameter (cmd, "id", rw.id);

				result = cmd.ExecuteScalar ();
				if (result != null && (result is int || result is long)) {
					rw.workhost_id = host.id;
					Logger.Log ("{0}: {1} (succeeded)", update_cmd, result);
					return true;
				}
				Logger.Log ("{0}: {1} (failed 2)", update_cmd, result);
				return false;
			}
		}
	}
}
