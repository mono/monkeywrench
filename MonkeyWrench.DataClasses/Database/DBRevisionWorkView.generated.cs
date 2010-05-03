/*
 * DBRevisionWorkView.generated.cs
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
	public partial class DBRevisionWorkView : DBView
	{
		private int _command_id;
		private int _state;
		private DateTime _starttime;
		private DateTime _endtime;
		private int _duration;
		private string _logfile;
		private string _summary;
		private string _host;
		private string _lane;
		private string _author;
		private string _revision;
		private string _command;
		private bool _nonfatal;
		private bool _alwaysexecute;
		private int _sequence;
		private bool _internal;
		private int _lane_id;
		private int _host_id;
		private int _revision_id;
		private int _revisionwork_state;
		private string _workhost;

		public int @command_id { get { return _command_id; } set { _command_id = value; } }
		public int @state { get { return _state; } set { _state = value; } }
		public DateTime @starttime { get { return _starttime; } set { _starttime = value; } }
		public DateTime @endtime { get { return _endtime; } set { _endtime = value; } }
		public int @duration { get { return _duration; } set { _duration = value; } }
		public string @logfile { get { return _logfile; } set { _logfile = value; } }
		public string @summary { get { return _summary; } set { _summary = value; } }
		public string @host { get { return _host; } set { _host = value; } }
		public string @lane { get { return _lane; } set { _lane = value; } }
		public string @author { get { return _author; } set { _author = value; } }
		public string @revision { get { return _revision; } set { _revision = value; } }
		public string @command { get { return _command; } set { _command = value; } }
		public bool @nonfatal { get { return _nonfatal; } set { _nonfatal = value; } }
		public bool @alwaysexecute { get { return _alwaysexecute; } set { _alwaysexecute = value; } }
		public int @sequence { get { return _sequence; } set { _sequence = value; } }
		public bool @internal { get { return _internal; } set { _internal = value; } }
		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public int @host_id { get { return _host_id; } set { _host_id = value; } }
		public int @revision_id { get { return _revision_id; } set { _revision_id = value; } }
		public int @revisionwork_state { get { return _revisionwork_state; } set { _revisionwork_state = value; } }
		public string @workhost { get { return _workhost; } set { _workhost = value; } }


		public const string SQL = 
@"SELECT  
		Work.id, Work.command_id, Work.state, Work.starttime, Work.endtime, Work.duration, Work.logfile, Work.summary,  
		Host.host,  
		Lane.lane,  
		Revision.author, Revision.revision,  
		Command.command,  
		Command.nonfatal, Command.alwaysexecute, Command.sequence, Command.internal, 
		RevisionWork.lane_id, RevisionWork.host_id, RevisionWork.revision_id,  
		RevisionWork.state AS revisionwork_state, 
		WorkHost.host AS workhost 
	FROM Work 
	INNER JOIN Revision ON Work.revision_id = Revision.id  
	INNER JOIN Lane ON Work.lane_id = Lane.id  
	INNER JOIN Host ON Work.host_id = Host.id  
	INNER JOIN Host AS WorkHost ON RevisionWork.workhost_id = WorkHost.id 
	INNER JOIN Command ON Work.command_id = Command.id 
	INNER JOIN RevisionWork ON Work.revisionwork_id = RevisionWork.id 
	WHERE  
		RevisionWork.host_id = @host_id AND 
		RevisionWork.lane_id = @lane_id AND 
		Work.revisionwork_id IN  
		
	(SELECT RevisionWork.id  
		
		FROM RevisionWork  
		
		INNER JOIN Revision on RevisionWork.id = Revision.id  
		
		WHERE RevisionWork.lane_id = @lane_id AND RevisionWork.host_id = @host_id  
		
		ORDER BY Revision.date DESC LIMIT @limit OFFSET @offset)  
	ORDER BY Revision.date DESC; ";


		private static string [] _fields_ = new string [] { "command_id", "state", "starttime", "endtime", "duration", "logfile", "summary", "host", "lane", "author", "revision", "command", "nonfatal", "alwaysexecute", "sequence", "internal", "lane_id", "host_id", "revision_id", "revisionwork_state", "workhost" };
		public override string [] Fields
		{
			get
			{
				return _fields_;
			}
		}
        

		public DBRevisionWorkView ()
		{
		}
	
		public DBRevisionWorkView (IDataReader reader) 
			: base (reader)
		{
		}


	}
}

