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
	public partial class DBLogin : DBRecord
	{
		private string _cookie;
		private int _person_id;
		private DateTime _expires;
		private string _ip4;

		public string @cookie { get { return _cookie; } set { _cookie = value; } }
		public int @person_id { get { return _person_id; } set { _person_id = value; } }
		public DateTime @expires { get { return _expires; } set { _expires = value; } }
		public string @ip4 { get { return _ip4; } set { _ip4 = value; } }


		public override string Table
		{
			get { return "Login"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "cookie", "person_id", "expires", "ip4" };
			}
		}
        

	}
}

