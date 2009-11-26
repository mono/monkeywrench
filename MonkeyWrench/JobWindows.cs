/*
 * JobWindows.cs: Job implementation for Windows
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
using System.Threading;

namespace MonkeyWrench
{
	public class JobWindows : Job
	{
		private IntPtr job_handle;

		public override void Dispose ()
		{
			if (job_handle != IntPtr.Zero) {
				CloseHandle (job_handle);
				job_handle = IntPtr.Zero;
			}

			base.Dispose ();
		}

		private static int mutex_counter = 0;

		public override void Start ()
		{
			/*
			 * Here we create a job object and assign the process to the job object. The problem is that there is
			 * a race condition between creating the process and assigning it to the job object - if the process is
			 * fast enough it can create children which wouldn't be a part of the job. Win32 allows you to create
			 * a process in a suspended state, attach it to the job, and then resume the process. The only way to do
			 * this is to use p/invokes to create the process. This however gets messy since we want to redirect
			 * streams etc (we'd end up re-creating a lot of the code in Process), so use an easier hack: execute
			 * another program that only waits for the process to be attached to the job, then execute what we wanted
			 * to do in the first place. To avoid creating another binary, we re-execute ourselves (there is code in
			 * Configuration.ExecuteSuspendedProcessHack to deal with this.
			 */
			Mutex suspended_mutex; // this is the mutex our child waits for before continuing
			string mutex_name = @"Local\MonkeyWrench-suspended-mutex-" + Process.GetCurrentProcess ().Id.ToString () + "-" + Interlocked.Increment (ref mutex_counter).ToString ();
			suspended_mutex = new Mutex (true, mutex_name);
			
			p.StartInfo.Arguments = "/respawn " + mutex_name + " \"" + p.StartInfo.FileName + "\" " + p.StartInfo.Arguments;
			p.StartInfo.FileName = System.Reflection.Assembly.GetEntryAssembly ().Location;

			if (Type.GetType ("Mono.Runtime") != null) {
				/* 
				 * If we're executing with mono now, we need to execute again with mono.
				 * No idea why, but make fails on cygwin otherwise (when invoking a recursive
				 * make, make claims that it can't find make)
				 */
				p.StartInfo.Arguments = "\"" + p.StartInfo.FileName + "\" " + p.StartInfo.Arguments;
				p.StartInfo.FileName = "mono";
			}

			p.Start (); // start the child, it'll wait for the suspended_mutex to be released.

			job_handle = CreateJobObject (IntPtr.Zero, null);

			// assign the child process to the job
			bool success = AssignProcessToJobObject (job_handle, p.Handle);
			Logger.Log ("JobWindows: assigned process to job object with status: {0}, will now release mutex", success);

			// allow the child process to execute what we really wanted to execute.
			suspended_mutex.ReleaseMutex ();
			Logger.Log ("JobWindows: mutex released");
		}

		public override void Terminate ()
		{
			bool success = TerminateJobObject (job_handle, 1);
			Logger.Log ("JobWindows: terminated job object with status: {0}", success);
			CloseHandle (job_handle);
			job_handle = IntPtr.Zero;
		}

		[DllImport ("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
		public extern static bool CloseHandle (IntPtr handle);

		[DllImport ("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern IntPtr CreateJobObject (IntPtr JobAttributes, [MarshalAs (UnmanagedType.LPTStr)]string strName);

		[DllImport ("kernel32.dll", SetLastError = true)]
		public static extern bool AssignProcessToJobObject (IntPtr hJob, IntPtr hProcess);

		[DllImport ("kernel32.dll", SetLastError = true)]
		public static extern bool TerminateJobObject (IntPtr hJob, uint exitCode);

	}
}
