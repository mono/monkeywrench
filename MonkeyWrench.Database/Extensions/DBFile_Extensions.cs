/*
 * DBFile_Extensions.cs
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
	public static class DBFile_Extensions
	{
		public static DBFile Create (DB db, int id)
		{
			return DBRecord_Extensions.Create (db, new DBFile (), DBFile.TableName, id);
		}

		/// <summary>
		/// This method deletes the record in pg_largeobject too (if the File could be deleted)
		/// </summary>
		/// <param name="db"></param>
		/// <param name="id">File.id</param>
		/// <param name="file_id">File.file_id</param>
		public static void Delete (DB db, int id, int file_id)
		{
			DBRecord_Extensions.Delete (db, id, DBFile.TableName);
			db.Manager.Delete (file_id);
		}


		public static DBFile Find (DB db, string md5)
		{
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM File WHERE md5 = @md5;";
				DB.CreateParameter (cmd, "md5", md5);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						return new DBFile (reader);
				}
			}

			return null;
		}
	}
}
