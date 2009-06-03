/*
 * DBLanefiles.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;

#pragma warning disable 649

namespace MonkeyWrench.DataClasses
{
	public partial class DBLanefiles : DBRecord
	{
		public const string TableName = "Lanefiles";

		public DBLanefiles ()
		{
		}
	
		public DBLanefiles (IDataReader reader) 
			: base (reader)
		{
		}
	}
}

