/*
 * DBEnvironmentVariable.generated.cs
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
	public partial class DBEnvironmentVariable : DBRecord
	{
		private int? _host_id;
		private int? _lane_id;
		private string _name;
		private string _value;

		public int? @host_id { get { return _host_id; } set { _host_id = value; } }
		public int? @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public string @name { get { return _name; } set { _name = value; } }
		public string @value { get { return _value; } set { _value = value; } }


		public override string Table
		{
			get { return "EnvironmentVariable"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "host_id", "lane_id", "name", "value" };
			}
		}
        

	}
}

