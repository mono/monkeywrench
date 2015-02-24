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
	public partial class DBRevision : DBRecord
	{
		private int _lane_id;
		private string _revision;
		private string _author;
		private DateTime _date;
		private string _log;
		private int? _log_file_id;
		private string _diff;
		private int? _diff_file_id;

		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public string @revision { get { return _revision; } set { _revision = value; } }
		public string @author { get { return _author; } set { _author = value; } }
		public DateTime @date { get { return _date; } set { _date = value; } }
		public string @log { get { return _log; } set { _log = value; } }
		public int? @log_file_id { get { return _log_file_id; } set { _log_file_id = value; } }
		public string @diff { get { return _diff; } set { _diff = value; } }
		public int? @diff_file_id { get { return _diff_file_id; } set { _diff_file_id = value; } }


		public override string Table
		{
			get { return "Revision"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "lane_id", "revision", "author", "date", "log", "log_file_id", "diff", "diff_file_id" };
			}
		}
        

	}
}

