/*
 * DBMasterHost.generated.cs
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
	public partial class DBMasterHost : DBRecord
	{
		private int _host_id;
		private int _master_host_id;

		public int @host_id { get { return _host_id; } set { _host_id = value; } }
		public int @master_host_id { get { return _master_host_id; } set { _master_host_id = value; } }


		public override string Table
		{
			get { return "MasterHost"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "host_id", "master_host_id" };
			}
		}
        

	}
}

