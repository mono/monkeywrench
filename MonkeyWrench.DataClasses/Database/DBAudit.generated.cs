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
	public partial class DBAudit : DBRecord
	{
		private int? _person_id;
		private string _person_login;
		private string _ip;
		private DateTime _stamp;
		private string _action;

		public int? @person_id { get { return _person_id; } set { _person_id = value; } }
		public string @person_login { get { return _person_login; } set { _person_login = value; } }
		public string @ip { get { return _ip; } set { _ip = value; } }
		public DateTime @stamp { get { return _stamp; } set { _stamp = value; } }
		public string @action { get { return _action; } set { _action = value; } }


		public override string Table
		{
			get { return "Audit"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "person_id", "person_login", "ip", "stamp", "action" };
			}
		}
        

	}
}

