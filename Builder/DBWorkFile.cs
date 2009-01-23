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


using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;

namespace Builder
{
	public partial class DBWorkFile : DBRecord
	{
		public const string TableName = "WorkFile";

		public DBWorkFile ()
		{
		}
	
		public DBWorkFile (DB db, int id)
			: base (db, id)
		{
		}
	
		public DBWorkFile (IDataReader reader) 
			: base (reader)
		{
		}
	}
}

