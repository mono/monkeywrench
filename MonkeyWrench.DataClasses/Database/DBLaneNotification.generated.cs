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
	public partial class DBLaneNotification : DBRecord
	{
		private int _lane_id;
		private int _notification_id;

		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public int @notification_id { get { return _notification_id; } set { _notification_id = value; } }


		public override string Table
		{
			get { return "LaneNotification"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "lane_id", "notification_id" };
			}
		}
        

	}
}

