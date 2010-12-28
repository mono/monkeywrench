/*
 * DBBuildBotStatus.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
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

namespace MonkeyWrench.DataClasses
{
	public partial class DBBuildBotStatus : DBRecord
	{
		public const string TableName = "BuildBotStatus";

		public DBBuildBotStatus ()
		{
		}
	
		public DBBuildBotStatus (IDataReader reader) 
			: base (reader)
		{
		}
	}
}

