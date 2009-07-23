using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace MonkeyWrench
{
	public class Lock
	{
		Mutex mutex;
		Semaphore semaphore;
		FileStream file;
		string file_existence;

		private Lock ()
		{
		}

		/// <summary>
		/// Try to aquire a machine-wide lock named 'name'. Returns null in case of failure (it doesn't wait at all).
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static Lock Create (string name)
		{
			Lock result = new Lock ();
			Mutex mutex;
			Semaphore semaphore;

			switch (Configuration.LockingAlgorithm.ToLowerInvariant ()) {
			case "mutex":
				mutex = new Mutex (true, name);
				if (mutex.WaitOne (1 /* ms */)) {
					result.mutex = mutex;
					return result;
				}
				return null;
			case "file":
				try {
					result.file = File.Open (Path.Combine (Path.GetTempPath (), name + ".lock"), FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
					return result;
				} catch (IOException ex) {
					Logger.Log ("Could not aquire builder lock: {0}", ex.Message);
					return null;
				}
			case "fileexistence":
			case "fileexistance":
				string tmp = Path.Combine (Path.GetTempPath (), name + ".fileexistence-lock--delete-to-unlock");
				Logger.Log ("Checking file existence for {0}", tmp);
				if (File.Exists (tmp))
					return null;
				// there is a race condition here.
				// given that the default setup is to execute a program at most once per minute,
				// the race condition is harmless.
				File.WriteAllText (tmp, Process.GetCurrentProcess ().Id.ToString ());
				result.file_existence = tmp;
				return result;
			case "semaphore":
				semaphore = new Semaphore (1, 1, name);
				if (semaphore.WaitOne (1 /* ms */)) {
					result.semaphore = semaphore;
					return result;
				}
				return null;
			default:
				Logger.Log ("Unknown locking algorithm: {0} (using default 'semaphore')", Configuration.LockingAlgorithm);
				goto case "semaphore";
			}
		}

		public void Unlock ()
		{
			try {
				if (mutex != null) {
					mutex.ReleaseMutex ();
				} else if (semaphore != null) {
					semaphore.Release ();
				} else if (file != null) {
					file.Close ();
				} else if (file_existence != null) {
					File.Delete (file_existence);
				}

			} catch (Exception ex) {
				Logger.Log ("Exception while unlocking process lock (file existence): {0}", ex.Message);
			}

			GC.SuppressFinalize (this);
		}

		~Lock ()
		{
			Unlock ();
		}
	}
}
