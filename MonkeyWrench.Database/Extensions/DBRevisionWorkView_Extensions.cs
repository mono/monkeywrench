/*
 * DBRevisionWorkView_Extensions.cs
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
	public static class DBRevisionWorkView_Extensions
	{

		/// <summary>
		/// 
		/// </summary>
		/// <param name="db"></param>
		/// <param name="lane"></param>
		/// <param name="host"></param>
		/// <param name="limit"></param>
		/// <param name="page">First page = 0, second page = 1, etc.</param>
		/// <returns></returns>
		public static List<DBRevisionWorkView> Query (DB db, DBLane lane, DBHost host, int limit, int page)
		{
			Console.WriteLine ("Query {0} {1} {2} {3}", lane, host, limit, page);
			List<DBRevisionWorkView> result = new List<DBRevisionWorkView> ();
			using (IDbTransaction transaction = db.Connection.BeginTransaction ()) {
				using (IDbCommand cmd = db.Connection.CreateCommand ()) {
					// copy&paste from CustomTypes.sql
					cmd.CommandText = @"
SELECT RevisionWork.id, Revision.revision 
	INTO TEMP revisionwork_temptable
	FROM RevisionWork 
	INNER JOIN Revision on RevisionWork.revision_id = Revision.id 
	WHERE RevisionWork.lane_id = @lane_id AND RevisionWork.host_id = @host_id
	ORDER BY Revision.date DESC LIMIT @limit OFFSET @offset;

	SELECT 
		Work.id, Work.command_id, Work.state, Work.starttime, Work.duration, Work.logfile, Work.summary, 
		Host.host, 
		Lane.lane, 
		Revision.author, Revision.revision, 
		Command.command, 
		Command.nonfatal, Command.alwaysexecute, Command.sequence, Command.internal,
		RevisionWork.lane_id, RevisionWork.host_id, RevisionWork.revision_id, 
		RevisionWork.state AS revisionwork_state,
		WorkHost.host AS workhost
	FROM Work
	INNER JOIN RevisionWork ON Work.revisionwork_id = RevisionWork.id
	INNER JOIN Revision ON RevisionWork.revision_id = Revision.id 
	INNER JOIN Lane ON RevisionWork.lane_id = Lane.id 
	INNER JOIN Host ON RevisionWork.host_id = Host.id 
	LEFT JOIN Host AS WorkHost ON RevisionWork.workhost_id = WorkHost.id
	INNER JOIN Command ON Work.command_id = Command.id
	WHERE
		Work.revisionwork_id IN (SELECT id FROM revisionwork_temptable)
	ORDER BY Revision.date DESC; 
";

					DB.CreateParameter (cmd, "lane_id", lane.id);
					DB.CreateParameter (cmd, "host_id", host.id);
					DB.CreateParameter (cmd, "limit", limit);
					DB.CreateParameter (cmd, "offset", page * limit);

					using (IDataReader reader = cmd.ExecuteReader ()) {
						while (reader.Read ()) {
							result.Add (new DBRevisionWorkView (reader));
						}
					}
				}

				return result;
			}
		}
	}
}
