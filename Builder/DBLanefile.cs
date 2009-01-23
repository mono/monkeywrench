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
    public partial class DBLanefile
    {
        public const string TableName = "Lanefile";

	    public DBLanefile ()
		{
		}
        public DBLanefile(DB db, int id)
			: base (db, id)
		{
		}
        public DBLanefile (IDataReader reader) : base (reader)
        {
        }

        public string GetContents(DB db)
        {
            if (contents != null && contents.Length != 0)
                return contents;

            using (IDbCommand cmd = db.Connection.CreateCommand())
            {
                cmd.CommandText = "SELECT contents FROM DBLanefile WHERE id = " + id.ToString();
                contents = (string) cmd.ExecuteScalar ();
                return contents;
            }
        }

        public string GetTextContents(DB db)
        {
            return contents;
        }

        public void SetTextContents(string value)
        {
            contents = value;
        }
    }
}
