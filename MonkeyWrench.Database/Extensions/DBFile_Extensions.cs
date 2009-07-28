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
using System.IO;
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
		public static void Delete (DB db, int id, int? file_id, string md5)
		{
			DBRecord_Extensions.Delete (db, id, DBFile.TableName);
			if (file_id.HasValue)
				db.Manager.Delete (file_id.Value);
			if (!string.IsNullOrEmpty (md5)) {
				string fullpath = GetFullPath (md5);
				if (!string.IsNullOrEmpty (fullpath) && File.Exists (fullpath)) {
					File.Delete (fullpath);
				}
			}
		}


		public static DBFile Find (DB db, string md5)
		{
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM File WHERE md5 = @md5;";
				DB.CreateParameter (cmd, "md5", md5);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						return new DBFile (reader);
				}
			}

			return null;
		}

		public static string GetFullPath (string md5)
		{
			string result = Configuration.GetFilesDirectory ();
			string name = md5;

			if (!result.EndsWith (Path.DirectorySeparatorChar.ToString ()))
				result += Path.DirectorySeparatorChar;

			do {
				result += name [0];
				name = name.Substring (1);

				if (Directory.Exists (result)) {
					result += Path.DirectorySeparatorChar;
					if (File.Exists (Path.Combine (result, md5))) {
						return Path.Combine (result, md5);
					} else if (File.Exists (Path.Combine (result, md5) + ".gz")) {
						return Path.Combine (result, md5) + ".gz";
					}
				}
			} while (!string.IsNullOrEmpty (name));

			if (File.Exists (result))
				return result;

			if (File.Exists (result + ".gz"))
				return result + ".gz";

			// we now have the directory of the file
			result = Path.Combine (result, md5);

			if (File.Exists (result))
				return result;

			if (File.Exists (result + ".gz"))
				return result + ".gz";

			return null;
		}

	}
}
