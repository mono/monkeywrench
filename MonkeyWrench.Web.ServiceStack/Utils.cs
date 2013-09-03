using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Web.ServiceStack
{
	public static class Utils
	{
		public static DBLane GetTopMostParent (DBLane forLane, IEnumerable<DBLane> lanes)
		{
			var parent = forLane;
			while (parent.parent_lane_id != null)
				parent = lanes.First (l => l.id == parent.parent_lane_id.Value);
			return parent;
		}

		public static Uri MakeBuildUrl (int laneId, int hostId, int revisionId)
		{
			var url = Configuration.GetWebSiteUrl ();
			url += string.Format ("/ViewLane.aspx?lane_id={0}&host_id={1}&revision_id={2}", laneId, hostId, revisionId);

			return new Uri (url);
		}

		public static DBHost FindHost (DB db, int host_id)
		{
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Host WHERE id = @id;";
				DB.CreateParameter (cmd, "id", host_id);

				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						return new DBHost (reader);
				}
			}
			
			return null;
		}

		public static DBLane FindLane (DB db, int lane_id)
		{
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Lane WHERE id = @id;";
				DB.CreateParameter (cmd, "id", lane_id);

				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						return new DBLane (reader);
				}
			}
			
			return null;
		}

		public static DBRevision FindRevision (DB db, int revision_id)
		{
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Revision WHERE id = @id;";
				DB.CreateParameter (cmd, "id", revision_id);

				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						return new DBRevision (reader);
				}
			}
			
			return null;
		}
	}
}

