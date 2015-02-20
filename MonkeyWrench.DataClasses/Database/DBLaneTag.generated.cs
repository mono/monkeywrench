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
	public partial class DBLaneTag : DBRecord
	{
		private int _lane_id;
		private string _tag;

		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public string @tag { get { return _tag; } set { _tag = value; } }


		public override string Table
		{
			get { return "LaneTag"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "lane_id", "tag" };
			}
		}
        

	}
}

