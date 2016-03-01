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
using System.Linq;

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

		protected internal override void Kill (int pid, SynchronizedStreamWriter log)
		{
			this.Kill (new int [] { pid }, log);
		}

		protected internal override void Kill (IEnumerable<int> pids, SynchronizedStreamWriter log)
		{
			foreach (var pid in pids)
				RenderStackTraceWithGdb (pid, log);

			KillImpl (pids, log);
		}

		protected override List<int> GetChildren (int pid)
		{
			return ProcessHelperLinux.GetChildrenImplPS (pid);
		}

		public override void PrintProcesses (SynchronizedStreamWriter log)
		{
			PrintProcessesImplPS (log);
		}

		internal static void PrintProcessesImplPS (SynchronizedStreamWriter log)
		{
			using (var ps = new Job ()) {
				ps.StartInfo.FileName = "ps";
				ps.StartInfo.Arguments = "aux";

				var reader = new ProcessReader (log);
				reader.Setup (ps);
				ps.Start ();
				reader.Start ();

				try {
					if (!ps.WaitForExit (1000 * 30 /* 30 seconds */))
						throw new ApplicationException (string.Format ("The 'ps' process didn't exit in 30 seconds."));
				} finally {
					reader.Join ();
				}
			}
		}

		internal static void RenderStackTraceWithGdb (int pid, SynchronizedStreamWriter log)
		{
			log.WriteLine (string.Format ("\n * Fetching stack trace for process {0} (name '{1}') * \n", pid, GetProcessName (pid)));

			using (var gdb = new Job ()) {
				gdb.StartInfo.FileName = "gdb";
				gdb.StartInfo.Arguments = string.Format ("-ex attach {0} --ex \"info target\" --ex \"info threads\" --ex \"thread apply all bt\" --batch", pid);

				var reader = new ProcessReader (log);
				reader.Setup (gdb);
				gdb.Start ();
				reader.Start ();

				try {
					if (!gdb.WaitForExit (1000 * 30 /* 30 seconds */))
						throw new ApplicationException (string.Format ("The 'gdb' process didn't exit in 30 seconds."));
				} finally {
					reader.Join ();
				}
			}
		}

		/// <summary>
		/// Implementation using kill
		/// </summary>
		/// <param name="pids"></param>
		internal static void KillImpl (IEnumerable<int> pids, SynchronizedStreamWriter log)
		{
			using (Process kill = new Process ()) {
				kill.StartInfo.FileName = "kill";
				kill.StartInfo.Arguments = "-9 ";
				foreach (int pid in pids) {
					kill.StartInfo.Arguments += pid.ToString () + " ";
				}

				log.WriteLine (string.Format ("\n * Killing the processes {0} * ", kill.StartInfo.Arguments.Substring (3)));

				kill.StartInfo.UseShellExecute = false;
				kill.Start ();

				if (!kill.WaitForExit (1000 * 15 /* 15 seconds */))
					throw new ApplicationException (string.Format ("The 'kill' process didn't exit in 15 seconds."));
			}
		}
	}
}
