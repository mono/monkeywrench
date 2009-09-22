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
using System.IO;
using System.Runtime.InteropServices;
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
			if (helper == null) {
				switch (Environment.OSVersion.Platform) {
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.WinCE:
					// case PlatformID.Xbox:
					helper = new ProcessHelperWindows ();
					break;
				case PlatformID.MacOSX:
					helper = new ProcessHelperMac ();
					break;
				case PlatformID.Unix:
				case (PlatformID) 128:
				default:
					helper = new ProcessHelperLinux ();
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
	}

	internal abstract class IProcessHelper
	{
		protected abstract List<int> GetChildren (int pid);

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
		public virtual void KillTree (Process p)
		{
			List<int> processes = new List<int> ();
			FindChildren (p.Id, processes);
			Kill (processes);
		}

		/// <summary>
		/// Default Kill implementation.
		/// </summary>
		/// <param name="pid"></param>
		protected virtual internal void Kill (int pid)
		{
			using (Process p = Process.GetProcessById (pid)) {
				p.Kill ();
			}
		}
		protected virtual internal void Kill (IEnumerable<int> pids)
		{
			foreach (int pid in pids) {
				Kill (pid);
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

	}
}
