/*
 * DBNotificationType.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2010 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

namespace MonkeyWrench.DataClasses
{
	public enum DBNotificationType
	{
		FatalFailuresOnly = 0,
		NonFatalFailuresOnly = 1,
		AllFailures = 2
	}

}
