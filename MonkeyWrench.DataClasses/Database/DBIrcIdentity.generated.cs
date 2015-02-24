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
	public partial class DBIrcIdentity : DBRecord
	{
		private string _name;
		private string _servers;
		private string _password;
		private string _channels;
		private string _nicks;
		private bool _use_ssl;
		private bool _join_channels;

		public string @name { get { return _name; } set { _name = value; } }
		public string @servers { get { return _servers; } set { _servers = value; } }
		public string @password { get { return _password; } set { _password = value; } }
		public string @channels { get { return _channels; } set { _channels = value; } }
		public string @nicks { get { return _nicks; } set { _nicks = value; } }
		public bool @use_ssl { get { return _use_ssl; } set { _use_ssl = value; } }
		public bool @join_channels { get { return _join_channels; } set { _join_channels = value; } }


		public override string Table
		{
			get { return "IrcIdentity"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "name", "servers", "password", "channels", "nicks", "use_ssl", "join_channels" };
			}
		}
        

	}
}

