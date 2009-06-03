/*
 * DBCommand_Extensions.cs
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
	public static class DBCommand_Extensions
	{
		public static DBCommand Create (DB db, int id)
		{
			return DBRecord_Extensions.Create (db, new DBCommand (), DBCommand.TableName, id);
		}

		public static void Delete (DB db, int id, string Table)
		{
			DBCommand command = DBCommand_Extensions.Create (db, id);

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
