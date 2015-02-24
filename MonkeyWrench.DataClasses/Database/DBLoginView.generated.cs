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
	public partial class DBLoginView : DBView
	{
		private string _cookie;
		private int _person_id;
		private string _ip4;
		private string _login;
		private string _fullname;

		public string @cookie { get { return _cookie; } set { _cookie = value; } }
		public int @person_id { get { return _person_id; } set { _person_id = value; } }
		public string @ip4 { get { return _ip4; } set { _ip4 = value; } }
		public string @login { get { return _login; } set { _login = value; } }
		public string @fullname { get { return _fullname; } set { _fullname = value; } }


		public const string SQL = 
@"SELECT Login.id, Login.cookie, Login.person_id, Login.ip4, Person.login, Person.fullname 
	FROM Login 
		INNER JOIN Person ON Login.person_id = Person.id 
	WHERE expires > now ();";


		private static string [] _fields_ = new string [] { "cookie", "person_id", "ip4", "login", "fullname" };
		public override string [] Fields
		{
			get
			{
				return _fields_;
			}
		}
        

		public DBLoginView ()
		{
		}
	
		public DBLoginView (IDataReader reader) 
			: base (reader)
		{
		}


	}
}

