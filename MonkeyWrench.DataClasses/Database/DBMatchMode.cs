/*
 * DBMatchMode.cs
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
	public enum DBMatchMode
	{
		ShellGlobs = 0, // space separated list of shell globs
		RegExp = 1, // regexp
		Exact = 2 // exact match
	}

}
