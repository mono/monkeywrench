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

namespace Builder
{
	public partial class DBFile
	{
		public const string TableName = "File";

		public DBFile ()
		{
		}
		public DBFile (DB db, int id)
			: base (db, id)
		{
		}
		public DBFile (IDataReader reader)
			: base (reader)
		{
		}

		public override void Save (IDbConnection connection)
		{
			base.Save (connection);
		}

		public override void Delete (IDbConnection connection)
		{
			base.Delete (connection);
		}

		public override void Load (IDataReader reader)
		{
			base.Load (reader);
		}

		/// <summary>
		/// This method deletes the record in pg_largeobject too (if the File could be deleted)
		/// </summary>
		/// <param name="db"></param>
		/// <param name="id">File.id</param>
		/// <param name="file_id">File.file_id</param>
		public static void Delete (DB db, int id, int file_id)
		{
			Delete (db, id, TableName);
			db.Manager.Delete (file_id);
		}
	}
}
