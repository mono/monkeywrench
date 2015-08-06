using System;
using System.Threading;
using log4net;

namespace MonkeyWrench
{
	public class ProcessReader {
		static readonly ILog log = LogManager.GetLogger (typeof (ProcessReader));

		SynchronizedStreamWriter logstream;
		Thread stdout_thread;
		Thread stderr_thread;

		public ProcessReader (SynchronizedStreamWriter output)
		{
			this.logstream = output;
		}

		public void Setup (Job p, bool timestamp=false)
		{
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardError = true;
			p.StartInfo.RedirectStandardOutput = true;

			stdout_thread = new Thread (delegate () {
				try {
					string line;
					while (null != (line = p.StandardOutput.ReadLine ())) {
						if (timestamp && !line.StartsWith("@MonkeyWrench:") && !line.StartsWith("@Moonbuilder:")) { line = "[" + DateTime.Now.ToString("h:mm:ss") + "] " + line; }
						logstream.WriteLine (line);
					}
				} catch (Exception ex) {
					log.ErrorFormat ("Stdin reader thread got exception: {0}", ex);
				}
			});

			stderr_thread = new Thread (delegate () {
				try {
					string line;
					while (null != (line = p.StandardError.ReadLine ())) {
						logstream.WriteLine (line);
					}
				} catch (Exception ex) {
					log.ErrorFormat ("Stderr reader thread got exception: {0}", ex);
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
				log.Error ("Waited 15s for stdout thread to finish, but it didn't");
			}
			if (!stderr_thread.Join (TimeSpan.FromSeconds (15))) {
				log.Error ("Waited 15s for stderr thread to finish, but it didn't");
			}
		}
	}

}

