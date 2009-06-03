/*
 * DBLane.cs
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
using System.Data;
using System.Data.Common;
using System.Text;

namespace MonkeyWrench.DataClasses
{
	public partial class DBLane : DBRecord
	{
		public static string TableName = "Lane";
		public DBLane ()
		{
		}

		public DBLane (IDataReader reader)
			: base (reader)
		{
		}

	}
}
