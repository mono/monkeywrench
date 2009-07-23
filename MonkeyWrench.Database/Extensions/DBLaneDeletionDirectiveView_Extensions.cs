/*
 * DBLaneDeletionDirectiveView_Extensions.cs
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
	public static class DBLaneDeletionDirectiveView_Extensions
	{

		public static List<DBLaneDeletionDirectiveView> Find (DB db, DBLane lane)
		{
			List<DBLaneDeletionDirectiveView> result = new List<DBLaneDeletionDirectiveView> ();

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = @"SELECT * FROM LaneDeletionDirectiveView WHERE lane_id = @lane_id";
				DB.CreateParameter (cmd, "lane_id", lane.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ())
						result.Add (new DBLaneDeletionDirectiveView (reader));
				}
			}

			return result;
		}

		public static DBLaneDeletionDirectiveView Find (DB db, int file_deletion_directive_id, int lane_id)
		{
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = @"SELECT * From LaneDeletionDirectiveView WHERE lane_id = @lane_id AND file_deletion_directive_id = @file_deletion_directive_id;";
				DB.CreateParameter (cmd, "lane_id", lane_id);
				DB.CreateParameter (cmd, "file_deletion_directive_id", file_deletion_directive_id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						return new DBLaneDeletionDirectiveView (reader);
					return null;
				}
			}
		}

		public static bool IsFileNameMatch (this DBLaneDeletionDirectiveView me, string fn)
		{
			if (string.IsNullOrEmpty (me.filename))
				return false;

			switch (me.MatchMode) {
			case DBMatchMode.RegExp:
				return System.Text.RegularExpressions.Regex.IsMatch (fn, me.filename);
			case DBMatchMode.ShellGlobs:
				foreach (string glob in me.filename.Split (' ')) {
					if (string.IsNullOrEmpty (glob))
						continue;

					if (System.Text.RegularExpressions.Regex.IsMatch (fn, FileUtilities.GlobToRegExp (glob)))
						return true;
				}
				return false;
			case DBMatchMode.Exact:
				return fn == me.filename;
			default:
				return false;
			}
		}
	}
}
