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
	public partial class DBLaneDependency : DBRecord
	{
		public const string TableName = "LaneDependency";

		public DBLaneDependency ()
		{
		}
	
		public DBLaneDependency (DB db, int id)
			: base (db, id)
		{
		}
	
		public DBLaneDependency (IDataReader reader) 
			: base (reader)
		{
		}

		public DBLaneDependencyCondition Condition
		{
			get { return (DBLaneDependencyCondition) condition; }
			set { condition = (int) value; }
		}

		/// <summary>
		/// Returns a list of all the dependencies for the specified lane.
		/// Returns null if there are no dependencies for the lane.
		/// </summary>
		/// <param name="lane"></param>
		/// <returns></returns>
		public static List<DBLaneDependency> GetDependencies (DB db, DBLane lane)
		{
			List<DBLaneDependency> result = null;

			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM LaneDependency WHERE lane_id = @lane_id;";
				DB.CreateParameter (cmd, "lane_id", lane.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						if (result == null)
							result = new List<DBLaneDependency> ();
						result.Add (new DBLaneDependency (reader));
					}
				}
			}

			return result;
		}
	}
}

