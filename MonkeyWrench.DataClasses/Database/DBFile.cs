/*
 * DBFile.cs
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

namespace MonkeyWrench.DataClasses
{
	public partial class DBFile
	{
		public const string TableName = "File";

		public DBFile ()
		{
		}

		public DBFile (IDataReader reader)
			: base (reader)
		{
		}
	}
}
