/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */


namespace Builder
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
