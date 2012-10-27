/*
 * DBHostLaneView.generated.cs
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
	public partial class DBHostLaneView : DBView
	{
		private int _lane_id;
		private int _host_id;
		private bool _enabled;
		private string _lane;
		private string _host;
		private bool _hidden;

		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public int @host_id { get { return _host_id; } set { _host_id = value; } }
		public bool @enabled { get { return _enabled; } set { _enabled = value; } }
		public string @lane { get { return _lane; } set { _lane = value; } }
		public string @host { get { return _host; } set { _host = value; } }
		public bool @hidden { get { return _hidden; } set { _hidden = value; } }


		public const string SQL = 
@"SELECT HostLane.id, HostLane.lane_id, HostLane.host_id, HostLane.enabled, Lane.lane, Host.host, HostLane.hidden 
	FROM HostLane 
		INNER JOIN Host ON HostLane.host_id = Host.id 
		INNER JOIN Lane ON HostLane.lane_id = Lane.id;";


		private static string [] _fields_ = new string [] { "lane_id", "host_id", "enabled", "lane", "host", "hidden" };
		public override string [] Fields
		{
			get
			{
				return _fields_;
			}
		}
        

		public DBHostLaneView ()
		{
		}
	
		public DBHostLaneView (IDataReader reader) 
			: base (reader)
		{
		}


	}
}

