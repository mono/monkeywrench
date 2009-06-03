/*
 * DBHost.cs
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
	public partial class DBHost : DBRecord
	{
        public const string TableName = "Host";

		public DBHost ()
		{
		}

		public DBHost (IDataReader reader) : base (reader)
		{
		}

		public DBQueueManagement QueueManagement
		{
			get { return (DBQueueManagement) queuemanagement; }
		}

	}
}
