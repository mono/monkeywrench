/*
 * DBLaneDependency.cs
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

namespace MonkeyWrench.DataClasses
{
	public partial class DBLaneDependency : DBRecord
	{
		public const string TableName = "LaneDependency";

		public DBLaneDependency ()
		{
		}


		public DBLaneDependency (IDataReader reader)
			: base (reader)
		{
		}

		public DBLaneDependencyCondition Condition
		{
			get { return (DBLaneDependencyCondition) condition; }
			set { condition = (int) value; }
		}
	}
}

