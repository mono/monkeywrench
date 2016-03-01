﻿/*
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
using log4net;

namespace MonkeyWrench
{
	public static class ProcessHelper
	{
		static readonly ILog log = LogManager.GetLogger (typeof (ProcessHelper));
		private static IProcessHelper helper;

		/// <summary>
		/// Kills this process and all its child processes (recursively, grand children are killed too).
		/// Also waits until the process has actually exited.
		/// </summary>
		/// <param name="process"></param>
		public static void KillTree (this Process process, SynchronizedStreamWriter log)
		{
			GetHelper ().PrintProcesses (log);
			GetHelper ().KillTree (process, log);
		}

		private static IProcessHelper GetHelper ()
		{
			if (helper == null) {
				switch (Configuration.GetPlatform ()) {
				case Platform.Windows:
					helper = new ProcessHelperWindows ();
					break;
				case Platform.Mac:
					helper = new ProcessHelperMac ();
					break;
				case Platform.Linux:
				default:
					if (Configuration.IsCygwin) {
						helper = new ProcessHelperWindows ();
					} else {
						helper = new ProcessHelperLinux ();
					}
					break;
				}
			}
			return helper;
		}

		// This is just a work around for bug #499702
		public static void Exit (int exitcode)
		{
			GetHelper ().Exit (exitcode);
		}

		public static Job CreateJob ()
		{
			if (Configuration.GetPlatform () == Platform.Windows || Configuration.IsCygwin)
				return new JobWindows ();
			
			return new Job ();
		}

		public static bool Exists (int pid)
		{
			try {
				using (Process p = Process.GetProcessById (pid)) {
				}
				return true;
			} catch (Exception ex) {
				return false;
			}
		}
	}

	internal abstract class IProcessHelper
	{
		protected static string GetProcessName (int pid)
		{
			try {
				var process = Process.GetProcessById (pid);
				return process == null ? "<NIL>" : process.ProcessName;
			} catch {
				return "<error getting process name>";
			}
		}

		protected virtual List<int> GetChildren (int pid)
		{
			if (string.IsNullOrEmpty (Configuration.ChildProcessAlgorithm)) {
				switch (Configuration.GetPlatform ()) {
				case Platform.Mac:
					return GetChildrenImplPS (pid);
				case Platform.Linux:
				default:
					return GetChildrenImplPgrep (pid);
				}
			} else {
				switch (Configuration.ChildProcessAlgorithm.ToLowerInvariant ()) {
				case "pgrep":
					return GetChildrenImplPgrep (pid);
				case "ps":
				default:
					return GetChildrenImplPS (pid);
				}
			}
		}

		/// <summary>
		/// Default Exit implementation (calls Environment.Exit)
		/// </summary>
		/// <param name="exitcode"></param>
		public virtual void Exit (int exitcode)
		{
			Environment.Exit (exitcode);
		}

		/// <summary>
		/// Default KillTree implementation.
		/// </summary>
		/// <param name="p"></param>
		public virtual void KillTree (Process p, SynchronizedStreamWriter log)
		{
			List<int> processes = new List<int> ();
			FindChildren (p.Id, processes);
			Kill (processes, log);
		}

		/// <summary>
		/// Default Kill implementation.
		/// </summary>
		/// <param name="pid"></param>
		protected virtual internal void Kill (int pid, SynchronizedStreamWriter log)
		{
			using (Process p = Process.GetProcessById (pid)) {
				p.Kill ();
			}
		}
		protected virtual internal void Kill (IEnumerable<int> pids, SynchronizedStreamWriter log)
		{
			foreach (int pid in pids) {
				Kill (pid, log);
			}
		}

		protected void FindChildren (int pid, List<int> result)
		{
			List<int> children = GetChildren (pid);
			if (children != null) {
				foreach (int child in children)
					FindChildren (child, result);
			}
			result.Add (pid);
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
					foreach (string line in stdout.Split (new char [] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)) {
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

		public abstract void PrintProcesses (SynchronizedStreamWriter log);
	}
}
