/*
 * DBEmailIdentity.generated.cs
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
	public partial class DBEmailIdentity : DBRecord
	{
		private string _name;
		private string _email;
		private string _password;

		public string @name { get { return _name; } set { _name = value; } }
		public string @email { get { return _email; } set { _email = value; } }
		public string @password { get { return _password; } set { _password = value; } }


		public override string Table
		{
			get { return "EmailIdentity"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "name", "email", "password" };
			}
		}
        

	}
}

