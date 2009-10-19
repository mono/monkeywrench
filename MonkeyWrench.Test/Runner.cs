/*
 * Runner.cs
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
using System.Reflection;
using System.Text;
using System.Threading;

namespace MonkeyWrench.Test
{
	public class Runner
	{
		public static string TemporaryTestDirectory = Path.Combine (Path.GetTempPath (), "MonkeyWrench.Test");
		public static string SourceDirectory = Path.GetDirectoryName (Path.GetDirectoryName (Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location)));
		public static string MonkeyWrenchXml_FileName = Path.Combine (TemporaryTestDirectory, "MonkeyWrench.xml");
		public static int TEST_DATABASE_PORT = 5678;
		public static string TEST_DB_ROOT_DIR = Path.Combine (Runner.TemporaryTestDirectory, "db");
		public static string TEST_DATA_DIR = Path.Combine (TEST_DB_ROOT_DIR, "data");
		public static string TEST_DATABASE_DIR = Path.Combine (TEST_DATA_DIR, "db");

		public static List<AssertException> Assertions;

		private static Thread XspThread;
		private static ManualResetEvent XspEvent = new ManualResetEvent (false);

		static void StartXsp (string monkeywrench_xml)
		{
			XspEvent.Reset ();

			XspThread = new Thread (delegate ()
			{
				StringBuilder stdout = new StringBuilder ();
				StringBuilder stderr = new StringBuilder ();

				using (Process p = new Process ()) {
					p.StartInfo.FileName = "xsp2";
					p.StartInfo.Arguments = string.Format ("--port 8123 --root {0} --applications /WebServices:{0}/MonkeyWrench.Web.WebService/,/:{0}/MonkeyWrench.Web.UI --nonstop", Runner.SourceDirectory);
					p.StartInfo.EnvironmentVariables ["MONO_OPTIONS"] = "--debug";
					p.StartInfo.EnvironmentVariables ["MONKEYWRENCH_CONFIG_FILE"] = monkeywrench_xml;
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.RedirectStandardError = true;
					p.StartInfo.RedirectStandardOutput = true;
					p.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
					{
						Console.WriteLine ("[XSP STDERR] {0}", e.Data);
						stderr.AppendLine (e.Data);
					};
					p.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
					{
						Console.WriteLine ("[XSP STDOUT] {0}", e.Data);
						stdout.AppendLine (e.Data);
					};
					p.Start ();
					p.BeginErrorReadLine ();
					p.BeginOutputReadLine ();
					XspEvent.WaitOne ();
					p.Kill ();
				}
			});
			XspThread.Start ();
		}

		static void StopXsp ()
		{
			XspEvent.Set ();
		}

		static int Main (string [] args)
		{
			bool result;

			try {
				if (Directory.Exists (TemporaryTestDirectory))
					Directory.Delete (TemporaryTestDirectory, true);
				Directory.CreateDirectory (TemporaryTestDirectory);
				Directory.CreateDirectory (TEST_DB_ROOT_DIR);

				File.WriteAllText (MonkeyWrenchXml_FileName, string.Format (
	@"<MonkeyWrench Version='2'>
<Configuration>		
	<WebServiceUrl>http://localhost:8123/WebServices/</WebServiceUrl>
	<WebServicePassword>hithere</WebServicePassword>
	<SchedulerPassword>hithere</SchedulerPassword>
	<Host>test</Host>
	<DatabasePort>{0}</DatabasePort>
	<DataDirectory>{1}</DataDirectory>
	<DatabaseDirectory>{2}</DatabaseDirectory>
	<LogFile>{3}</LogFile>
</Configuration>
</MonkeyWrench>", TEST_DATABASE_PORT, TEST_DATA_DIR, TEST_DATABASE_DIR, Path.Combine (TemporaryTestDirectory, "MonkeyWrench.Test.log")));
				Environment.SetEnvironmentVariable ("MONKEYWRENCH_CONFIG_FILE", MonkeyWrenchXml_FileName);

				if (!Configuration.LoadConfiguration (args, MonkeyWrenchXml_FileName))
					return 2;

				StartXsp (MonkeyWrenchXml_FileName);
				result = Run ();
				StopXsp ();
				return result ? 0 : 1;
			} catch (Exception ex) {
				Console.WriteLine ("MonkeyWrench.Test: Unexpected exception:");
				Console.WriteLine (ex);
				return 1;
			}
		}

		static bool Run ()
		{
			bool result = true;
			object obj;
			Type TestAttribute = typeof (TestAttribute);
			Type TestSetupAttribute = typeof (TestSetupAttribute);
			Type TestFixtureAttribute = typeof (TestFixtureAttribute);
			Type TestCleanupAttribute = typeof (TestCleanupAttribute);
			MethodInfo Setup = null;
			MethodInfo Cleanup = null;
			MethodInfo [] Methods;
			int tests = 0, failed = 0;
			string filter_to_class = Environment.GetEnvironmentVariable ("MONKEYWRENCH_TEST_CLASS");
			string filter_to_method = Environment.GetEnvironmentVariable ("MONKEYWRENCH_TEST_METHOD");

			Database.Create ();

			Assertions = new List<AssertException> ();

			foreach (Type type in Assembly.GetExecutingAssembly ().GetTypes ()) {
				if (!type.IsDefined (TestAttribute, true))
					continue;

				if (!string.IsNullOrEmpty (filter_to_class) && !string.Equals (type.Name, filter_to_class) && !string.Equals (type.FullName, filter_to_class)) {
					Console.WriteLine ("{0} SKIPPED", type.FullName);
					continue;
				}

				Methods = type.GetMethods ();

				for (int i = 0; i < Methods.Length; i++) {
					bool clear = true;
					if (Methods [i].IsDefined (TestSetupAttribute, true)) {
						Setup = Methods [i];
					} else if (Methods [i].IsDefined (TestCleanupAttribute, true)) {
						Cleanup = Methods [i];
					} else if (Methods [i].IsDefined (TestFixtureAttribute, true)) {
						clear = false;
					}
					if (clear) {
						Methods [i] = null;
					}
				}

				Environment.CurrentDirectory = TemporaryTestDirectory;

				Console.WriteLine ("{0}...", type.FullName);

				try {
					obj = Activator.CreateInstance (type);
				} catch (Exception ex) {
					while (ex is TargetInvocationException)
						ex = ex.InnerException;
					Console.WriteLine ("FAIL: Exception during object construction: {0}", ex);
					result = false;
					continue;
				}

				try {
					if (Setup != null) {
						Setup.Invoke (obj, null);
					}
				} catch (Exception ex) {
					while (ex is TargetInvocationException)
						ex = ex.InnerException;
					Console.WriteLine ("FAIL: Exception during test initialization: {0}", ex);
					result = false;
					continue;
				}

				for (int i = 0; i < Methods.Length; i++) {
					MethodInfo Test = Methods [i];

					if (Test == null)
						continue;

					Test = Methods [i];

					try {
						Console.WriteLine (" {0}.{1}", type.FullName, Test.Name);
						Assertions.Clear ();
						tests++;
						Test.Invoke (obj, null);
					} catch (Exception ex) {
						while (ex is TargetInvocationException)
							ex = ex.InnerException;
						if (ex is AssertException) {
							Assertions.Add ((AssertException) ex);
						} else {
							Assertions.Add (new AssertException ("Unexpected exception: {0}", ex));
						}
						result = false;
					}

					if (Assertions.Count > 0) {
						failed++;
						Console.ForegroundColor = ConsoleColor.Red;
						foreach (AssertException ex in Assertions) {
							Console.WriteLine ("  FAIL: {0}", ex.Message);
						}
						result = false;
					} else {
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine ("  PASS");
					}
					Console.ResetColor ();
				}

				try {
					IDisposable idisposable = obj as IDisposable;

					if (idisposable != null) {
						idisposable.Dispose ();
					}

					if (Cleanup != null) {
						Cleanup.Invoke (obj, null);
					}
				} catch (Exception ex) {
					while (ex is TargetInvocationException)
						ex = ex.InnerException;
					Console.WriteLine ("FAIL: Exception during test cleanup: {0}", ex);
					result = false;
					continue;
				}
			}

			Database.Stop ();

			Console.WriteLine ("Executed {0} tests, {1} passed and {2} failed.", tests, tests - failed, failed);

			return result;
		}
	}
}
