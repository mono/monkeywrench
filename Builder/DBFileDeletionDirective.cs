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
	public partial class DBFileDeletionDirective : DBRecord
	{
		public const string TableName = "FileDeletionDirective";

		public DBFileDeletionDirective ()
		{
		}
	
		public DBFileDeletionDirective (DB db, int id)
			: base (db, id)
		{
		}
	
		public DBFileDeletionDirective (IDataReader reader) 
			: base (reader)
		{
		}

		public static List<DBFileDeletionDirective> GetAll (DB db)
		{
			List<DBFileDeletionDirective> result = new List<DBFileDeletionDirective> ();

			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM FileDeletionDirective";
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (new DBFileDeletionDirective (reader));
					}
				}
			}

			return result;
		}
	}
}

