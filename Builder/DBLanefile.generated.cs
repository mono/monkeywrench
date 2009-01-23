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
	public partial class DBLanefile : DBRecord
	{
		private int _lane_id;
		private string _name;
		private string _contents;
		private string _mime;

		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public string @name { get { return _name; } set { _name = value; } }
		public string @contents { get { return _contents; } set { _contents = value; } }
		public string @mime { get { return _mime; } set { _mime = value; } }


		public override string Table
		{
			get { return "Lanefile"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "lane_id", "name", "contents", "mime" };
			}
		}
        

	}
}

