using System;
using System.Collections.Generic;
using System.Text;

namespace Builder
{
	partial class DBWorkView2
	{
		private string _masterhost;
		private string _workhost;

		public string masterhost { get { return _masterhost; } }
		public string workhost { get { return _workhost; } }

		public DBState State
		{
			get { return (DBState) state; }
		}
	}
}
