/*
 * DBView.cs
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
	public abstract class DBView : DBRecord
	{
		public DBView ()
			: base ()
		{
		}

		public DBView (IDataReader reader)
			: base (reader)
		{
		}

		public override void Save (System.Data.IDbConnection connection)
		{
			throw new Exception ("Can't save a View");
		}

		public override void Delete (System.Data.IDbConnection connection)
		{
			throw new Exception ("Can't delete a View");
		}

		public override string Table
		{
			get { throw new Exception ("A view doesn't have a table"); }
		}
	}
}
