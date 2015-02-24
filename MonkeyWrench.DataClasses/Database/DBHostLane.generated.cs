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
	public partial class DBHostLane : DBRecord
	{
		private int _host_id;
		private int _lane_id;
		private bool _enabled;
		private bool _hidden;

		public int @host_id { get { return _host_id; } set { _host_id = value; } }
		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public bool @enabled { get { return _enabled; } set { _enabled = value; } }
		public bool @hidden { get { return _hidden; } set { _hidden = value; } }


		public override string Table
		{
			get { return "HostLane"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "host_id", "lane_id", "enabled", "hidden" };
			}
		}
        

	}
}

