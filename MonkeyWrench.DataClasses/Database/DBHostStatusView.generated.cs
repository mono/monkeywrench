/*
 * DBHostStatusView.generated.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */


/*
 * This file has been generated. 
 * If you modify it you'll loose your changes.
 */ 


using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;

#pragma warning disable 649

namespace MonkeyWrench.DataClasses
{
	public partial class DBHostStatusView : DBView
	{
		private string _host;
		private DateTime _report_date;
		private int _revisionwork_id;
		private int _lane_id;
		private int _revision_id;
		private string _lane;

		public string @host { get { return _host; } set { _host = value; } }
		public DateTime @report_date { get { return _report_date; } set { _report_date = value; } }
		public int @revisionwork_id { get { return _revisionwork_id; } set { _revisionwork_id = value; } }
		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public int @revision_id { get { return _revision_id; } set { _revision_id = value; } }
		public string @lane { get { return _lane; } set { _lane = value; } }


		public const string SQL = 
@"SELECT Host.id, Host.host, BuildBotStatus.report_date, rw2.id AS revisionwork_id, rw2.lane_id AS lane_id, rw2.revision_id, Lane.lane
	FROM Host 
	LEFT JOIN (SELECT id, lane_id, revision_id, workhost_id FROM RevisionWork WHERE state <> 0 AND NOT completed) rw2 ON rw2.workhost_id = Host.id 
	LEFT JOIN Lane ON Lane.id = lane_id 
	INNER JOIN BuildBotStatus ON BuildBotStatus.host_id = Host.id 
	WHERE Host.id IN (
		-- SELECT DISTINCT workhost_id FROM RevisionWork
		-- SELECT DISTINCT is very slow in postgres
		-- the below line does the same thing, but much (20x) faster.
		WITH RECURSIVE t(n) AS (SELECT min(workhost_id) FROM RevisionWork UNION SELECT (SELECT workhost_id FROM revisionwork WHERE workhost_id > n ORDER BY workhost_id LIMIT 1) FROM t WHERE n IS NOT NULL) SELECT n FROM t
		) AND Host.enabled = true
	ORDER BY Lane.lane IS NULL ASC, host ASC;";


		private static string [] _fields_ = new string [] { "host", "report_date", "revisionwork_id", "lane_id", "revision_id", "revision", "lane" };
		public override string [] Fields
		{
			get
			{
				return _fields_;
			}
		}
        

		public DBHostStatusView ()
		{
		}
	
		public DBHostStatusView (IDataReader reader) 
			: base (reader)
		{
		}


	}
}

