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
	public enum DBMatchMode
	{
		ShellGlobs = 0, // space separated list of shell globs
		RegExp = 1, // regexp
		Exact = 2 // exact match
	}

}
