/*
 * DBLaneDeletionDirectiveView.generated.cs
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

		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public int @file_deletion_directive_id { get { return _file_deletion_directive_id; } set { _file_deletion_directive_id = value; } }
		public bool @enabled { get { return _enabled; } set { _enabled = value; } }
		public string @name { get { return _name; } set { _name = value; } }
		public string @filename { get { return _filename; } set { _filename = value; } }
		public int @match_mode { get { return _match_mode; } set { _match_mode = value; } }
		public int @condition { get { return _condition; } set { _condition = value; } }
		public int @x { get { return _x; } set { _x = value; } }


		public const string SQL = 
@"SELECT LaneDeletionDirective.id, LaneDeletionDirective.lane_id, LaneDeletionDirective.file_deletion_directive_id, LaneDeletionDirective.enabled, 
		FileDeletionDirective.name, FileDeletionDirective.filename, FileDeletionDirective.match_mode, FileDeletionDirective.condition, FileDeletionDirective.x 
	FROM LaneDeletionDirective 
		INNER JOIN FileDeletionDirective ON FileDeletionDirective.id = LaneDeletionDirective.file_deletion_directive_id;";


		private static string [] _fields_ = new string [] { "lane_id", "file_deletion_directive_id", "enabled", "name", "filename", "match_mode", "condition", "x" };
		public override string [] Fields
		{
			get
			{
				return _fields_;
			}
		}
        

		public DBLaneDeletionDirectiveView ()
		{
		}
	
		public DBLaneDeletionDirectiveView (IDataReader reader) 
			: base (reader)
		{
		}


	}
}

