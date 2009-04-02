/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
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

using Builder;

namespace Builder
{
	internal class BuildInfo
	{
		public int number;
		public DBLane lane;
		public DBHost host;
		public DBHost master_host;
		public DBWork work;
		public DBCommand command;
		public DBRevision revision;
		public string BUILDER_DATA_LOG_DIR;
		public string BUILDER_DATA_INSTALL_DIR;
		public string BUILDER_DATA_SOURCE_DIR;
		public string temp_dir;
	}

	public class Builder
	{
		public static int Main (string [] args)
		{
			List<DBLane> lanes;
			DBHost master_host;
			DBHost host;

			try {
				Logger.LogFile = Configuration.DEFAULT_BUILDER_LOG;

				Configuration.InitializeApp (args, "Builder");

				using (DB db = new DB (true)) {
					master_host = db.LookupHost (Configuration.MasterHost);
					if (Configuration.MasterHost != Configuration.Host)
						host = db.LookupHost (Configuration.Host);
					else
						host = master_host;

					lanes = db.GetLanesForHost (master_host.id, true);

					Logger.Log ("Builder will now build {0} lanes.", lanes.Count);

					foreach (DBLane lane in lanes) {
						Build (db, lane, host, master_host);
					}
				}
				Logger.Log ("Builder finished successfully.");

				return 0;
			} catch (Exception ex) {
				Logger.Log ("An exception occurred: {0}", ex.ToString ());
				return 1;
			}
		}

		public static string Dos2Unix (string contents)
		{
			return contents.Replace ("\r\n", "\n");
		}

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
			using (DB db = new DB (true)) {
				try {
					object sync_object = new object ();
					string log_file = Path.Combine (info.BUILDER_DATA_LOG_DIR, info.command.command + ".log");
					DateTime last_stamp = DateTime.Now;
					DateTime local_starttime = DateTime.Now;
					DBState result;
					DBWork work = info.work;
					DBLane lane = info.lane;
					DBCommand command = info.command;
					int exitcode = 0;
					
					Logger.Log ("{0} Builder started new thread for sequence {1} step {2}", info.number, info.command.sequence, info.command.command);

					result = DBState.Executing;

					work.Reload (db.Connection);
					work.starttime = DBRecord.DatabaseNow;
					work.State = result;
					work.host_id = info.host.id;
					work.Save (db.Connection);
					work.UpdateRevisionWorkState (db);

					using (Process p = new Process ()) {
						using (FileStream fs = new FileStream (log_file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
							using (StreamWriter log = new StreamWriter (fs)) {
								p.StartInfo.FileName = info.command.filename;
								p.StartInfo.Arguments = string.Format (info.command.arguments, Path.Combine (info.temp_dir, info.command.command));
								p.StartInfo.WorkingDirectory = info.BUILDER_DATA_SOURCE_DIR;
								p.StartInfo.UseShellExecute = false;
								p.StartInfo.RedirectStandardError = true;
								p.StartInfo.RedirectStandardOutput = true;

								p.StartInfo.EnvironmentVariables ["BUILD_LANE"] = info.lane.lane;
								p.StartInfo.EnvironmentVariables ["BUILD_COMMAND"] = info.command.command;
								p.StartInfo.EnvironmentVariables ["BUILD_REVISION"] = info.revision.revision;
								p.StartInfo.EnvironmentVariables ["BUILD_INSTALL"] = info.BUILDER_DATA_INSTALL_DIR;
								p.StartInfo.EnvironmentVariables ["BUILD_DATA_LANE"] = Configuration.GetDataLane (info.lane.lane);
								p.StartInfo.EnvironmentVariables ["BUILD_REPOSITORY"] = info.lane.repository;
								p.StartInfo.EnvironmentVariables ["BUILD_SEQUENCE"] = "0";
								p.StartInfo.EnvironmentVariables ["LD_LIBRARY_PATH"] = Configuration.GetLdLibraryPath (info.lane.lane, info.revision.revision);
								p.StartInfo.EnvironmentVariables ["PKG_CONFIG_PATH"] = Configuration.GetPkgConfigPath (info.lane.lane, info.revision.revision);
								p.StartInfo.EnvironmentVariables ["PATH"] = Configuration.GetPath (info.lane.lane, info.revision.revision);

								// Obsolete:
								p.StartInfo.EnvironmentVariables.Add ("SVN_REPOSITORY", info.lane.repository);
								p.StartInfo.EnvironmentVariables.Add ("BUILD_STEP", info.command.command.Replace (".sh", ""));
								p.StartInfo.EnvironmentVariables.Add ("BUILDER_DATA_LANE", Configuration.GetDataLane (lane.lane));

								// We need to remove all paths from environment variables that were
								// set for this executable to work so that they don't mess with 
								// whatever we're trying to execute
								string [] bot_dependencies = new string [] { "PATH", "LD_LIBRARY_PATH", "PKG_CONFIG_PATH", "C_INCLUDE_PATH", "CPULS_INCLUDE_PATH", "AC_LOCAL_PATH" , "MONO_PATH"};
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

									//// Check if finished every 3 seconds.
									//Thread.Sleep(3 * 1000);

									//if (p.HasExited)
									//    break;

									//// 
									//if (last_abort_check.AddSeconds(60) < DateTime.Now)
									//    continue;
									//last_abort_check = DateTime.Now;

									// Check if step has been aborted.
									work.Reload (db.Connection);
									if (work.State == DBState.Aborted) {
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
									int timeout = 10;

									if ((DateTime.Now > local_starttime.AddMinutes (info.command.timeout))) {
										timedout = true;
										timeoutReason = string.Format ("The build step '{0}' didn't finish in {1} minutes.", info.command.command, timeout);
									} else if (last_stamp.AddMinutes (timeout) <= DateTime.Now) {
										timedout = true;
										timeoutReason = string.Format ("The build step '{0}' has had no output for {1} minutes.", info.command.command, timeout);
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

					work.Reload (db.Connection);
					work.State = result;
					using (TextReader reader = new StreamReader (new FileStream (log_file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
						work.CalculateSummary (reader);
					work.endtime = DBRecord.DatabaseNow;
					work.Save (db.Connection);

					work.AddFile (db, log_file, false);

					CheckLog (log_file, db, work);

					Logger.Log ("{4} Revision {0}, executed step '{1}' in {2} s, ExitCode: {3}, State: {4}", info.revision.revision, info.command.command, work.duration, exitcode, info.number, work.State);
				} catch (Exception ex) {
					info.work.State = DBState.Failed;
					info.work.summary = ex.Message;
					info.work.Save (db.Connection);
					Logger.Log ("{3} Revision {0}, got exception '{1}': \n{2}", info.revision.revision, ex.Message, ex.StackTrace, info.number);
					throw;
				} finally {
					Logger.Log ("{0} Builder finished thread for sequence {0}", info.number);
				}
			}
		}

		private static void Build (DB db, DBLane lane, DBHost host, DBHost master_host)
		{
			string BUILDER_DATA_LOG_DIR;
			string BUILDER_DATA_INSTALL_DIR;
			string BUILDER_DATA_SOURCE_DIR;

			DBHostLane hostlane;
			FileStream file_lock = null;
			bool aborted = false;
			DBRevision revision;
			DBRevisionWork revisionwork = null;
			string temp_dir = null;
			List<Thread> threads = new List<Thread> ();
			List<BuildInfo> infos = new List<BuildInfo> ();
			List<DBWorkView2> pending_work = null;

			Logger.Log ("Building {0} for workhost {1} and masterhost {2}", lane.lane, host.host, master_host.host);

			try {
				hostlane = db.GetHostLane (master_host.id, lane.id);

				if (hostlane == null) {
					Logger.Log ("The lane {0} is not configured for the master host {1} (host: {2})", lane.lane, master_host.host, host.host);
					return;
				} else if (!hostlane.enabled) {
					Logger.Log ("The lane {0} is disabled on the master host {1} (host: {2})", lane.lane, master_host.host, host.host);
					return;
				}

				try {
					string lock_file = string.Format (Path.Combine (Path.GetTempPath (), "Builder.Builder.Builder.lock"), lane.lane);
					file_lock = File.Open (lock_file, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
					Logger.Log ("Builder lock aquired successfully for lane '{0}'.", lane.lane);
				} catch (IOException ex) {
					Logger.Log ("Could not aquire builder lock for lane '{1}': {0}", ex.Message, lane.lane);
					return;
				}

				int counter = 10;
				do {
					revisionwork = db.GetRevisionWork (lane, master_host, host);
					if (revisionwork == null) {
						Logger.Log ("There is nothing to do for lane '{0}'.", lane.lane);
						return;
					}
				} while (!revisionwork.SetWorkHost (db, host) && counter-- > 0);

				if (revisionwork.State == DBState.NotDone)
					revisionwork.State = DBState.Executing;

				revision = new DBRevision (db, revisionwork.revision_id);

				// Set revision-specific paths
				BUILDER_DATA_LOG_DIR = Configuration.GetDataLogDir (lane.lane, revision.revision);
				BUILDER_DATA_SOURCE_DIR = Configuration.GetDataSourceDir (lane.lane, revision.revision);
				BUILDER_DATA_INSTALL_DIR = Configuration.GetDataInstallDir (lane.lane, revision.revision);

				if (!Directory.Exists (BUILDER_DATA_SOURCE_DIR))
					Directory.CreateDirectory (BUILDER_DATA_SOURCE_DIR);
				if (!Directory.Exists (BUILDER_DATA_LOG_DIR))
					Directory.CreateDirectory (BUILDER_DATA_LOG_DIR);

				// Get the path of the temporary directory for this process
				using (Process p = Process.GetCurrentProcess ()) {
					temp_dir = Path.Combine (Path.Combine (Path.Combine (Path.GetTempPath (), "builder"), lane.lane), p.Id.ToString ());
					if (Directory.Exists (temp_dir)) {
						// This should happen very rarely
						Directory.Delete (temp_dir, true);
					}
					Directory.CreateDirectory (temp_dir);
					Logger.Log ("Created temporary directory: {0}", temp_dir);
				}

				// Write all the files to the temporary directory
				foreach (DBLanefile file in lane.GetFiles (db))
					File.WriteAllText (Path.Combine (temp_dir, file.name), Dos2Unix (file.contents));

				do {
					if (Configuration.IsStopSignaled) {
						Logger.Log ("Builder has received stop signal.");
						break;
					}

					if (pending_work != null) {
						revisionwork.UpdateState (db);

						if (host.QueueManagement == DBQueueManagement.ExecuteLatestAsap && !db.IsLatestRevisionWork (revisionwork)) {
							Logger.Log ("Builder has detected that a new revision has been committed, and this host is configured to build new revisions asap. Exiting.");
							break; ;
						}
					}

					pending_work = revisionwork.GetNextWork (db, lane, master_host, revision);

					if (pending_work == null || pending_work.Count == 0) {
						Logger.Log ("There is no more work to do for lane '{0}'", lane.lane);
						break;
					}

					threads.Clear ();
					infos.Clear ();

					int timeout = 60;

					// Create worker data.
					for (int i = 0; i < pending_work.Count; i++) {
						BuildInfo info = new BuildInfo ();
						info.BUILDER_DATA_INSTALL_DIR = BUILDER_DATA_INSTALL_DIR;
						info.BUILDER_DATA_LOG_DIR = BUILDER_DATA_LOG_DIR;
						info.BUILDER_DATA_SOURCE_DIR = BUILDER_DATA_SOURCE_DIR;
						info.command = new DBCommand (db, pending_work [i].command_id);
						info.host = host;
						info.master_host = master_host;
						info.lane = lane;
						info.number = i;
						info.revision = revision;
						info.temp_dir = temp_dir;
						info.work = new DBWork (db, pending_work [i].id);
						timeout = Math.Max (timeout, info.command.timeout);
						Logger.Log ("Starting thread #{3} for revision {0} step {1} with sequence {2}", revision.revision, pending_work [i].command, pending_work [i].sequence, i);
						infos.Add (info);
					}

					// Start worker threads.
					for (int i = 0; i < infos.Count; i++) {
						threads.Add (new Thread (Build));
						threads [i].Start (infos [i]);
					}

					// Wait until all threads have stopped or the max timeout + 15 minutes has passed.
					// Give a max of max timeout + 15 minutes before all threads must have stopped.
					DateTime start = DateTime.Now;
					DateTime end = start.AddMinutes (timeout + 15);
					for (int i = 0; i < threads.Count; i++) {
						DateTime now = DateTime.Now;
						TimeSpan duration;

						if (end <= DateTime.Now) {
							if (threads [i].ThreadState != System.Threading.ThreadState.Stopped) {
								Logger.Log ("Aborting thread #{0}, time is up.", i);
								threads [i].Abort ();
								Thread.Sleep (1000);
							}
						} else {
							duration = end - now;
							if (threads [i].ThreadState != System.Threading.ThreadState.Stopped) {
								if (!threads [i].Join (duration)) {
									Logger.Log ("Aborting thread #{0}, join timed out.", i);
									threads [i].Abort ();
									Thread.Sleep (1000);
								}
							}
						}
					}

					for (int i = 0; i < infos.Count; i++) {
						if (infos [i].work.State == DBState.Aborted) {
							aborted = true;
							break;
						}
					}

					Logger.Log ("Revision {0}, executed {1} step(s) with sequence {2}.", revision.revision, pending_work.Count, pending_work [0].sequence);
				} while (!aborted);

				revisionwork.UpdateState (db);
				if (revisionwork.completed) {
					// Cleanup after us.
					string base_dir = Configuration.GetDataRevisionDir (lane.lane, revision.revision);
					try {
						Directory.Delete (base_dir, true);
						Logger.Log ("Successfully deleted directory {0}", base_dir);
					} catch (Exception ex) {
						Logger.Log ("Couldn't delete directory {0}: {1}\n{2}", base_dir, ex.Message, ex.StackTrace);
					}
				}

				Logger.Log ("Revision {0} on lane {1} finished.", revision.revision, lane.lane);
			} catch (Exception ex) {
				Logger.Log ("Exception while building lane '{0}': {1}", lane.lane, ex.ToString ());
			} finally {
				if (temp_dir != null && Directory.Exists (temp_dir))
					Directory.Delete (temp_dir, true);
				// This is strictly not required, given that the OS will release the file once this process
				// exists. We still need to access the file_lock somehow here, given that otherwise the gc
				// might determine that the object should be freed at any moment, causing the lock to get freed
				// too early.
				if (file_lock != null)
					file_lock.Close ();
			}
			Logger.Log ("Builder finished with lane '{0}'", lane.lane);
		}

		public static void CheckLog (string log_file, DB db, DBWork work)
		{
			string line, l;
			string cmd;
			int index;

			using (FileStream fs = new FileStream (log_file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				using (StreamReader reader = new StreamReader (fs)) {
					while (null != (l = reader.ReadLine ())) {
						line = l;
						if (!line.StartsWith ("@Moonbuilder:"))
							continue;
						line = line.Substring ("@Moonbuilder:".Length);
						while (line.StartsWith (" "))
							line = line.Substring (1);

						index = line.IndexOf (':');
						if (index == -1)
							continue;

						cmd = line.Substring (0, index);
						line = line.Substring (index + 1);
						while (line.StartsWith (" "))
							line = line.Substring (1);

						switch (cmd) {
						case "AddFile":
							try {
								Logger.Log ("@Moonbuilder command: '{0}' args: '{1}'", cmd, line);
								work.AddFile (db, line.Trim (), false);
							} catch (Exception e) {
								Logger.Log ("Error while executing @Moonbuilder command '{0}': '{1}'", cmd, e.Message);
							}
							break;
						case "AddHiddenFile":
							try {
								Logger.Log ("@Moonbuilder command: '{0}' args: '{1}'", cmd, line);
								work.AddFile (db, line.Trim (), true);
							} catch (Exception e) {
								Logger.Log ("Error while executing @Moonbuilder command '{0}': '{1}'", cmd, e.Message);
							}
							break;
						default:
							Logger.Log ("Invalid @Moonbuilder command: '{0}', entire line: '{1}'", cmd, l);
							break;
						}
					}
				}
			}
		}
	}
}

