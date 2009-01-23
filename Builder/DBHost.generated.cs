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
	public partial class DBHost : DBRecord
	{
		private string _host;
		private string _description;
		private string _architecture;
		private int _queuemanagement;

		public string @host { get { return _host; } set { _host = value; } }
		public string @description { get { return _description; } set { _description = value; } }
		public string @architecture { get { return _architecture; } set { _architecture = value; } }
		public int @queuemanagement { get { return _queuemanagement; } set { _queuemanagement = value; } }


		public override string Table
		{
			get { return "Host"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "host", "description", "architecture", "queuemanagement" };
			}
		}
        

	}
}

