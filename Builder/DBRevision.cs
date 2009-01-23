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
using System.Data;
using System.Data.Common;
using System.IO;
using Npgsql;

namespace Builder
{
	public partial class DBRevision : DBRecord
	{
		public DBRevision ()
		{
		}
		public DBRevision (DB db, int id)
			: base (db, id)
		{
		}

		public DBRevision (IDataReader reader)
			: base (reader)
		{
		}

	}
}