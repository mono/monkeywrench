using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Test
{
	public abstract class TestBase : IDisposable
	{
		protected DB db;

		protected TestBase ()
		{
			db = new DB ();
			Database.Clean (db);
		}

		~TestBase ()
		{
			Dispose ();
		}

		public void Dispose ()
		{
			if (db != null) {
				//CleanDatabase ();
				db.Dispose ();
				db = null;
			}
			GC.SuppressFinalize (this);
		}

		public void CheckFileContents (int? file_id, string message, string contents, bool trim)
		{
			if (file_id.HasValue) {
				using (Stream s = db.Download (DBFile_Extensions.Create (db, file_id.Value))) {
					using (StreamReader reader = new StreamReader (s)) {
						string file = reader.ReadToEnd ();
						if (trim) {
							contents = contents.Trim ();
							file = file.Trim ();
						}
						Check.AreEqual (contents, file, message);
					}
				}
			} else {
				Check.AreEqual (null, contents, message);
			}
		}


		public static void Execute (string workingdir, string process, TimeSpan timeout, StringBuilder stdout, StringBuilder stderr, string arguments)
		{
			Console.WriteLine ("Executing: '{0}' '{1}' in '{2}", process, arguments, workingdir);

			using (Process p = new Process ()) {
				p.StartInfo.WorkingDirectory = workingdir;
				p.StartInfo.FileName = process;
				p.StartInfo.Arguments = arguments;
				if (stdout != null) {
					p.StartInfo.RedirectStandardOutput = true;
					p.StartInfo.UseShellExecute = false;
					p.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
					{
						stdout.AppendLine (e.Data);
					};
				}
				if (stderr != null) {
					p.StartInfo.RedirectStandardError = true;
					p.StartInfo.UseShellExecute = false;
					p.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
					{
						stderr.AppendLine (e.Data);
					};
				}
				p.Start ();
				if (stdout != null) {
					p.BeginOutputReadLine ();
				}
				if (stderr != null) {
					p.BeginErrorReadLine ();
				}

				if (!p.WaitForExit ((int) timeout.TotalMilliseconds)) {
					p.Kill ();
					throw new AssertException ("Process {0} didn't finish in {1} ms", process, (int) timeout.TotalMilliseconds);
				}
			}
		}

		public static void Execute (string workingdir, string process, params string [] arguments)
		{
			StringBuilder stdout = new StringBuilder ();
			StringBuilder stderr = new StringBuilder ();
			Execute (workingdir, process, TimeSpan.FromMinutes (1), stdout, stderr, arguments);
			if (stderr.ToString ().Trim () != string.Empty)
				Console.Error.WriteLine (stderr.ToString ().Trim ());
		}

		public static void Execute (string workingdir, string process, TimeSpan timeout, StringBuilder stdout, StringBuilder stderr, params string [] arguments)
		{
			StringBuilder builder = new StringBuilder ();
			for (int i = 0; i < arguments.Length; i++) {
				if (i > 0)
					builder.Append (' ');
				if (arguments [i].IndexOf (' ') >= 0) {
					builder.Append ('"');
					builder.Append (arguments [i]);
					builder.Append ('"');
				} else {
					builder.Append (arguments [i]);
				}
			}
			Execute (workingdir, process, timeout, stdout, stderr, builder.ToString ());
		}
	}
}
