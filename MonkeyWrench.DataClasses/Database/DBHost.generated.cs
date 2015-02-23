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
	public partial class DBHost : DBRecord
	{
		private string _host;
		private string _description;
		private string _architecture;
		private int _queuemanagement;
		private bool _enabled;
		private int? _release_id;

		public string @host { get { return _host; } set { _host = value; } }
		public string @description { get { return _description; } set { _description = value; } }
		public string @architecture { get { return _architecture; } set { _architecture = value; } }
		public int @queuemanagement { get { return _queuemanagement; } set { _queuemanagement = value; } }
		public bool @enabled { get { return _enabled; } set { _enabled = value; } }
		public int? @release_id { get { return _release_id; } set { _release_id = value; } }


		public override string Table
		{
			get { return "Host"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "host", "description", "architecture", "queuemanagement", "enabled", "release_id" };
			}
		}
        

	}
}

