/*
 * DeletionDirectives.cs
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
using System.Data.Common;
using System.Text;
using System.Threading;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Database
{
	public static class DeletionDirectives
	{
		private static bool is_executing;

		public static bool IsExecuting
		{
			get { return is_executing; }
		}

		private static void LogWithTime (string message, params object [] args)
		{
			Logger.Log (message, args);
		}

		public static void ExecuteAsync ()
		{
			Async.Execute (delegate (object o)
			{
				Execute ();
			});
		}

		public static void Execute ()
		{
			long space_recovered = 0;

			try {
				LogWithTime ("ExecuteDeletionDirectives: Start");
				
				is_executing = true;

				using (DB db = new DB (true)) {
					List<DBLane> lanes = db.GetAllLanes ();
					foreach (DBLane lane in lanes) {
						LogWithTime ("ExecuteDeletionDirectives: Lane = {0} {1}", lane.id, lane.lane);
						List<DBLaneDeletionDirectiveView> directives = DBLaneDeletionDirectiveView_Extensions.Find (db, lane);
						foreach (DBLaneDeletionDirectiveView directive in directives) {
							LogWithTime ("ExecuteDeletionDirectives: Found directive: '{0}' Enabled: {1}, Condition: {2}, Filename: '{3}', MatchMode: {4}, X: {5}",
								directive.name, directive.enabled, directive.Condition, directive.filename, directive.MatchMode, directive.x);

							if (!directive.enabled)
								continue;

							string sql = @"
SELECT 
	WorkFile.id AS workfile_id,
	WorkFile.filename AS workfile_filename,
	File.id AS file_id,
	File.file_id AS file_file_id,
	File.md5,
	File.size,
	Revision.revision
FROM 
	WorkFile
INNER JOIN Work ON Work.id = WorkFile.work_id
INNER JOIN File ON WorkFile.file_id = File.id	
INNER JOIN RevisionWork ON RevisionWork.id = Work.revisionwork_id
INNER JOIN Revision ON Revision.id = RevisionWork.revision_id
WHERE
	RevisionWork.lane_id = @lane_id
	AND RevisionWork.completed = TRUE
";
							switch (directive.Condition) {
							case DBDeleteCondition.AfterXBuiltRevisions:
								sql += string.Format (@" 
AND Revision.id NOT IN (
	SELECT Revision.id
	FROM Revision
	INNER JOIN RevisionWork ON Revision.id = RevisionWork.revision_id
	WHERE 
		RevisionWork.lane_id = @lane_id 
		AND Revision.lane_id = @lane_id
		AND RevisionWork.completed = TRUE
	ORDER BY Revision.date DESC
	LIMIT {0}
)
", (int) directive.x);
								break;
							case DBDeleteCondition.AfterXDays:
								sql += string.Format (@"
AND Work.endtime + interval '{0} days' < now ();
", (int) directive.x);
								break;
							default:
								continue;
							}

							using (IDbCommand cmd = db.CreateCommand (TimeSpan.FromHours (1 /* this is a slow query, have a big timeout */))) {
								cmd.CommandText = sql;
								DB.CreateParameter (cmd, "lane_id", lane.id);
								using (IDataReader reader = cmd.ExecuteReader ()) {
									using (DB write_db = new DB (true)) {
										while (reader.Read ()) {
											int index;
											string workfile_filename;
											int workfile_id;
											int? file_file_id = null;
											int file_id;
											int size;
											string md5;
											bool match;

											workfile_filename = reader.GetString (reader.GetOrdinal ("workfile_filename"));
											match = directive.IsFileNameMatch (workfile_filename);

											if (!match)
												continue;

											index = reader.GetOrdinal ("file_file_id");
											if (!reader.IsDBNull (index))
												file_file_id = reader.GetInt32 (reader.GetOrdinal ("file_file_id"));
											file_id = reader.GetInt32 (reader.GetOrdinal ("file_id"));
											size = reader.GetInt32 (reader.GetOrdinal ("size"));
											md5 = reader.GetString (reader.GetOrdinal ("md5"));
											workfile_id = reader.GetInt32 (reader.GetOrdinal ("workfile_id"));

											LogWithTime ("ExecuteDeletionDirectives:  >Processing: workfile_id: {0}, workfile_filename: '{1}', file_id: {2}, md5: {3}, match: {4}", workfile_id, workfile_filename, file_id, md5, match);

											// delete the work file
											DBRecord_Extensions.Delete (write_db, workfile_id, DBWorkFile.TableName);
											LogWithTime ("ExecuteDeletionDirectives:  >>WorkFile {0} deleted succesfully.", workfile_id);

											// try to delete the file too
											try {
												DBFile_Extensions.Delete (write_db, file_id, file_file_id, md5);
												space_recovered += size;
												LogWithTime ("ExecuteDeletionDirectives:  >>File {0} deleted successfully. Recovered {1} bytes (total {2} bytes).", file_id, size, space_recovered);
											} catch (Exception ex) {
												LogWithTime ("ExecuteDeletionDirectives:  >>Could not delete File (since the File is used somewhere else, this is normal): {0}", ex.Message);
											}
										}
									}
								}
							}
						}
					}
				}

				LogWithTime ("ExecuteDeletionDirectives: Deleted {0} bytes ({1:#0.0} Kb, {2:#0.00} Mb, {3:#0.000} Gb, {4:#0.0000} Tb)",
					space_recovered, space_recovered / (double) 1024, space_recovered / (double) (1024 * 1024), space_recovered / (double) (1024 * 1024 * 1024), space_recovered / (double) (1024 * 1024 * 1024 * 1024L));
			} catch (Exception ex) {
				LogWithTime ("ExecuteDeletionDirectives: Exception: {0}", ex);
			} finally {
				LogWithTime ("ExecuteDeletionDirectives: Done");
				is_executing = false;
			}
		}
	}
}
