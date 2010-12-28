/*
 * DBIrcIdentity.generated.cs
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
	public partial class DBIrcIdentity : DBRecord
	{
		private string _name;
		private string _servers;
		private string _channels;
		private string _nicks;

		public string @name { get { return _name; } set { _name = value; } }
		public string @servers { get { return _servers; } set { _servers = value; } }
		public string @channels { get { return _channels; } set { _channels = value; } }
		public string @nicks { get { return _nicks; } set { _nicks = value; } }


		public override string Table
		{
			get { return "IrcIdentity"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "name", "servers", "channels", "nicks" };
			}
		}
        

	}
}

