/*
 * ProcessHelperLinux.cs
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
	internal class ProcessHelperLinux : IProcessHelper
	{
		[DllImport ("libc")]
		private static extern void exit (int exitcode);

		public override void Exit (int exitcode)
		{
			// Work around #499702
			exit (exitcode);
		}

		protected internal override void Kill (int pid)
		{
			this.Kill (new int [] { pid });
		}

		protected internal override void Kill (IEnumerable<int> pids)
		{
			KillImpl (pids);
		}

		protected override List<int> GetChildren (int pid)
		{
			return ProcessHelperLinux.GetChildrenImplPS (pid);
		}

		/// <summary>
		/// Implementation using kill
		/// </summary>
		/// <param name="pids"></param>
		internal static void KillImpl (IEnumerable<int> pids)
		{
			using (Process kill = new Process ()) {
				kill.StartInfo.FileName = "kill";
				kill.StartInfo.Arguments = "-9 ";
				foreach (int pid in pids) {
					kill.StartInfo.Arguments += pid.ToString () + " ";
				}
				kill.StartInfo.UseShellExecute = false;
				kill.Start ();

				if (!kill.WaitForExit (1000 * 15 /* 15 seconds */))
					throw new ApplicationException (string.Format ("The 'kill' process didn't exit in 15 seconds."));
			}
		}
	}
}
