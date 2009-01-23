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
	public partial class DBPerson : DBRecord
	{
		public const string TableName = "Person";

		public DBPerson ()
		{
		}
	
		public DBPerson (DB db, int id)
			: base (db, id)
		{
		}
	
		public DBPerson (IDataReader reader) 
			: base (reader)
		{
		}

		public static List<DBPerson> GetAll (DB db)
		{
			List<DBPerson> result = new List<DBPerson> ();

			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
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

