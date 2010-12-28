/*
 * DBPerson.generated.cs
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
	public partial class DBPerson : DBRecord
	{
		private string _login;
		private string _password;
		private string _fullname;
		private string _roles;
		private string _irc_nicknames;

		public string @login { get { return _login; } set { _login = value; } }
		public string @password { get { return _password; } set { _password = value; } }
		public string @fullname { get { return _fullname; } set { _fullname = value; } }
		public string @roles { get { return _roles; } set { _roles = value; } }
		public string @irc_nicknames { get { return _irc_nicknames; } set { _irc_nicknames = value; } }


		public override string Table
		{
			get { return "Person"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "login", "password", "fullname", "roles", "irc_nicknames" };
			}
		}
        

	}
}

