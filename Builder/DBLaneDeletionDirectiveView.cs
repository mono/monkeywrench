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
using System.IO;
using System.Reflection;

namespace Builder
{
	public partial class DBLaneDeletionDirectiveView : DBView
	{
		public DBMatchMode MatchMode
		{
			get { return (DBMatchMode) match_mode; }
		}

		public DBDeleteCondition Condition
		{
			get { return (DBDeleteCondition) condition; }
		}

		public static List<DBLaneDeletionDirectiveView> Find (DB db, DBLane lane)
		{
			List<DBLaneDeletionDirectiveView> result = new List<DBLaneDeletionDirectiveView> ();

			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
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
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
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

		public bool IsFileNameMatch (string fn)
		{
			if (string.IsNullOrEmpty (filename))
				return false;

			switch (MatchMode) {
			case DBMatchMode.RegExp:
				return System.Text.RegularExpressions.Regex.IsMatch (fn, filename);
			case DBMatchMode.ShellGlobs:
				foreach (string glob in filename.Split (' ')) {
					if (string.IsNullOrEmpty (glob))
						continue;

					if (System.Text.RegularExpressions.Regex.IsMatch (fn, FileManager.GlobToRegExp (glob)))
						return true;
				}
				return false;
			case DBMatchMode.Exact:
				return fn == filename;
			default:
				return false;
			}
		}
	}
}