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
using System.Text;
using System.Data;
using System.Data.Common;

#pragma warning disable 649

namespace Builder
{
	public partial class DBLaneDeletionDirective : DBRecord
	{
		public const string TableName = "LaneDeletionDirective";

		public DBLaneDeletionDirective ()
		{
		}
	
		public DBLaneDeletionDirective (DB db, int id)
			: base (db, id)
		{
		}
	
		public DBLaneDeletionDirective (IDataReader reader) 
			: base (reader)
		{
		}
	}
}

