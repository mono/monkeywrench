/*
 * Job.cs
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
using System.IO;
using System.Diagnostics;
using System.Text;

namespace MonkeyWrench
{
	/*
	 * This class is a wrapper around a process with one difference: it's able to kill itself and all its descendants (like the win32 job object).
	 */
	public class Job : IDisposable
	{
		protected Process p = new Process ();

		public ProcessStartInfo StartInfo
		{
			get { return p.StartInfo; }
		}

		public int ExitCode
		{
			get { return p.ExitCode; }
		}

		public bool HasExited
		{
			get { return p.HasExited; }
		}

		/// <summary>
		/// Terminate this job and all the processes within.
		/// </summary>
		public virtual void Terminate ()
		{
			p.KillTree ();
		}

		/// <summary>
		/// Start this job.
		/// </summary>
		public virtual void Start ()
		{
			p.Start ();
		}

		public bool WaitForExit (int milliseconds)
		{
			return p.WaitForExit (milliseconds);
		}

		public StreamReader StandardError
		{
			get { return p.StandardError; }
		}

		public StreamReader StandardOutput
		{
			get { return p.StandardOutput; }
		}

		public StreamWriter StandardInput
		{
			get { return p.StandardInput; }
		}


		#region IDisposable Members
		public virtual void Dispose ()
		{
			p.Dispose ();
		}

		#endregion
	}
}
