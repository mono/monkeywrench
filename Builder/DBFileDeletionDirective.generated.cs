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
	public partial class DBFileDeletionDirective : DBRecord
	{
		private string _name;
		private string _filename;
		private int _match_mode;
		private int _condition;
		private int _x;

		public string @name { get { return _name; } set { _name = value; } }
		public string @filename { get { return _filename; } set { _filename = value; } }
		public int @match_mode { get { return _match_mode; } set { _match_mode = value; } }
		public int @condition { get { return _condition; } set { _condition = value; } }
		public int @x { get { return _x; } set { _x = value; } }


		public override string Table
		{
			get { return "FileDeletionDirective"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "name", "filename", "match_mode", "condition", "x" };
			}
		}
        

	}
}

