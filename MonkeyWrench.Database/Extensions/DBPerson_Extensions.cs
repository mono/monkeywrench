/*
 * DBPerson_Extensions.cs
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
	public static class DBPerson_Extensions
	{
		public static DBPerson Create (DB db, int id)
		{
			return DBRecord_Extensions.Create (db, new DBPerson (), DBPerson.TableName, id);
		}

		public static List<DBPerson> GetAll (DB db)
		{
			List<DBPerson> result = new List<DBPerson> ();

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Person";
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (new DBPerson (reader));
					}
				}
			}

			return result;
		}
	}
}
