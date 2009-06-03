/*
 * DBRevisionWorkView.cs
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
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;

namespace MonkeyWrench.DataClasses
{
	public partial class DBRevisionWorkView : DBView
	{
		public DBState State
		{
			get { return (DBState) state; }
		}

		public DBState RevisionWorkState
		{
			get { return (DBState) revisionwork_state; }
		}
	}
}
