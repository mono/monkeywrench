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
	public partial class DBWork : DBRecord
	{
		private int _host_id;
		private int _command_id;
		private int _state;
		private DateTime _starttime;
		private DateTime _endtime;
		private int _duration;
		private string _logfile;
		private string _summary;
		private int _revisionwork_id;

		public int @host_id { get { return _host_id; } set { _host_id = value; } }
		public int @command_id { get { return _command_id; } set { _command_id = value; } }
		public int @state { get { return _state; } set { _state = value; } }
		public DateTime @starttime { get { return _starttime; } set { _starttime = value; } }
		public DateTime @endtime { get { return _endtime; } set { _endtime = value; } }
		public int @duration { get { return _duration; } set { _duration = value; } }
		public string @logfile { get { return _logfile; } set { _logfile = value; } }
		public string @summary { get { return _summary; } set { _summary = value; } }
		public int @revisionwork_id { get { return _revisionwork_id; } set { _revisionwork_id = value; } }


		public override string Table
		{
			get { return "Work"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "host_id", "command_id", "state", "starttime", "endtime", "duration", "logfile", "summary", "revisionwork_id" };
			}
		}
        

	}
}

