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
	public partial class DBGitHubIdentity : DBRecord
	{
		private string _name;
		private string _username;
		private string _token;

		public string @name { get { return _name; } set { _name = value; } }
		public string @username { get { return _username; } set { _username = value; } }
		public string @token { get { return _token; } set { _token = value; } }


		public override string Table
		{
			get { return "GitHubIdentity"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "name", "username", "token" };
			}
		}
        

	}
}

