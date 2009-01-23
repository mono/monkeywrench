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
	public partial class DBRevision : DBRecord
	{
		private int _lane_id;
		private string _revision;
		private string _author;
		private DateTime _date;
		private string _log;
		private string _diff;

		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public string @revision { get { return _revision; } set { _revision = value; } }
		public string @author { get { return _author; } set { _author = value; } }
		public DateTime @date { get { return _date; } set { _date = value; } }
		public string @log { get { return _log; } set { _log = value; } }
		public string @diff { get { return _diff; } set { _diff = value; } }


		public override string Table
		{
			get { return "Revision"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "lane_id", "revision", "author", "date", "log", "diff" };
			}
		}
        

	}
}

