/*
 * ProcessHelper.cs
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
using System.Text;

namespace MonkeyWrench
{
	public static class ProcessHelper
	{
		private static IProcessHelper helper;

		/// <summary>
		/// Kills this process and all its child processes (recursively, grand children are killed too).
		/// Also waits until the process has actually exited.
		/// </summary>
		/// <param name="process"></param>
		public static void KillTree (this Process process)
		{
			GetHelper ().KillTree (process);
		}

		private static IProcessHelper GetHelper ()
		{
			if (helper == null)
				helper = new LinuxProcessHelper ();
			return helper;
		}

		// This is just a work around for bug #499702
		public static void Exit (int exitcode)
		{
			GetHelper ().Exit (exitcode);
		}
	}

	internal interface IProcessHelper
	{
		void KillTree (Process p);
		void Exit (int exitcode);
	}

	internal class LinuxProcessHelper : IProcessHelper
	{
		[System.Runtime.InteropServices.DllImport ("libc.so.6")]
		private static extern void exit (int exitcode);

		public void Exit (int exitcode)
		{
			exit (exitcode);
		}

		public void KillTree (Process p)
		{
			List<int> processes = new List<int> ();
			FindChildren (p.Id, processes);

			using (Process kill = new Process ()) {
				kill.StartInfo.FileName = "kill";
				kill.StartInfo.Arguments = "-9 ";
				foreach (int pid in processes) {
					kill.StartInfo.Arguments += pid.ToString () + " ";
				}
				kill.StartInfo.UseShellExecute = false;
				kill.Start ();
			}

			if (!p.WaitForExit (1000 * 15 /* 15 seconds */))
				throw new ApplicationException (string.Format ("The killed process {0} didn't exit.", p.Id));
		}

		private void FindChildren (int pid, List<int> result)
		{
			List<int> children = GetChildren (pid);
			if (children != null) {
				foreach (int child in children)
					FindChildren (child, result);
			}
			result.Add (pid);
		}

		private List<int> GetChildren (int pid)
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
