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

		public static List<DBLanefile> GetFiles (this DBLane me, DB db)
		{
			return GetFiles (db, me.id);
		}

		public static List<DBCommand> GetCommands (this DBLane me, DB db)
		{
			List<DBCommand> result = new List<DBCommand> ();
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Command WHERE lane_id = @lane_id ORDER BY sequence;";
				DB.CreateParameter (cmd, "lane_id", me.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBCommand (reader));
				}
			}
			return result;
		}

		public static List<DBLanefile> GetFiles (DB db, int lane_id)
		{
			List<DBLanefile> result = new List<DBLanefile> ();
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT Lanefile.id, Lanefiles.lane_id, name, mime, contents FROM Lanefile INNER JOIN Lanefiles ON Lanefiles.lanefile_id = Lanefile.id WHERE Lanefiles.lane_id = @lane_id ORDER BY name ASC";
				DB.CreateParameter (cmd, "lane_id", lane_id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBLanefile (reader));
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

		public static void Delete (DB db, int lane_id)
		{
			using (IDbTransaction transaction = db.BeginTransaction ()) {
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = "";
					// Don't be this destructive quite yet.
					// cmd.CommandText += "DELETE FROM Work WHERE lane_id = @id;\n";
					cmd.CommandText += "DELETE FROM LaneFile WHERE lane_id = @id;\n";
					cmd.CommandText += "DELETE FROM Command WHERE lane_id = @id;\n";
					cmd.CommandText += "DELETE FROM HostLane WHERE lane_id = @id;\n";
					cmd.CommandText += "DELETE FROM Lane WHERE id = @id;\n";
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
	}
}
