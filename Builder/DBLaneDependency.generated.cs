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
	public partial class DBLaneDependency : DBRecord
	{
		private int _lane_id;
		private int _dependent_lane_id;
		private int? _dependent_host_id;
		private int _condition;
		private string _filename;
		private string _download_files;

		public int @lane_id { get { return _lane_id; } set { _lane_id = value; } }
		public int @dependent_lane_id { get { return _dependent_lane_id; } set { _dependent_lane_id = value; } }
		public int? @dependent_host_id { get { return _dependent_host_id; } set { _dependent_host_id = value; } }
		public int @condition { get { return _condition; } set { _condition = value; } }
		public string @filename { get { return _filename; } set { _filename = value; } }
		public string @download_files { get { return _download_files; } set { _download_files = value; } }


		public override string Table
		{
			get { return "LaneDependency"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "lane_id", "dependent_lane_id", "dependent_host_id", "condition", "filename", "download_files" };
			}
		}
        

	}
}

