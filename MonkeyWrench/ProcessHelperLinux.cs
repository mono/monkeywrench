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
		[DllImport ("libc.so.6")]
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

		protected override List<int> GetChildren (int pid)
		{
			return ProcessHelperLinux.GetChildrenImpl (pid);
		}

		internal static List<int> GetChildrenImpl (int pid)
		{
			string children;

			using (Process pgrep = new Process ()) {
				pgrep.StartInfo.FileName = "pgrep";
				pgrep.StartInfo.Arguments = "-P " + pid;
				pgrep.StartInfo.UseShellExecute = false;
				pgrep.StartInfo.RedirectStandardOutput = true;
				pgrep.Start ();
				children = pgrep.StandardOutput.ReadToEnd ();

				if (!pgrep.WaitForExit (1000))
					throw new ApplicationException (string.Format ("pgrep didn't finish in a reasonable amount of time (1 second)."));

				if (pgrep.ExitCode == 0 && !string.IsNullOrEmpty (children.Trim ())) {
					List<int> result = new List<int> ();
					foreach (string line in children.Split (new char [] { '\n', '\r', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) {
						result.Add (int.Parse (line));
					}
					return result;
				}
			}

			return null;
		}
	}
}
