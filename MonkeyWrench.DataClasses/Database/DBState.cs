/*
 * DBState.cs
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
	public enum DBState
	{
		NotDone = 0,
		Executing = 1,
		Failed = 2,
		Success = 3,
		Aborted = 4,
		Timeout = 5,
		Paused = 6,
		Skipped = 7,
		Issues = 8,
		DependencyNotFulfilled = 9
	}

}
