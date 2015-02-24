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
	public partial class DBBuildBotStatus : DBRecord
	{
		private int _host_id;
		private string _version;
		private string _description;
		private DateTime _report_date;

		public int @host_id { get { return _host_id; } set { _host_id = value; } }
		public string @version { get { return _version; } set { _version = value; } }
		public string @description { get { return _description; } set { _description = value; } }
		public DateTime @report_date { get { return _report_date; } set { _report_date = value; } }


		public override string Table
		{
			get { return "BuildBotStatus"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "host_id", "version", "description", "report_date" };
			}
		}
        

	}
}

