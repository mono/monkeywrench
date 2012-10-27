/*
 * DBLaneDependencyCondition.cs
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
	public enum DBLaneDependencyCondition
	{
		Invalid = 0,
		DependentLaneSuccess = 1,
		DependentLaneSuccessWithFile = 2,
		DependentLaneIssuesOrSuccess = 3,
	}
}
