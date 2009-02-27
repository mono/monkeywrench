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

/*
 * This file has been generated. 
 * If you modify it you'll loose your changes.
 */ 


using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;

#pragma warning disable 649

namespace Builder
{
	public partial class DBLanefiles : DBRecord
	{
		public const string TableName = "Lanefiles";

		public DBLanefiles ()
		{
		}
	
		public DBLanefiles (DB db, int id)
			: base (db, id)
		{
		}
	
		public DBLanefiles (IDataReader reader) 
			: base (reader)
		{
		}

		public static void Delete (DB db, int lane_id, int lanefile_id)
		{
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "DELETE FROM Lanefiles WHERE lane_id = @lane_id AND lanefile_id = @lanefile_id;";
				DB.CreateParameter (cmd, "lane_id", lane_id);
				DB.CreateParameter (cmd, "lanefile_id", lanefile_id);
				cmd.ExecuteNonQuery ();
			}
		}
	}
}

