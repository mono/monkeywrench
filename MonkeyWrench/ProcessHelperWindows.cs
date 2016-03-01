/*
 * ProcessHelperWindows.cs
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MonkeyWrench
{
	internal class ProcessHelperWindows : IProcessHelper
	{
		public override void PrintProcesses (SynchronizedStreamWriter log)
		{
			log.WriteLine ("IProcessHelper.PrintProcesses is unimplemented on windows");
		}
	}
}
