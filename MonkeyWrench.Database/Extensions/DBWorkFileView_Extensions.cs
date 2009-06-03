/*
 * DBWorkFileView_Extensions.cs
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
	public static class DBWorkFileView_Extensions
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

		public static DBWorkFileView Find (DB db, string filename, string lane, string revision, string host)
		{
			DBWorkFileView result;

			if (lane == null)
				throw new ArgumentNullException ("lane");

			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = @"
	SELECT 
		WorkFile.id, WorkFile.work_id, WorkFile.file_id, WorkFile.filename, WorkFile.hidden, 
		File.mime, File.compressed_mime, 
		Command.internal
	FROM WorkFile
		INNER JOIN File ON WorkFile.file_id = File.id
		INNER JOIN Work ON WorkFile.work_id = Work.id
		INNER JOIN Command ON Work.command_id = Command.id
		INNER JOIN RevisionWork ON Work.revisionwork_id = RevisionWork.id
		INNER JOIN Revision ON RevisionWork.revision_id = Revision.id
		INNER JOIN Lane ON RevisionWork.lane_id = Lane.id
		INNER JOIN Host ON RevisionWork.host_id = Host.id
	WHERE
		Lane.lane = @lane
		AND Revision.revision = @revision
		AND WorkFile.filename = @filename
		AND Host.host = @host;
";

				DB.CreateParameter (cmd, "filename", filename);
				DB.CreateParameter (cmd, "lane", lane);
				DB.CreateParameter (cmd, "revision", revision);
				DB.CreateParameter (cmd, "host", host);

				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						return null;

					result = new DBWorkFileView (reader);


					int counter = 0;
					string ids = result.id.ToString ();
					while (reader.Read ()) {
						counter++;
						ids += ", " + reader ["id"];
					}

					if (counter > 0)
						throw new ApplicationException (string.Format ("Found more than one file ({0} too many: {1}).", counter, ids));
				}
			}

			return result;
		}
	}
}
