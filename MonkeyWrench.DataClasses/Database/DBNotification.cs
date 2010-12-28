/*
 * DBNotification.cs
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

#pragma warning disable 649

namespace MonkeyWrench.DataClasses
{
	public partial class DBNotification : DBRecord
	{
		public const string TableName = "Notification";

		public DBNotification ()
		{
		}
	
		public DBNotification (IDataReader reader) 
			: base (reader)
		{
		}

		public DBNotificationMode Mode
		{
			get { return (DBNotificationMode) mode; }
			set { mode = (int) value; }
		}

		public DBNotificationType Type
		{
			get { return (DBNotificationType) type; }
			set { type = (int) value; }
		}
	}
}

