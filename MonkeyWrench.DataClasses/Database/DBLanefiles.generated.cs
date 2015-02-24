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
	public partial class DBLanefiles : DBRecord
	{
		private int _lanefile_id;
		private int _lane_id;

		public int @lanefile_id { get { return _lanefile_id; } set { _lanefile_id = value; } }
		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }


		public override string Table
		{
			get { return "Lanefiles"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "lanefile_id", "lane_id" };
			}
		}
        

	}
}

