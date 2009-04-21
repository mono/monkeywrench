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
using System.Data;
using System.Data.Common;
using System.Text;

namespace Builder
{
	public partial class DBLane : DBRecord
	{
		public static string TableName = "Lane";
		public DBLane ()
		{
		}
		public DBLane (DB db, int id)
			: base (db, id)
		{
		}
		public DBLane (IDataReader reader)
			: base (reader)
		{
		}

		public List<DBLanefile> GetFiles (DB db)
		{
			return GetFiles (db, id);
		}

		public List<DBCommand> GetCommands (DB db)
		{
			List<DBCommand> result = new List<DBCommand> ();
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Command WHERE lane_id = @lane_id ORDER BY sequence;";
				DB.CreateParameter (cmd, "lane_id", id);
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
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT Lanefile.id, Lanefiles.lane_id, name, mime, contents FROM Lanefile INNER JOIN Lanefiles ON Lanefiles.lanefile_id = Lanefile.id WHERE Lanefiles.lane_id = @lane_id ORDER BY name ASC";
				DB.CreateParameter (cmd, "lane_id", lane_id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBLanefile (reader));
				}
			}
			return result;
		}

		public List<DBHostLaneView> GetHosts (DB db)
		{
			List<DBHostLaneView> result = new List<DBHostLaneView> ();
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM HostLaneView WHERE lane_id = @lane_id ORDER BY host;";
				DB.CreateParameter (cmd, "lane_id", id);
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
			using (IDbTransaction transaction = db.Connection.BeginTransaction ()) {
				using (IDbCommand cmd = db.Connection.CreateCommand ()) {
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

		public List<DBLaneDependency> GetDependencies (DB db)
		{
			List<DBLaneDependency> result = new List<DBLaneDependency> ();
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM LaneDependency WHERE lane_id = @lane_id ORDER BY dependent_lane_id;";
				DB.CreateParameter (cmd, "lane_id", id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBLaneDependency (reader));
				}
			}
			return result;
		}

		public DBRevision FindRevision (DB db, string revision)
		{
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT Revision.* FROM Revision WHERE lane_id = @lane_id AND revision = @revision;";
				DB.CreateParameter (cmd, "lane_id", id);
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
