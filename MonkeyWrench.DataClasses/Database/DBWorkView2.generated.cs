/*
 * DBWorkView2.generated.cs
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
	public partial class DBWorkView2 : DBView
	{
		private string _lane;
		private int _command_id;
		private int _state;
		private DateTime _starttime;
		private DateTime _endtime;
		private int _duration;
		private string _logfile;
		private string _summary;
		private int? _workhost_id;
		private bool _nonfatal;
		private bool _alwaysexecute;
		private int _sequence;
		private bool _internal;
		private string _command;
		private int _revisionwork_id;
		private int _masterhost_id;
		private int _lane_id;
		private int _revision_id;
		private string _masterhost;
		private string _workhost;
		private string _author;
		private string _revision;

		public string @lane { get { return _lane; } set { _lane = value; } }
		public int @command_id { get { return _command_id; } set { _command_id = value; } }
		public int @state { get { return _state; } set { _state = value; } }
		public DateTime @starttime { get { return _starttime; } set { _starttime = value; } }
		public DateTime @endtime { get { return _endtime; } set { _endtime = value; } }
		public int @duration { get { return _duration; } set { _duration = value; } }
		public string @logfile { get { return _logfile; } set { _logfile = value; } }
		public string @summary { get { return _summary; } set { _summary = value; } }
		public int? @workhost_id { get { return _workhost_id; } set { _workhost_id = value; } }
		public bool @nonfatal { get { return _nonfatal; } set { _nonfatal = value; } }
		public bool @alwaysexecute { get { return _alwaysexecute; } set { _alwaysexecute = value; } }
		public int @sequence { get { return _sequence; } set { _sequence = value; } }
		public bool @internal { get { return _internal; } set { _internal = value; } }
		public string @command { get { return _command; } set { _command = value; } }
		public int @revisionwork_id { get { return _revisionwork_id; } set { _revisionwork_id = value; } }
		public int @masterhost_id { get { return _masterhost_id; } set { _masterhost_id = value; } }
		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public int @revision_id { get { return _revision_id; } set { _revision_id = value; } }
		public string @masterhost { get { return _masterhost; } set { _masterhost = value; } }
		public string @workhost { get { return _workhost; } set { _workhost = value; } }
		public string @author { get { return _author; } set { _author = value; } }
		public string @revision { get { return _revision; } set { _revision = value; } }


		public const string SQL = 
@"SELECT  
		Work.id, Lane.lane, Work.command_id, Work.state,  
		Work.starttime, Work.endtime, Work.duration, Work.logfile, Work.summary, Work.host_id AS workhost_id,  
		Command.nonfatal, Command.alwaysexecute, Command.sequence, Command.internal, Command.command, 
		RevisionWork.id AS revisionwork_id, 
		RevisionWork.host_id AS masterhost_id, 
		RevisionWork.lane_id, 
		RevisionWork.revision_id, 
		MasterHost.host AS masterhost,  
		WorkHost.host AS workhost, 
		Revision.author, Revision.revision 
	FROM Work  
		INNER JOIN RevisionWork ON Work.revisionwork_id = RevisionWork.id 
		INNER JOIN Revision ON RevisionWork.revision_id = Revision.id  
		INNER JOIN Lane ON RevisionWork.lane_id = Lane.id  
		INNER JOIN Host AS MasterHost ON RevisionWork.host_id = MasterHost.id  
		LEFT JOIN Host AS WorkHost ON Work.host_id = WorkHost.id 
		INNER JOIN Command ON Work.command_id = Command.id;";


		private static string [] _fields_ = new string [] { "lane", "command_id", "state", "starttime", "endtime", "duration", "logfile", "summary", "workhost_id", "nonfatal", "alwaysexecute", "sequence", "internal", "command", "revisionwork_id", "masterhost_id", "lane_id", "revision_id", "masterhost", "workhost", "author", "revision" };
		public override string [] Fields
		{
			get
			{
				return _fields_;
			}
		}
        

		public DBWorkView2 ()
		{
		}
	
		public DBWorkView2 (IDataReader reader) 
			: base (reader)
		{
		}


	}
}

