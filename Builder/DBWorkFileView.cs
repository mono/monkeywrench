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
using System.Data;
using System.Data.Common;
using System.Text;

namespace Builder
{
	public partial class DBWorkFileView : DBView
	{
		public static DBWorkFileView Find (DB db, int workfile_id)
		{
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM WorkFileView WHERE id = @id;";
				DB.CreateParameter (cmd, "id", workfile_id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						return null;

					return new DBWorkFileView (reader);
				}
			}
		}
	}
}
