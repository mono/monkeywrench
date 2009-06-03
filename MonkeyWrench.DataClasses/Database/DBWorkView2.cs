/*
 * DBWorkView2.cs
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

#pragma warning disable 649 

namespace MonkeyWrench.DataClasses
{
	partial class DBWorkView2
	{
		public DBState State
		{
			get { return (DBState) state; }
		}
	}
}
