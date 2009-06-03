/*
 * DBWorkFile_Extensions.cs
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
using System.IO;
using System.Linq;
using System.Text;

using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Database
{
	public static class DBWorkFile_Extensions
	{
		public static DBWorkFile Create (DB db, int id)
		{
			return DBRecord_Extensions.Create (db, new DBWorkFile (), DBWorkFile.TableName, id);
		}


		public static void WriteToDisk (this DBWorkFile wf, DB db, string dir)
		{
			byte [] buffer = new byte [1024];
			int read;
			string filename = Path.Combine (dir, wf.filename);
			DBFile file = DBFile_Extensions.Create (db, wf.file_id);

			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);

			using (Stream stream = db.Download (wf)) {
				using (FileStream fs = new FileStream (filename, FileMode.Create, FileAccess.Write, FileShare.Read)) {
					while (0 != (read = stream.Read (buffer, 0, buffer.Length))) {
						fs.Write (buffer, 0, read);
					}
				}
			}

			if (file.compressed_mime == "application/x-gzip")
				FileUtilities.GZUncompress (filename);
		}
	}
}
