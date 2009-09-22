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
			KillImpl (pids);
		}

		protected override List<int> GetChildren (int pid)
		{
			return ProcessHelperLinux.GetChildrenImplPgrep (pid);
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

		/// <summary>
		/// Implementation using ps
		/// </summary>
		/// <param name="pid"></param>
		/// <returns></returns>
		internal static List<int> GetChildrenImplPS (int pid)
		{
			string stdout;

			using (Process ps = new Process ()) {
				ps.StartInfo.FileName = "ps";
				ps.StartInfo.Arguments = "-eo ppid,pid";
				ps.StartInfo.UseShellExecute = false;
				ps.StartInfo.RedirectStandardOutput = true;
				ps.Start ();
				stdout = ps.StandardOutput.ReadToEnd ();

				if (!ps.WaitForExit (1000))
					throw new ApplicationException (string.Format ("ps didn't finish in a reasonable amount of time (1 second)."));

				if (ps.ExitCode == 0 && !string.IsNullOrEmpty (stdout.Trim ())) {
					List<int> result = null;
					foreach (string line in stdout.Split (new char [] { '\n', '\r', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) {
						string l = line.Trim ();
						int space = l.IndexOf (' ');
						if (space > 0) {
							string parent = l.Substring (0, space);
							string process = l.Substring (space + 1);
							int parent_id, process_id;

							if (int.TryParse (parent, out parent_id) && int.TryParse (process, out process_id)) {
								if (parent_id == pid) {
									if (result == null)
										result = new List<int> ();
									result.Add (process_id);
								}
							}
						}
					}
					return result;
				}
			}

			return null;
		}

		/// <summary>
		/// Implementation using pgrep
		/// </summary>
		/// <param name="pid"></param>
		/// <returns></returns>
		internal static List<int> GetChildrenImplPgrep (int pid)
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
