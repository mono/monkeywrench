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
	public partial class DBHost : DBRecord
	{
        public const string TableName = "Host";

		public DBHost ()
		{
		}

		public DBHost (DB db, int id)
			: base (db, id)
		{
		}

		public DBHost (IDataReader reader) : base (reader)
		{
		}

		public DBQueueManagement QueueManagement
		{
			get { return (DBQueueManagement) queuemanagement; }
		}

        public void AddLane(DB db, int lane_id)
        {
            using (IDbCommand cmd = db.Connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO HostLane (host_id, lane_id) VALUES (@host_id, @lane_id);";
                DB.CreateParameter(cmd, "host_id", id);
                DB.CreateParameter(cmd, "lane_id", lane_id);
                cmd.ExecuteNonQuery();
            }
        }

        public void RemoveLane(DB db, int lane_id)
        {
            using (IDbCommand cmd = db.Connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM HostLane WHERE host_id = @host_id AND lane_id = @lane_id;";
                DB.CreateParameter(cmd, "host_id", id);
                DB.CreateParameter(cmd, "lane_id", lane_id);
                cmd.ExecuteNonQuery();
            }
        }

        public void EnableLane(DB db, int lane_id, bool enable)
        {
            using (IDbCommand cmd = db.Connection.CreateCommand())
            {
                cmd.CommandText = "UPDATE HostLane SET enabled = @enabled WHERE host_id = @host_id AND lane_id = @lane_id;";
                DB.CreateParameter(cmd, "host_id", id);
                DB.CreateParameter(cmd, "lane_id", lane_id);
                DB.CreateParameter(cmd, "enabled", enable);
                cmd.ExecuteNonQuery();
            }
        }

		public List<DBHostLaneView> GetLanes (DB db)
		{
            List<DBHostLaneView> result = new List<DBHostLaneView>();
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM HostLaneView WHERE host_id = @host_id ORDER BY lane;";
				DB.CreateParameter (cmd, "host_id", id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
                        result.Add(new DBHostLaneView(reader));
					}
				}
			}
			return result;
		}
	}
}
