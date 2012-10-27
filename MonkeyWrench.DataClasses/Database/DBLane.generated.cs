/*
 * DBLane.generated.cs
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
	public partial class DBLane : DBRecord
	{
		private string _lane;
		private string _source_control;
		private string _repository;
		private string _min_revision;
		private string _max_revision;
		private int? _parent_lane_id;
		private string _commit_filter;
		private bool _traverse_merge;

		public string @lane { get { return _lane; } set { _lane = value; } }
		public string @source_control { get { return _source_control; } set { _source_control = value; } }
		public string @repository { get { return _repository; } set { _repository = value; } }
		public string @min_revision { get { return _min_revision; } set { _min_revision = value; } }
		public string @max_revision { get { return _max_revision; } set { _max_revision = value; } }
		public int? @parent_lane_id { get { return _parent_lane_id; } set { _parent_lane_id = value; } }
		public string @commit_filter { get { return _commit_filter; } set { _commit_filter = value; } }
		public bool @traverse_merge { get { return _traverse_merge; } set { _traverse_merge = value; } }


		public override string Table
		{
			get { return "Lane"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "lane", "source_control", "repository", "min_revision", "max_revision", "parent_lane_id", "commit_filter", "traverse_merge" };
			}
		}
        

	}
}

