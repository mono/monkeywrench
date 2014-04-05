/*
 * DBLane_Extensions.cs
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
using System.Linq;
using System.Text;

using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Database
{
	public static class DBLane_Extensions
	{
		public static DBLane Create (DB db, int id)
		{
			return DBRecord_Extensions.Create (db, new DBLane (), DBLane.TableName, id);
		}

		public static List<DBLanefile> GetFiles (this DBLane me, DB db, List<DBLane> all_lanes)
		{
			List<DBLanefile> result = new List<DBLanefile> ();
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT Lanefile.* FROM Lanefile INNER JOIN Lanefiles ON Lanefiles.lanefile_id = Lanefile.id WHERE Lanefiles.lane_id = " + me.id.ToString ();

				DBLane parent = me;
				if (all_lanes != null) {
					while (null != (parent = all_lanes.FirstOrDefault ((v) => v.id == parent.parent_lane_id))) {
						cmd.CommandText += " OR LaneFiles.lane_id = " + parent.id.ToString ();
					}
				}

				cmd.CommandText += " ORDER BY name ASC";

				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBLanefile (reader));
				}

				Logger.Log ("Found {0} lane files with {1}", result.Count, cmd.CommandText);
			}
			return result;
		}

		public static List<DBCommand> GetCommandsInherited (this DBLane me, DB db, List<DBLane> all_lanes)
		{
			List<DBCommand> result = new List<DBCommand> ();
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Command WHERE lane_id = " + me.id.ToString ();

				DBLane parent = me;
				while (null != (parent = all_lanes.FirstOrDefault ((v) => v.id == parent.parent_lane_id))) {
					cmd.CommandText += " OR lane_id = " + parent.id.ToString ();
				}

				cmd.CommandText += " ORDER BY sequence;";
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBCommand (reader));
				}
			}
			return result;
		}

		public static List<DBHostLaneView> GetHosts (this DBLane me, DB db)
		{
			List<DBHostLaneView> result = new List<DBHostLaneView> ();
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM HostLaneView WHERE lane_id = @lane_id ORDER BY host;";
				DB.CreateParameter (cmd, "lane_id", me.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (new DBHostLaneView (reader));
					}
				}
			}
			return result;
		}

		public static List<DBLaneNotification> GetNotifications (this DBLane me, DB db)
		{
			var result = new List<DBLaneNotification> ();
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM LaneNotification WHERE lane_id = @lane_id;";
				DB.CreateParameter (cmd, "lane_id", me.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (new DBLaneNotification (reader));
					}
				}
			}
			return result;
		}

		public static void Delete (DB db, int lane_id)
		{
			using (IDbTransaction transaction = db.BeginTransaction ()) {
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"
DELETE FROM RevisionWork WHERE lane_id = @id;
DELETE FROM Revision WHERE lane_id = @id;
DELETE FROM LaneFiles WHERE lane_id = @id;
DELETE FROM Command WHERE lane_id = @id;
DELETE FROM HostLane WHERE lane_id = @id;
DELETE FROM EnvironmentVariable WHERE lane_id = @id;
DELETE FROM LaneDeletionDirective WHERE lane_id = @id;
DELETE FROM LaneDependency WHERE lane_id = @id OR dependent_lane_id = @id;
DELETE FROM Lane WHERE id = @id;
";
					DB.CreateParameter (cmd, "id", lane_id);
					cmd.ExecuteNonQuery ();
				}
				transaction.Commit ();
			}
		}

		public static List<DBLaneDependency> GetDependencies (this DBLane me, DB db)
		{
			List<DBLaneDependency> result = new List<DBLaneDependency> ();
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM LaneDependency WHERE lane_id = @lane_id ORDER BY dependent_lane_id;";
				DB.CreateParameter (cmd, "lane_id", me.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBLaneDependency (reader));
				}
			}
			return result;
		}

		public static List<DBLane> GetDependentLanes (this DBLane me, DB db)
		{
			var result = new List<DBLane> ();
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT Lane.* FROM Lane INNER JOIN LaneDependency ON LaneDependency.lane_id = Lane.id WHERE LaneDependency.dependent_lane_id = @lane_id ORDER BY Lane.lane;";
				DB.CreateParameter (cmd, "lane_id", me.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBLane (reader));
				}
			}
			Logger.Log ("*** * *** GetDependentLanes for {0}: {1} results\n", me.id, result.Count);
			return result;
		}

		public static DBRevision FindRevision (this DBLane me, DB db, string revision)
		{
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT Revision.* FROM Revision WHERE lane_id = @lane_id AND revision = @revision;";
				DB.CreateParameter (cmd, "lane_id",  me.id);
				DB.CreateParameter (cmd, "revision", revision);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						return new DBRevision (reader);
					return null;
				}
			}
		}

		public static List<DBEnvironmentVariable> GetEnvironmentVariables (this DBLane me, DB db)
		{
			List<DBEnvironmentVariable> result = new List<DBEnvironmentVariable> ();
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM EnvironmentVariable WHERE lane_id = @lane_id;";
				DB.CreateParameter (cmd, "lane_id", me.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBEnvironmentVariable (reader));
				}
			}
			return result;
		}

		public static List<DBLaneTag> GetTags (this DBLane me, DB db)
		{
			var result = new List<DBLaneTag> ();
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM LaneTag WHERE lane_id = " + me.id.ToString () + ";";
				using (var reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBLaneTag (reader));
				}
			}
			return result;
		}
	}
}

