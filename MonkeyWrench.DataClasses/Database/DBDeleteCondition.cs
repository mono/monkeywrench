/*
 * DBDeleteCondition.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

namespace MonkeyWrench.DataClasses
{
	public enum DBDeleteCondition
	{
		Never = 0,
		AfterXDays = 1,
		AfterXBuiltRevisions = 2
	}

}
