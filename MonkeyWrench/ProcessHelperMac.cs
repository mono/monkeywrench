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
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;

namespace MonkeyWrench
{
	internal class ProcessHelperMac : IProcessHelper
	{
		protected override List<int> GetChildren (int pid)
		{
			// there's no pgrep on the mac, use ps
			return ProcessHelperLinux.GetChildrenImplPS (pid);
		}

		internal static void RenderStackTraceWithGdb (int pid, SynchronizedStreamWriter log)
		{
			log.WriteLine (string.Format ("\n * Fetching stack trace for process {0} (name '{1}') * \n", pid, GetProcessName (pid)));

			var template = Path.GetTempFileName ();
			try {
				bool using_lldb = false;
				string debugger;

				if (File.Exists ("/usr/bin/gdb")) {
					using_lldb = false;
					debugger = "/usr/bin/gdb";
				} else if (File.Exists ("/usr/bin/lldb")) {
					using_lldb = true;
					debugger = "/usr/bin/lldb";
				} else {
					using_lldb = false; // lets hope "gdb" is somewhere.
					debugger = "gdb";
				}

				var commands = new StringBuilder ();
				using (var dbg = new Job ()) {
					dbg.StartInfo.UseShellExecute = false;
					dbg.StartInfo.FileName = debugger;
					if (using_lldb) {
						commands.AppendFormat ("process attach --pid {0}\n", pid);
						commands.Append ("thread list\n");
						commands.Append ("thread backtrace all\n");
						commands.Append ("detach\n");
						commands.Append ("quit\n");
						dbg.StartInfo.Arguments = "--source \"" + template + "\"";
					} else {
						commands.AppendFormat ("attach {0}\n", pid);
						commands.Append ("info target\n");
						commands.Append ("info threads\n");
						commands.Append ("thread apply all bt\n");
						dbg.StartInfo.Arguments = "-batch -x \"" + template + "\"";
					}
					File.WriteAllText (template, commands.ToString ());

					var reader = new ProcessReader (log);
					reader.Setup (dbg);
					dbg.Start ();
					reader.Start ();

					try {
						if (!dbg.WaitForExit (1000 * 30 /* 30 seconds */))
							throw new ApplicationException (string.Format ("The 'gdb' process didn't exit in 30 seconds.", dbg.StartInfo.FileName));
					} finally {
						reader.Join ();
					}
				}
			} finally {
				try {
					File.Delete (template);
				} catch {
				}
			}
		}

		protected internal override void Kill (IEnumerable<int> pids, SynchronizedStreamWriter log)
		{
			foreach (var pid in pids)
				RenderStackTraceWithGdb (pid, log);

			ProcessHelperLinux.KillImpl (pids, log);
		}

		public override void PrintProcesses (SynchronizedStreamWriter log)
		{
			ProcessHelperLinux.PrintProcessesImplPS (log);
		}
	}
}
