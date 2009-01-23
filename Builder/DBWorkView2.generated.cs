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

namespace Builder
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
		private int _workhost_id;
		private bool _nonfatal;
		private bool _alwaysexecute;
		private int _sequence;
		private bool _internal;
		private string _command;
		private int _masterhost_id;
		private int _lane_id;
		private int _revision_id;
		private string _author;
		private string _revision;

		public string @lane { get { return _lane; } }
		public int @command_id { get { return _command_id; } }
		public int @state { get { return _state; } }
		public DateTime @starttime { get { return _starttime; } }
		public DateTime @endtime { get { return _endtime; } }
		public int @duration { get { return _duration; } }
		public string @logfile { get { return _logfile; } }
		public string @summary { get { return _summary; } }
		public int @workhost_id { get { return _workhost_id; } }
		public bool @nonfatal { get { return _nonfatal; } }
		public bool @alwaysexecute { get { return _alwaysexecute; } }
		public int @sequence { get { return _sequence; } }
		public bool @internal { get { return _internal; } }
		public string @command { get { return _command; } }
		public int @masterhost_id { get { return _masterhost_id; } }
		public int @lane_id { get { return _lane_id; } }
		public int @revision_id { get { return _revision_id; } }
		public string @author { get { return _author; } }
		public string @revision { get { return _revision; } }



		private static string [] _fields_ = new string [] { "id", "lane", "command_id", "state", "starttime", "endtime", "duration", "logfile", "summary", "workhost_id", "nonfatal", "alwaysexecute", "sequence", "internal", "command", "masterhost_id", "lane_id", "revision_id", "masterhost", "workhost", "author", "revision" };
		public override string [] Fields
		{
			get
			{
				return _fields_;
			}
		}
        

		public DBWorkView2 (IDataReader reader) : base (reader)
		{
		}


	}
}

