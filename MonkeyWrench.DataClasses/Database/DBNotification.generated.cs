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
	public partial class DBNotification : DBRecord
	{
		private string _name;
		private int? _ircidentity_id;
		private int? _emailidentity_id;
		private int? _githubidentity_id;
		private int _mode;
		private int _type;

		public string @name { get { return _name; } set { _name = value; } }
		public int? @ircidentity_id { get { return _ircidentity_id; } set { _ircidentity_id = value; } }
		public int? @emailidentity_id { get { return _emailidentity_id; } set { _emailidentity_id = value; } }
		public int? @githubidentity_id { get { return _githubidentity_id; } set { _githubidentity_id = value; } }
		public int @mode { get { return _mode; } set { _mode = value; } }
		public int @type { get { return _type; } set { _type = value; } }


		public override string Table
		{
			get { return "Notification"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "name", "ircidentity_id", "emailidentity_id", "githubidentity_id", "mode", "type" };
			}
		}
        

	}
}

