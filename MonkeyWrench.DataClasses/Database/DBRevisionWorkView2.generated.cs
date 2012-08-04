/*
 * DBRevisionWorkView2.generated.cs
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
	public partial class DBRevisionWorkView2 : DBView
	{
		private int _lane_id;
		private int _host_id;
		private int _revision_id;
		private int _state;
		private bool _completed;
		private DateTime _endtime;
		private string _revision;
		private DateTime _date;
		private string _author;

		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public int @host_id { get { return _host_id; } set { _host_id = value; } }
		public int @revision_id { get { return _revision_id; } set { _revision_id = value; } }
		public int @state { get { return _state; } set { _state = value; } }
		public bool @completed { get { return _completed; } set { _completed = value; } }
		public DateTime @endtime { get { return _endtime; } set { _endtime = value; } }
		public string @revision { get { return _revision; } set { _revision = value; } }
		public DateTime @date { get { return _date; } set { _date = value; } }
		public string @author { get { return _author; } set { _author = value; } }


		public const string SQL = 
@"SELECT 
		RevisionWork.id, RevisionWork.lane_id, RevisionWork.host_id, RevisionWork.revision_id,  
		RevisionWork.state, RevisionWork.completed, RevisionWork.endtime, 
		Revision.revision, Revision.date, Revision.author 
	FROM RevisionWork 
	INNER JOIN Revision ON RevisionWork.revision_id = Revision.id 
	ORDER BY  
		Revision.date DESC;";


		private static string [] _fields_ = new string [] { "lane_id", "host_id", "revision_id", "state", "completed", "endtime", "revision", "date", "author" };
		public override string [] Fields
		{
			get
			{
				return _fields_;
			}
		}
        

		public DBRevisionWorkView2 ()
		{
		}
	
		public DBRevisionWorkView2 (IDataReader reader) 
			: base (reader)
		{
		}


	}
}

