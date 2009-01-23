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
	public partial class DBHostLaneView : DBView
	{
		private int _lane_id;
		private int _host_id;
		private bool _enabled;
		private string _lane;
		private string _host;

		public int @lane_id { get { return _lane_id; } }
		public int @host_id { get { return _host_id; } }
		public bool @enabled { get { return _enabled; } }
		public string @lane { get { return _lane; } }
		public string @host { get { return _host; } }



		private static string [] _fields_ = new string [] { "id", "lane_id", "host_id", "enabled", "lane", "host" };
		public override string [] Fields
		{
			get
			{
				return _fields_;
			}
		}
        

		public DBHostLaneView (IDataReader reader) : base (reader)
		{
		}


	}
}

