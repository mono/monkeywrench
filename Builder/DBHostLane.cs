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

namespace Builder
{
    public partial class DBHostLane
    {        
        public const string TableName = "HostLane";

	    public DBHostLane ()
		{
		}
        public DBHostLane(DB db, int id)
			: base (db, id)
		{
		}
        public DBHostLane(IDataReader reader)
            : base(reader)
        {
        }

    }
}
