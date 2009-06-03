/*
 * DBHost_Extensions.cs
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
	public static class DBHost_Extensions
	{
		public static DBHost Create (DB db, int id)
		{
			return DBRecord_Extensions.Create (db, new DBHost (), DBHost.TableName, id);
		}

		public static void AddLane (this DBHost me, DB db, int lane_id)
		{
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "INSERT INTO HostLane (host_id, lane_id) VALUES (@host_id, @lane_id);";
				DB.CreateParameter (cmd, "host_id", me.id);
				DB.CreateParameter (cmd, "lane_id", lane_id);
				cmd.ExecuteNonQuery ();
			}
		}

		public static void RemoveLane (this DBHost me, DB db, int lane_id)
		{
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "DELETE FROM HostLane WHERE host_id = @host_id AND lane_id = @lane_id;";
				DB.CreateParameter (cmd, "host_id", me.id);
				DB.CreateParameter (cmd, "lane_id", lane_id);
				cmd.ExecuteNonQuery ();
			}
		}

		public static void EnableLane (this DBHost me, DB db, int lane_id, bool enable)
		{
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "UPDATE HostLane SET enabled = @enabled WHERE host_id = @host_id AND lane_id = @lane_id;";
				DB.CreateParameter (cmd, "host_id", me.id);
				DB.CreateParameter (cmd, "lane_id", lane_id);
				DB.CreateParameter (cmd, "enabled", enable);
				cmd.ExecuteNonQuery ();
			}
		}

		public static List<DBHostLaneView> GetLanes (this DBHost me, DB db)
		{
			List<DBHostLaneView> result = new List<DBHostLaneView> ();
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM HostLaneView WHERE host_id = @host_id ORDER BY lane;";
				DB.CreateParameter (cmd, "host_id", me.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (new DBHostLaneView (reader));
					}
				}
			}
			return result;
		}
	}
}
