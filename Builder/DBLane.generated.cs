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
	public partial class DBLane : DBRecord
	{
		private string _lane;
		private string _source_control;
		private string _repository;
		private string _min_revision;
		private string _max_revision;

		public string @lane { get { return _lane; } set { _lane = value; } }
		public string @source_control { get { return _source_control; } set { _source_control = value; } }
		public string @repository { get { return _repository; } set { _repository = value; } }
		public string @min_revision { get { return _min_revision; } set { _min_revision = value; } }
		public string @max_revision { get { return _max_revision; } set { _max_revision = value; } }


		public override string Table
		{
			get { return "Lane"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "lane", "source_control", "repository", "min_revision", "max_revision" };
			}
		}
        

	}
}

