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
	public partial class DBLaneDeletionDirectiveView : DBView
	{
		private int _lane_id;
		private int _file_deletion_directive_id;
		private bool _enabled;
		private string _name;
		private string _filename;
		private int _match_mode;
		private int _condition;
		private int _x;

		public int @lane_id { get { return _lane_id; } }
		public int @file_deletion_directive_id { get { return _file_deletion_directive_id; } }
		public bool @enabled { get { return _enabled; } }
		public string @name { get { return _name; } }
		public string @filename { get { return _filename; } }
		public int @match_mode { get { return _match_mode; } }
		public int @condition { get { return _condition; } }
		public int @x { get { return _x; } }



		private static string [] _fields_ = new string [] { "id", "lane_id", "file_deletion_directive_id", "enabled", "name", "filename", "match_mode", "condition", "x" };
		public override string [] Fields
		{
			get
			{
				return _fields_;
			}
		}
        

		public DBLaneDeletionDirectiveView (IDataReader reader) : base (reader)
		{
		}


	}
}

