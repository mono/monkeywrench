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
using System.IO;
using System.Text;
using System.Data;
using System.Data.Common;

namespace Builder
{
	public partial class DBWorkFile : DBRecord
	{
		public const string TableName = "WorkFile";

		public DBWorkFile ()
		{
		}
	
		public DBWorkFile (DB db, int id)
			: base (db, id)
		{
		}
	
		public DBWorkFile (IDataReader reader) 
			: base (reader)
		{
		}

		public void WriteToDisk (DB db, string dir)
		{
			byte [] buffer = new byte [1024];
			int read;
			string filename = Path.Combine (dir, this.filename);
			DBFile file = new DBFile (db, file_id);

			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);

			using (Stream stream = db.Download (this)) {
				using (FileStream fs = new FileStream (filename, FileMode.Create, FileAccess.Write, FileShare.Read)) {
					while (0 != (read = stream.Read (buffer, 0, buffer.Length))) {
						fs.Write (buffer, 0, read);
					}
				}
			}

			if (file.compressed_mime == "application/x-gzip")
				FileManager.GZUncompress (filename);
		}
	}
}

