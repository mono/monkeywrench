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
    public partial class DBCommand
    {
		public const string TableName = "Command";

        public DBCommand()
        {
        }
        public DBCommand(DB db, int id)
			: base (db, id)
		{
		}
        public DBCommand(IDataReader reader)
            : base(reader)
        {
        }

		public static new void Delete (DB db, int id, string Table)
		{
			DBCommand command = new DBCommand (db, id);

			if (command == null)
				throw new Exception ("Invalid id.");

			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = 
					"DELETE FROM WorkFile WHERE EXISTS (SELECT * FROM Work WHERE Work.lane_id = @lane_id AND Work.command_id = @id AND Work.id = WorkFile.work_id); " + 
					"DELETE FROM Work WHERE Work.lane_id = @lane_id AND Work.command_id = @id; " +
					"DELETE FROM Command WHERE id = @id;";
				DB.CreateParameter (cmd, "lane_id", command.lane_id);
				DB.CreateParameter (cmd, "id", id);
				cmd.ExecuteNonQuery ();
			}
		}
    }
}
