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

#pragma warning disable 649

namespace Builder
{
    public partial class DBWorkView
	{
		private string _masterhost;
		private string _workhost;

		public string workhost { get { return _workhost; } }
		public string masterhost { get { return _masterhost; } }

		static DBWorkView ()
		{
			List<string> fields = new List<string> (_fields_);
			for (int i = fields.Count - 1; i >= 0; i--)
				if (fields [i].Contains (":"))
					fields.RemoveAt (i);
			fields.Add ("masterhost");
			fields.Add ("workhost");
			_fields_ = fields.ToArray ();
		}

		public DBState State
        {
            get { return (DBState) state; }
        }
    }
}
