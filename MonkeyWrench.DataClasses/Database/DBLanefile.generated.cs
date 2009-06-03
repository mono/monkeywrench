/*
 * DBLanefile.generated.cs
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
	public partial class DBLanefile : DBRecord
	{
		private string _name;
		private string _contents;
		private string _mime;
		private int? _original_id;
		private DateTime? _changed_date;

		public string @name { get { return _name; } set { _name = value; } }
		public string @contents { get { return _contents; } set { _contents = value; } }
		public string @mime { get { return _mime; } set { _mime = value; } }
		public int? @original_id { get { return _original_id; } set { _original_id = value; } }
		public DateTime? @changed_date { get { return _changed_date; } set { _changed_date = value; } }


		public override string Table
		{
			get { return "Lanefile"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "name", "contents", "mime", "original_id", "changed_date" };
			}
		}
        

	}
}

