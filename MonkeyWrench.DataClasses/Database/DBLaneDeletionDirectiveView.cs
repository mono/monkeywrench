/*
 * DBLaneDeletionDirectiveView.cs
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
	public partial class DBLaneDeletionDirectiveView : DBView
	{
		public DBMatchMode MatchMode
		{
			get { return (DBMatchMode) match_mode; }
		}

		public DBDeleteCondition Condition
		{
			get { return (DBDeleteCondition) condition; }
		}
	}
}