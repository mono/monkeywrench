
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;

#pragma warning disable 649

namespace Builder
{
	public partial class DBRevisionWorkView : DBView
	{
		private int _revision_id;
		private int _lane_id;
		private int _host_id;
		private int _command_id;
		private int _state;
		private DateTime _starttime;
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
		private int _revisionwork_state;

		public int @revision_id { get { return _revision_id; } }
		public int @lane_id { get { return _lane_id; } }
		public int @host_id { get { return _host_id; } }
		public int @command_id { get { return _command_id; } }
		public int @state { get { return _state; } }
		public DateTime @starttime { get { return _starttime; } }
		public int @duration { get { return _duration; } }
		public string @logfile { get { return _logfile; } }
		public string @summary { get { return _summary; } }
		public string @host { get { return _host; } }
		public string @lane { get { return _lane; } }
		public string @author { get { return _author; } }
		public string @revision { get { return _revision; } }
		public string @command { get { return _command; } }
		public bool @nonfatal { get { return _nonfatal; } }
		public bool @alwaysexecute { get { return _alwaysexecute; } }
		public int @sequence { get { return _sequence; } }
		public bool @internal { get { return _internal; } }
		public int @revisionwork_state { get { return _revisionwork_state; } }



		public override string [] Fields
		{
			get
			{
				return new string [] { "id", "revision_id", "lane_id", "host_id", "command_id", "state", "starttime", "duration", "logfile", "summary", "host", "lane", "author", "revision", "command", "nonfatal", "alwaysexecute", "sequence", "internal", "revisionwork_state" };
			}
		}
        

		public DBRevisionWorkView (IDataReader reader) : base (reader)
		{
		}


	}
}

