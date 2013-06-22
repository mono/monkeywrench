using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace MonkeyWrench
{
	public class ProcessReader {
		SynchronizedStreamWriter log;
		Thread stdout_thread;
		Thread stderr_thread;

		public ProcessReader (SynchronizedStreamWriter output)
		{
			this.log = output;
		}

		public void Setup (Job p)
		{
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardError = true;
			p.StartInfo.RedirectStandardOutput = true;

			stdout_thread = new Thread (delegate () {
				try {
					string line;
					while (null != (line = p.StandardOutput.ReadLine ())) {
						log.WriteLine (line);
					}
				} catch (Exception ex) {
					Logger.Log ("? Stdin reader thread got exception: {0}", ex.Message);
				}
			});

			stderr_thread = new Thread (delegate () {
				try {
					string line;
					while (null != (line = p.StandardError.ReadLine ())) {
						log.WriteLine (line);
					}
				} catch (Exception ex) {
					Logger.Log ("? Stderr reader thread got exception: {0}", ex.Message);
				}
			});
		}

		public void Start ()
		{
			stderr_thread.Start ();
			stdout_thread.Start ();
		}

		public void Join ()
		{
			if (!stdout_thread.Join (TimeSpan.FromSeconds (15))) {
				Logger.Log ("Waited 15s for stdout thread to finish, but it didn't");
			}
			if (!stderr_thread.Join (TimeSpan.FromSeconds (15))) {
				Logger.Log ("Waited 15s for stderr thread to finish, but it didn't");
			}
		}
	}

}

