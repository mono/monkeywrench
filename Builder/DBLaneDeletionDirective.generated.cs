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
	public partial class DBLaneDeletionDirective : DBRecord
	{
		private int _lane_id;
		private int _file_deletion_directive_id;
		private bool _enabled;

		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public int @file_deletion_directive_id { get { return _file_deletion_directive_id; } set { _file_deletion_directive_id = value; } }
		public bool @enabled { get { return _enabled; } set { _enabled = value; } }


		public override string Table
		{
			get { return "LaneDeletionDirective"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "lane_id", "file_deletion_directive_id", "enabled" };
			}
		}
        

	}
}

