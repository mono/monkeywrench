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
	public partial class DBCommand : DBRecord
	{
		private int _lane_id;
		private string _command;
		private string _filename;
		private string _arguments;
		private int _sequence;
		private bool _alwaysexecute;
		private bool _nonfatal;
		private bool _internal;

		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public string @command { get { return _command; } set { _command = value; } }
		public string @filename { get { return _filename; } set { _filename = value; } }
		public string @arguments { get { return _arguments; } set { _arguments = value; } }
		public int @sequence { get { return _sequence; } set { _sequence = value; } }
		public bool @alwaysexecute { get { return _alwaysexecute; } set { _alwaysexecute = value; } }
		public bool @nonfatal { get { return _nonfatal; } set { _nonfatal = value; } }
		public bool @internal { get { return _internal; } set { _internal = value; } }


		public override string Table
		{
			get { return "Command"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "lane_id", "command", "filename", "arguments", "sequence", "alwaysexecute", "nonfatal", "internal" };
			}
		}
        

	}
}

