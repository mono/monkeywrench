/*
 * ProcessHelperMac.cs
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
using System.Linq;
using System.Text;

namespace MonkeyWrench
{
	internal class ProcessHelperMac : IProcessHelper
	{
		protected override List<int> GetChildren (int pid)
		{
			// assume the linux implementation (using pgrep) also work on the mac
			return ProcessHelperLinux.GetChildrenImpl (pid);
		}
	}
}
