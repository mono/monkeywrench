/*
 * DBLanefile_Extensions.cs
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
	public static class DBLanefile_Extensions
	{
		public static DBLanefile Create (DB db, int id)
		{
			return DBRecord_Extensions.Create (db, new DBLanefile (), DBLanefile.TableName, id);
		}

		public static string GetContents (this DBLanefile me, DB db)
		{
			if (me.contents != null && me.contents.Length != 0)
				return me.contents;

			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT contents FROM DBLanefile WHERE id = " + me.id.ToString ();
				me.contents = (string) cmd.ExecuteScalar ();
				return me.contents;
			}
		}

		public static string GetTextContents (this DBLanefile me, DB db)
		{
			return me.contents;
		}

		public static void SetTextContents (this DBLanefile me, string value)
		{
			me.contents = value;
		}


		public static List<DBLane> GetLanesForFile (DB db, DBLanefile lanefile)
		{
			List<DBLane> result = new List<DBLane> ();

			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Lane WHERE Lane.id IN (SELECT DISTINCT lane_id FROM Lanefiles WHERE lanefile_id = @lanefile_id);";
				DB.CreateParameter (cmd, "lanefile_id", lanefile.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBLane (reader));
				}
			}

			return result;
		}
	}
}
