/*
 * DBEnvironmentVariable_Extensions.cs
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
	public static class DBEnvironmentVariable_Extensions
	{

		public static List<DBEnvironmentVariable> Find (DB db, int? lane_id, int? masterhost_id, int? workhost_id)
		{
			List<DBEnvironmentVariable> result = null;


			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM EnvironmentVariable WHERE 1 = 1 ";

				if (lane_id.HasValue) {
					cmd.CommandText += " AND lane_id = @lane_id";
					DB.CreateParameter (cmd, "lane_id", lane_id.Value);
				} else {
					cmd.CommandText += " AND lane_id IS NULL";
				}

				if (masterhost_id.HasValue && workhost_id.HasValue) {
					cmd.CommandText += " AND host_id = @masterhost_id OR host_id = @workhost_id";
					DB.CreateParameter (cmd, "masterhost_id", masterhost_id.Value);
					DB.CreateParameter (cmd, "workhost_id", workhost_id.Value);
				} else if (masterhost_id.HasValue) {
					cmd.CommandText += " AND host_id = @masterhost_id";
					DB.CreateParameter (cmd, "masterhost_id", masterhost_id.Value);
				} else if (workhost_id.HasValue) {
					cmd.CommandText += " AND host_id = @workhost_id";
					DB.CreateParameter (cmd, "workhost_id", workhost_id.Value);
				} else {
					cmd.CommandText += " AND host_id IS NULL";
				}

				cmd.CommandText += " ORDER BY id ASC;";

				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						if (result == null)
							result = new List<DBEnvironmentVariable> ();
						result.Add (new DBEnvironmentVariable (reader));
					}
				}
			}

			return result;
		}
	}
}
