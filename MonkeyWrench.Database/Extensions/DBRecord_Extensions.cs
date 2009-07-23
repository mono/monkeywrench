/*
 * DBRecord_Extensions.cs
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
	public static class DBRecord_Extensions
	{
		public static T Create<T> (DB db, T record, string Table, int id) where T : DBRecord
		{
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM " + Table + " WHERE id = " + id.ToString ();
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						throw new Exception (string.Format ("Could not find the id {0} in the table {1}", id, Table));
					record.Load (reader);
					if (reader.Read ())
						throw new Exception (string.Format ("Found more than one record with the id {0} in the table {1}", id, Table));
				}
			}

			return record;
		}

		public static void Save ( this DBRecord me, DB db)
		{
			me.Save (db);
		}

		public static void Delete (this DBRecord me, DB db)
		{
			me.Delete (db);
		}

		public static void Delete (IDB db, int id, string Table)
		{
			DBRecord.DeleteInternal (db, id, Table);
		}

		public static void Reload (this DBRecord me, IDB db)
		{
			me.Reload (db);
		}

	}
}
