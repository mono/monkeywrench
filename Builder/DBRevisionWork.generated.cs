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
	public partial class DBRevisionWork : DBRecord
	{
		private int _lane_id;
		private int _host_id;
		private int? _workhost_id;
		private int _revision_id;
		private int _state;
		private DateTime _lock_expires;
		private bool _completed;

		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public int @host_id { get { return _host_id; } set { _host_id = value; } }
		public int? @workhost_id { get { return _workhost_id; } set { _workhost_id = value; } }
		public int @revision_id { get { return _revision_id; } set { _revision_id = value; } }
		public int @state { get { return _state; } set { _state = value; } }
		public DateTime @lock_expires { get { return _lock_expires; } set { _lock_expires = value; } }
		public bool @completed { get { return _completed; } set { _completed = value; } }


		public override string Table
		{
			get { return "RevisionWork"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "lane_id", "host_id", "workhost_id", "revision_id", "state", "lock_expires", "completed" };
			}
		}
        

	}
}

