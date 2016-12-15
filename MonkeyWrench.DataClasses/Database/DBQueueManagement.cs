/*
 * DBQueueManagement.cs
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

namespace MonkeyWrench.DataClasses
{
	public enum DBQueueManagement
	{
		FinishBeforeNew = 0,
		ExecuteLatestAsap = 1, // currently same as FinishBeforeNew (i.e. ignored)
		OneRevisionWorkAtATime = 2, //
		ChooseHighestPriorityLeastRecent = 3
	}
}
