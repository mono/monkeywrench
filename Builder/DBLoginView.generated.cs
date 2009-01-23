/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
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

namespace Builder
{
	public partial class DBLoginView : DBView
	{
		private string _cookie;
		private int _person_id;
		private string _ip4;
		private string _login;
		private string _fullname;

		public string @cookie { get { return _cookie; } }
		public int @person_id { get { return _person_id; } }
		public string @ip4 { get { return _ip4; } }
		public string @login { get { return _login; } }
		public string @fullname { get { return _fullname; } }



		private static string [] _fields_ = new string [] { "id", "cookie", "person_id", "ip4", "login", "fullname" };
		public override string [] Fields
		{
			get
			{
				return _fields_;
			}
		}
        

		public DBLoginView (IDataReader reader) : base (reader)
		{
		}


	}
}

