/*
 * Builder.cs
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
using System.Xml;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Text;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

namespace MonkeyWrench.Builder
{
	public class Builder
	{
		private static WebServices WebService;
		private static GetBuildInfoResponse response;
		private static List<DBHostLane> failed_hostlanes = new List<DBHostLane> ();

		public static void Main (string [] arguments)
		{
			ProcessHelper.Exit (Main2 (arguments)); // Work around #499702
		}

		private static int Main2 (string [] arguments)
		{
			Lock process_lock;

			try {
				if (!Configuration.LoadConfiguration (arguments))
					return 1;

				if (!Configuration.VerifyBuildBotConfiguration ())
					return 1;

				process_lock = Lock.Create ("MonkeyWrench.Builder");
				if (process_lock == null) {
					Logger.Log ("Builder could not acquire lock. Exiting");
					return 1;
				}
				Logger.Log ("Builder lock aquired successfully.");
			} catch (Exception ex) {
				Logger.Log ("Could not aquire lock: {0}", ex.Message);
				return 1;
			}

			try {
				WebService = WebServices.Create ();
				WebService.CreateLogin (Configuration.Host, Configuration.WebServicePassword);

				response = WebService.GetBuildInfoMultiple (WebService.WebServiceLogin, Configuration.Host, true);

				if (!response.Host.enabled) {
					Logger.Log ("This host is disabled. Exiting.");
					return 0;
				}

				Logger.Log ("Builder will now build {0} lists of work items.", response.Work.Count);

				for (int i = 0; i < response.Work.Count; i++) {
					//foreach (var item in response.Work)
					Logger.Log ("Building list #{0}/{1}", i+ 1, response.Work.Count);
					Build (response.Work [i]);//item);
				}

				Logger.Log ("Builder finished successfully.");

				return 0;
			} catch (Exception ex) {
				Logger.Log ("An exception occurred: {0}", ex.ToString ());
				return 1;
			} finally {
				process_lock.Unlock ();
			}
		}

		public static string Dos2Unix (string contents)
		{
			return contents.Replace ("\r\n", "\n");
		}

		// This is the entry point for the worker threads
		private static void Build (object input)
		{
			BuildInfo info = null;

			try {
				info = (BuildInfo) input;
				Build (info);
			} catch (Exception ex) {
				// Just swallow all exceptions here.
				if (info == null) {
					Logger.Log ("Exception while building lane: {0}", ex.ToString ());
				} else {
					Logger.Log ("{2} Exception while building lane '{0}': {1}", info.lane.lane, ex.ToString (), info.number);
				}
			}
		}
		private static void Build (BuildInfo info)
		{
			try {
				object sync_object = new object ();
				string log_file = Path.Combine (info.BUILDER_DATA_LOG_DIR, info.command.command + ".log");
				DateTime last_stamp = DateTime.Now;
				DateTime local_starttime = DateTime.Now;
				DBState result;
				DBCommand command = info.command;
				int exitcode = 0;
				ReportBuildStateResponse response;

				Logger.Log ("{0} Builder started new thread for sequence {1} step {2}", info.number, info.command.sequence, info.command.command);

				result = DBState.Executing;

				info.work.starttime = DBRecord.DatabaseNow;
				info.work.State = result;
				info.work.host_id = info.host.id;
				info.work = WebService.ReportBuildStateSafe (info.work).Work;

				using (Process p = new Process ()) {
					using (FileStream fs = new FileStream (log_file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
						using (StreamWriter log = new StreamWriter (fs)) {
							p.StartInfo.FileName = info.command.filename;
							p.StartInfo.Arguments = string.Format (info.command.arguments, Path.Combine (info.temp_dir, info.command.command));
							p.StartInfo.WorkingDirectory = info.BUILDER_DATA_SOURCE_DIR;
							p.StartInfo.UseShellExecute = false;
							p.StartInfo.RedirectStandardError = true;
							p.StartInfo.RedirectStandardOutput = true;

							// set environment variables
							p.StartInfo.EnvironmentVariables ["BUILD_LANE"] = info.lane.lane;
							p.StartInfo.EnvironmentVariables ["BUILD_COMMAND"] = info.command.command;
							p.StartInfo.EnvironmentVariables ["BUILD_REVISION"] = info.revision.revision;
							p.StartInfo.EnvironmentVariables ["BUILD_INSTALL"] = Configuration.CygwinizePath (info.BUILDER_DATA_INSTALL_DIR);
							p.StartInfo.EnvironmentVariables ["BUILD_DATA_LANE"] = Configuration.GetDataLane (info.lane.lane);
							p.StartInfo.EnvironmentVariables ["BUILD_REPOSITORY"] = info.lane.repository;
							p.StartInfo.EnvironmentVariables ["BUILD_HOST"] = Configuration.Host;

							int r = 0;
							foreach (string repo in info.lane.repository.Split (',')) {
								p.StartInfo.EnvironmentVariables ["BUILD_REPOSITORY_" + r.ToString ()] = repo;
								r++;
							}
							p.StartInfo.EnvironmentVariables ["BUILD_REPOSITORY_SPACE"] = info.lane.repository.Replace (',', ' ');
							p.StartInfo.EnvironmentVariables ["BUILD_SEQUENCE"] = "0";
							p.StartInfo.EnvironmentVariables ["BUILD_SCRIPT_DIR"] = info.temp_dir;
							p.StartInfo.EnvironmentVariables ["LD_LIBRARY_PATH"] = Configuration.CygwinizePath (Configuration.GetLdLibraryPath (info.lane.lane, info.revision.revision));
							p.StartInfo.EnvironmentVariables ["PKG_CONFIG_PATH"] = Configuration.CygwinizePath (Configuration.GetPkgConfigPath (info.lane.lane, info.revision.revision));
							p.StartInfo.EnvironmentVariables ["PATH"] = Configuration.CygwinizePath (Configuration.GetPath (info.lane.lane, info.revision.revision));
							p.StartInfo.EnvironmentVariables ["C_INCLUDE_PATH"] = Configuration.CygwinizePath (Configuration.GetCIncludePath (info.lane.lane, info.revision.revision));
							p.StartInfo.EnvironmentVariables ["CPLUS_INCLUDE_PATH"] = Configuration.CygwinizePath (Configuration.GetCPlusIncludePath (info.lane.lane, info.revision.revision));

							// We need to remove all paths from environment variables that were
							// set for this executable to work so that they don't mess with 
							// whatever we're trying to execute
							string [] bot_dependencies = new string [] { "PATH", "LD_LIBRARY_PATH", "PKG_CONFIG_PATH", "C_INCLUDE_PATH", "CPLUS_INCLUDE_PATH", "AC_LOCAL_PATH", "MONO_PATH" };
							foreach (string bot_dependency in bot_dependencies) {
								if (!p.StartInfo.EnvironmentVariables.ContainsKey (bot_dependency))
									continue;
								List<string> paths = new List<string> (p.StartInfo.EnvironmentVariables [bot_dependency].Split (new char [] { ':' /* XXX: does windows use ';' here? */}, StringSplitOptions.None));
								for (int i = paths.Count - 1; i >= 0; i--) {
									if (paths [i].Contains ("bot-dependencies"))
										paths.RemoveAt (i);
								}
								p.StartInfo.EnvironmentVariables [bot_dependency] = string.Join (":", paths.ToArray ());
							}

							if (info.environment_variables != null) {
								// order is important here, we need to loop over the array in the same order get got the variables.
								for (int e = 0; e < info.environment_variables.Count; e++) {
									info.environment_variables [e].Evaluate (p.StartInfo.EnvironmentVariables);
								}
							}

							Thread stdout_thread = new Thread (delegate ()
							{
								try {
									string line;
									while (null != (line = p.StandardOutput.ReadLine ())) {
										lock (sync_object) {
											log.WriteLine (line);
											log.Flush ();
										}
										last_stamp = DateTime.Now;
									}
								} catch (Exception ex) {
									Logger.Log ("{1} Stdin reader thread got exception: {0}", ex.Message, info.number);
								}
							});

							Thread stderr_thread = new Thread (delegate ()
							{
								try {
									string line;
									while (null != (line = p.StandardError.ReadLine ())) {
										lock (sync_object) {
											log.WriteLine (line);
											log.Flush ();
										}
										last_stamp = DateTime.Now;
									}
								} catch (Exception ex) {
									Logger.Log ("{1} Stderr reader thread got exception: {0}", ex.Message, info.number);
								}
							});

							p.Start ();

							stderr_thread.Start ();
							stdout_thread.Start ();

							while (!p.WaitForExit (1000 * info.command.timeout)) {
								if (p.HasExited)
									break;

								// Check if step has been aborted.
								info.work.State = WebService.GetWorkStateSafe (info.work);
								if (info.work.State == DBState.Aborted) {
									result = DBState.Aborted;
									try {
										exitcode = 255;
										Logger.Log ("{1} The build step '{0}' has been aborted, killing it.", info.command.command, info.number);
										p.KillTree ();
										log.WriteLine ("{1} The build step '{0}' was aborted, killed it.", info.command.command, info.number);
									} catch (Exception ex) {
										Logger.Log ("{1} Exception while killing build step: {0}", ex.ToString (), info.number);
									}
									break;
								}

								// Check if step has timedout
								bool timedout = false;
								string timeoutReason = null;
								int timeout = 15;

								if ((DateTime.Now > local_starttime.AddMinutes (info.command.timeout))) {
									timedout = true;
									timeoutReason = string.Format ("The build step '{0}' didn't finish in {1} minute(s).", info.command.command, info.command.timeout);
								} else if (last_stamp.AddMinutes (timeout) <= DateTime.Now) {
									timedout = true;
									timeoutReason = string.Format ("The build step '{0}' has had no output for {1} minute(s).", info.command.command, timeout);
								}

								if (!timedout)
									continue;

								try {
									result = DBState.Timeout;
									exitcode = 255;
									Logger.Log ("{0} {1}", info.number, timeoutReason);
									p.KillTree ();
									log.WriteLine (timeoutReason);
								} catch (Exception ex) {
									Logger.Log ("{1} Exception while killing build step: {0}", ex.ToString (), info.number);
								}
								break;
							}

							// Sleep a bit so that the process has enough time to finish
							System.Threading.Thread.Sleep (1000);

							if (p.HasExited) {
								exitcode = p.ExitCode;
							} else {
								Logger.Log ("{1} Step: {0}: the process didn't exit in time.", command.command, info.number);
								exitcode = 1;
							}
							if (result == DBState.Executing) {
								if (exitcode == 0) {
									result = DBState.Success;
								} else {
									result = DBState.Failed;
								}
							} else if (result == DBState.Aborted) {
								result = DBState.Failed;
							}
						}
					}
				}

				info.work.State = result;
				using (TextReader reader = new StreamReader (new FileStream (log_file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
					info.work.CalculateSummary (reader);
				info.work.endtime = DBRecord.DatabaseNow;
				response = WebService.ReportBuildStateSafe (info.work);
				info.work = response.Work;

				WebService.UploadFileSafe (info.work, log_file, false);

				CheckLog (log_file, info.work);

				if (response.RevisionWorkCompleted) {
					// Cleanup after us.
					string base_dir = Configuration.GetDataRevisionDir (info.lane.lane, info.revision.revision);
					try {
						Directory.Delete (base_dir, true);
						Logger.Log ("Successfully deleted directory {0}", base_dir);
					} catch (Exception ex) {
						Logger.Log ("Couldn't delete directory {0}: {1}\n{2}", base_dir, ex.Message, ex.StackTrace);
					}
				}

				Logger.Log ("{4} Revision {0}, executed step '{1}' in {2} s, ExitCode: {3}, State: {4}", info.revision.revision, info.command.command, info.work.duration, exitcode, info.number, info.work.State);
			} catch (Exception ex) {
				info.work.State = DBState.Failed;
				info.work.summary = ex.Message;
				info.work = WebService.ReportBuildStateSafe (info.work).Work;
				Logger.Log ("{3} Revision {0}, got exception '{1}': \n{2}", info.revision.revision, ex.Message, ex.StackTrace, info.number);
				throw;
			} finally {
				Logger.Log ("{0} Builder finished thread for sequence {0}", info.number);

				if (info.work.State == DBState.Failed && !info.command.nonfatal) {
					failed_hostlanes.Add (info.hostlane);
				}
			}
		}

		private static void Build (List<BuildInfoEntry> list)
		{
			string temp_dir = null;
			List<Thread> threads = new List<Thread> ();
			List<BuildInfo> infos = new List<BuildInfo> ();

			Logger.Log ("Building {0} work items", list.Count);

			try {
				// Get the path of the temporary directory for this process
				using (Process p = Process.GetCurrentProcess ()) {
					temp_dir = Path.Combine (Path.Combine (Path.GetTempPath (), Configuration.ApplicationName), p.Id.ToString ());
					if (Directory.Exists (temp_dir)) {
						// This should happen very rarely
						Directory.Delete (temp_dir, true);
					}
					Directory.CreateDirectory (temp_dir);
				}

				for (int i = 0; i < list.Count; i++) {
					BuildInfoEntry entry = list [i];
					BuildInfo info;

					if (failed_hostlanes != null) {
						foreach (DBHostLane failed in failed_hostlanes) {
							if (failed.id == entry.HostLane.id) {
								Logger.Log ("Skipping work, the hostlane {0} has failed work in this run, which has disabled any further work (in this run).", failed.id);
								entry = null;
								break;
							}
						}
					}

					if (entry == null)
						continue;

					info = new BuildInfo ();

					infos.Add (info);

					info.lane = entry.Lane;
					info.revision = entry.Revision;

					// download dependent files
					if (entry.FilesToDownload != null) {
						for (int f = 0; f < entry.FilesToDownload.Count; f++) {
							DBWorkFile file = entry.FilesToDownload [f];
							DBLane dependent_lane = entry.DependentLaneOfFiles [f];
							WebService.DownloadFileSafe (file, Configuration.GetDependentDownloadDirectory (info.lane.lane, dependent_lane.lane, info.revision.revision));
						}
					}

					// Set revision-specific paths
					info.BUILDER_DATA_INSTALL_DIR = Configuration.GetDataInstallDir (entry.Lane.lane, entry.Revision.revision);
					info.BUILDER_DATA_LOG_DIR = Configuration.GetDataLogDir (entry.Lane.lane, entry.Revision.revision);
					info.BUILDER_DATA_SOURCE_DIR = Configuration.GetDataSourceDir (entry.Lane.lane, entry.Revision.revision);
					info.environment_variables = entry.EnvironmentVariables;

					if (!Directory.Exists (info.BUILDER_DATA_SOURCE_DIR))
						Directory.CreateDirectory (info.BUILDER_DATA_SOURCE_DIR);
					if (!Directory.Exists (info.BUILDER_DATA_LOG_DIR))
						Directory.CreateDirectory (info.BUILDER_DATA_LOG_DIR);

					info.temp_dir = Path.Combine (temp_dir, i.ToString ());
					Directory.CreateDirectory (info.temp_dir); // this directory should not exist.

					// Write all the files to the temporary directory
					foreach (DBLanefile file in entry.LaneFiles)
						File.WriteAllText (Path.Combine (info.temp_dir, file.name), Dos2Unix (file.contents));

					info.command = entry.Command;
					info.host = response.Host;
					info.work = entry.Work;
					info.lane = entry.Lane;
					info.hostlane = entry.HostLane;
					info.number = i;
					info.revision = entry.Revision;
				}

				threads.Clear ();

				// Start worker threads.
				for (int i = 0; i < infos.Count; i++) {
					threads.Add (new Thread (Build));
					threads [i].Start (infos [i]);
				}

				// Wait until all threads have stopped.
				// Don't try to abort the threads after a certain time has passed
				// when threads are aborted they leave things in a pretty messed-up state,
				// and that pain is worse than the one caused if something hangs.
				for (int i = 0; i < threads.Count; i++)
					threads [i].Join ();

				Logger.Log ("Finished building {0} work items", list.Count);

			} catch (Exception ex) {
				Logger.Log ("Exception while building lane: {0}", ex);
			} finally {
				// clean up after us
				if (temp_dir != null && Directory.Exists (temp_dir))
					Directory.Delete (temp_dir, true);
			}
		}

		public static void CheckLog (string log_file, DBWork work)
		{
			string line, l;
			string cmd;
			int index;

			using (FileStream fs = new FileStream (log_file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				using (StreamReader reader = new StreamReader (fs)) {
					while (null != (l = reader.ReadLine ())) {
						line = l;
						if (line.StartsWith ("@Moonbuilder:")) {
							line = line.Substring ("@Moonbuilder:".Length);
						} else if (line.StartsWith ("@MonkeyWrench:")) {
							line = line.Substring ("@MonkeyWrench:".Length);
						} else {
							continue;
						}

						line = line.TrimStart (' ');

						index = line.IndexOf (':');
						if (index == -1)
							continue;

						cmd = line.Substring (0, index);
						line = line.Substring (index + 1);
						while (line.StartsWith (" "))
							line = line.Substring (1);

						switch (cmd) {
						case "AddFile":
						case "AddHiddenFile":
							try {
								Logger.Log ("@MonkeyWrench command: '{0}' args: '{1}'", cmd, line);
								string filename = line.Trim ();
								try {
									WebService.UploadFileSafe (work, filename, cmd.Contains ("Hidden"));
								} catch (Exception ex) {
									Logger.Log ("Error while uploading file {0}: '{1}'. Skipping upload of this file", filename, ex.Message);
								}
							} catch (Exception e) {
								Logger.Log ("Error while executing @MonkeyWrench command '{0}': '{1}'", cmd, e.Message);
							}
							break;
						case "AddDirectory":
						case "AddHiddenDirectory":
							try {
								Logger.Log ("@MonkeyWrench command: '{0}' args: '{1}'", cmd, line);
								foreach (string file in Directory.GetFiles (line.Trim ())) {
									try {
										WebService.UploadFileSafe (work, file, cmd.Contains ("Hidden"));
									} catch (Exception ex) {
										Logger.Log ("Error while uploading file {0}: '{1}'. Skipping upload of this file", file, ex.Message);
									}
								}
							} catch (Exception e) {
								Logger.Log ("Error while executing @MonkeyWrench command '{0}': '{1}'", cmd, e.Message);
							}
							break;
						default:
							Logger.Log ("Invalid @MonkeyWrench command: '{0}', entire line: '{1}'", cmd, l);
							break;
						}
					}
				}
			}
		}
	}
}

