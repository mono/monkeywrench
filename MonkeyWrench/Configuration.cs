/*
 * Configuration.cs
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
using System.Text;
using System.Xml;

using NDesk.Options;
using MonkeyWrench;

namespace MonkeyWrench
{
	public static class Configuration
	{
		public static string LogFile = Path.Combine (Path.GetTempPath (), "MonkeyWrench.log");
		public static string DataDirectory = Path.Combine (Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "monkeywrench"), "data");
		public static string RevDataDirectory;
		public static string Host;
		public static string WebServiceUrl;
		public static string WebSiteUrl;
		public static bool ForceFullUpdate = true;
		public static string WebServicePassword;
		public static string DatabaseHost = "localhost";
		public static string DatabasePort;
		public static string DatabaseUser = "builder";
		public static string DatabasePassword;
		public static bool StoreFilesInDB = false;
		public static int ConnectionRetryDuration = 1440;
		public static string LockingAlgorithm = "semaphore";
		public static string SchedulerAccount = "scheduler";
		public static string SchedulerPassword;
		public static string ChildProcessAlgorithm = "pgrep";
		public static string Platform = ""; // detect automatically
		public static string AllowedCommitReporterIPs = "";
		public static string SiteSkin = "wrench";
		public static int UploadPort = 0; // default = 0 (any port)
		public static int LogVerbosity = 1; // 0: quiet, 1: some messages, 2: verbose (default: 1)
		public static bool AllowAnonymousAccess = true;
		public static int NoOutputTimeout = 30; // timeout after this many minutes of no output.
		public static int NoProgressTimeout = 60; // timeout after this many minutes if the test thread(s) don't seem to be progressing.

		// openid
		public static string OpenIdProvider = null;
		public static string OpenIdRoles = null;
		public static bool AllowPasswordLogin = true;
		public static bool AutomaticScheduler = false;
		public static int AutomaticSchedulerInterval = 60;

		//the following are used by the database manager.
		public static bool CleanLargeObjects;
		public static bool CompressFiles;
		public static bool ExecuteDeletionDirectives;
		public static bool MoveFilesToFileSystem;
		public static bool MoveFilesToDatabase;

		private static string GetNodeValue (this XmlNode node, string @default)
		{
			return node == null ? @default : node.InnerText;
		}

		private static void ShowHelp (OptionSet options)
		{
			Console.WriteLine ("{0} usage is: {0} [options]", Path.GetFileName (System.Reflection.Assembly.GetEntryAssembly ().Location));
			Console.WriteLine ();
			options.WriteOptionDescriptions (Console.Out);
		}

		public static bool VerifyBuildBotConfiguration ()
		{
			bool result = true;

			if (string.IsNullOrEmpty (DataDirectory)) {
				Logger.Log ("BuildBot configuration error: DataDirectory not provided.");
				result = false;
			} else if (!Directory.Exists (DataDirectory)) {
				Directory.CreateDirectory (DataDirectory);
				Logger.Log ("BuildBot configuration warning: DataDirectory ('{0}') did not exist, it was created.", DataDirectory);
			}

			if (string.IsNullOrEmpty (RevDataDirectory)) {
				RevDataDirectory = Path.Combine (DataDirectory, "lanes");
			}

			if (!Directory.Exists (RevDataDirectory)) {
				Directory.CreateDirectory (RevDataDirectory);
				Logger.Log ("BuildBot configuration warning: RevDataDirectory ('{0}') did not exist, it was created.", RevDataDirectory);
			}

			if (string.IsNullOrEmpty (WebServiceUrl)) {
				Logger.Log ("BuildBot configuration error: WebServiceUrl not provided.");
				result = false;
			}

			if (string.IsNullOrEmpty (Host)) {
				Logger.Log ("BuildBot configuration error: Host not provided.");
				result = false;
			}

			return result;
		}

		private static void ExecuteSuspendedProcessHack (string [] arguments)
		{
			// this is a hack around the fact that we can't create a suspended process without p/invokes
			// see comment in JobWindows.Start for a complete explanation
			if (arguments.Length < 3 || arguments [0] != "/respawn")
				return;

			string mutex_name = arguments [1];
			string respawn_filename = arguments [2];
			string respawn_arguments = string.Empty;

			for (int i = 3; i < arguments.Length; i++) {
				if (i > 3)
					respawn_arguments += " ";
				if (arguments [i].IndexOf (' ') >= 0) {
					respawn_arguments += "\"" + arguments [i] + "\"";
				} else {
					respawn_arguments += arguments [i];
				}
			}

			System.Threading.Mutex m = System.Threading.Mutex.OpenExisting (mutex_name);
			Logger.Log ("Respawn process: acquiring mutex...");
			m.WaitOne (); // wait for the mutex
			Logger.Log ("Respawn process: mutex acquired, releasing it");
			m.ReleaseMutex (); // no need to keep it locked
			using (System.Diagnostics.Process p = new System.Diagnostics.Process ()) {
				p.StartInfo.FileName = respawn_filename;
				p.StartInfo.Arguments = respawn_arguments;
				p.StartInfo.UseShellExecute = false;
				Logger.Log ("Respawning '{0}' with '{1}'", p.StartInfo.FileName, p.StartInfo.Arguments);
				p.Start ();
				p.WaitForExit ();
				Environment.Exit (p.ExitCode);
			}
		}

		public static bool LoadConfiguration (string [] arguments, string file)
		{
			ExecuteSuspendedProcessHack (arguments);

			Logger.Log (2, "Loading configuration from #{0}", file);

			if (string.IsNullOrEmpty (file))
				return false;

			if (!File.Exists (file))
				return false;


			try {
				XmlDocument xml = new XmlDocument ();
				xml.Load (file);

				// parse configuration file

				DataDirectory = xml.SelectSingleNode ("/MonkeyWrench/Configuration/DataDirectory").GetNodeValue (DataDirectory);
				XmlNode node = xml.SelectSingleNode ("/MonkeyWrench/Configuration/RevDataDirectory");
				if (node != null)
					RevDataDirectory = node.GetNodeValue (RevDataDirectory);
				Host = xml.SelectSingleNode ("/MonkeyWrench/Configuration/Host").GetNodeValue (Host);
				LogFile = xml.SelectSingleNode ("/MonkeyWrench/Configuration/LogFile").GetNodeValue (LogFile);
				ForceFullUpdate = Boolean.Parse (xml.SelectSingleNode ("/MonkeyWrench/Configuration/ForceFullUpdate").GetNodeValue (ForceFullUpdate.ToString ()));
				WebServiceUrl = xml.SelectSingleNode ("/MonkeyWrench/Configuration/WebServiceUrl").GetNodeValue (WebServiceUrl);
				WebServicePassword = xml.SelectSingleNode ("/MonkeyWrench/Configuration/WebServicePassword").GetNodeValue (WebServicePassword);
				WebSiteUrl = xml.SelectSingleNode ("/MonkeyWrench/Configuration/WebSiteUrl").GetNodeValue (WebSiteUrl);
				DatabaseHost = xml.SelectSingleNode ("/MonkeyWrench/Configuration/DatabaseHost").GetNodeValue (DatabaseHost);
				DatabasePort = xml.SelectSingleNode ("/MonkeyWrench/Configuration/DatabasePort").GetNodeValue (DatabasePort);
				DatabaseUser = xml.SelectSingleNode("/MonkeyWrench/Configuration/DatabaseUser").GetNodeValue(DatabaseUser);
				DatabasePassword = xml.SelectSingleNode("/MonkeyWrench/Configuration/DatabasePassword").GetNodeValue(DatabasePassword);
				StoreFilesInDB = Boolean.Parse (xml.SelectSingleNode ("MonkeyWrench/Configuration/StoreFilesInDb").GetNodeValue (StoreFilesInDB.ToString ()));
				ConnectionRetryDuration = int.Parse (xml.SelectSingleNode ("MonkeyWrench/Configuration/ConnectionRetryDuration").GetNodeValue (ConnectionRetryDuration.ToString ()));
				LockingAlgorithm = xml.SelectSingleNode ("MonkeyWrench/Configuration/LockingAlgorithm").GetNodeValue (LockingAlgorithm);
				SchedulerAccount = xml.SelectSingleNode ("MonkeyWrench/Configuration/SchedulerAccount").GetNodeValue (SchedulerAccount);
				SchedulerPassword = xml.SelectSingleNode ("MonkeyWrench/Configuration/SchedulerPassword").GetNodeValue (SchedulerPassword);
				ChildProcessAlgorithm = xml.SelectSingleNode ("MonkeyWrench/Configuration/ChildProcessAlgorithm").GetNodeValue (ChildProcessAlgorithm);
				Platform = xml.SelectSingleNode ("MonkeyWrench/Configuration/Platform").GetNodeValue (Platform);
				AllowedCommitReporterIPs = xml.SelectSingleNode ("MonkeyWrench/Configuration/AllowedCommitReporterIPs").GetNodeValue (AllowedCommitReporterIPs);
				LogVerbosity = int.Parse (xml.SelectSingleNode ("MonkeyWrench/Configuration/LogVerbosity").GetNodeValue (LogVerbosity.ToString ()));
				SiteSkin = xml.SelectSingleNode ("MonkeyWrench/Configuration/SiteSkin").GetNodeValue (SiteSkin);
				UploadPort = int.Parse (xml.SelectSingleNode ("MonkeyWrench/Configuration/UploadPort").GetNodeValue (UploadPort.ToString ()));
				AllowAnonymousAccess = bool.Parse(xml.SelectSingleNode("MonkeyWrench/Configuration/AllowAnonymousAccess").GetNodeValue(AllowAnonymousAccess.ToString()));
				OpenIdProvider = xml.SelectSingleNode ("MonkeyWrench/Configuration/OpenIdProvider").GetNodeValue (OpenIdProvider);
				OpenIdRoles = xml.SelectSingleNode ("MonkeyWrench/Configuration/OpenIdRoles").GetNodeValue (OpenIdRoles);
				AutomaticScheduler = Boolean.Parse (xml.SelectSingleNode ("MonkeyWrench/Configuration/AutomaticScheduler").GetNodeValue (AutomaticScheduler.ToString ()));
				AutomaticSchedulerInterval = int.Parse (xml.SelectSingleNode ("MonkeyWrench/Configuration/AutomaticSchedulerInterval").GetNodeValue (AutomaticSchedulerInterval.ToString ()));
				AllowPasswordLogin = bool.Parse (xml.SelectSingleNode ("MonkeyWrench/Configuration/AllowPasswordLogin").GetNodeValue (AllowPasswordLogin.ToString ()));

				// override from command line

				bool help = false;
				OptionSet set = new OptionSet ()
				{
					{"h|help|?", v => help = true},
					{"datadirectory=", v => DataDirectory = v},
					{"revdatadirectory=", v => RevDataDirectory = v},
					{"host=", v => Host = v},
					{"logfile=", v => LogFile = v},
					{"forcefullupdate=", v => ForceFullUpdate = Boolean.Parse (v.Trim ())},
					{"webserviceurl=", v => WebServiceUrl = v},
					{"webservicepassword=", v => WebServicePassword = v},
					{"websiteurl=", v => WebSiteUrl = v},
					{"databasehost=", v => DatabaseHost = v},
					{"databaseport=", v => DatabasePort = v},
					{"storefilesindb=", v => StoreFilesInDB = Boolean.Parse (v.Trim ())},
					{"connectionretryduration=", v => ConnectionRetryDuration = int.Parse (v.Trim ())},
					{"lockingalgorithm=", v => LockingAlgorithm = v},
					{"scheduleraccount=", v => SchedulerAccount = v},
					{"schedulerpassword=", v => SchedulerPassword = v},
					{"childprocessalgorithm=", v => ChildProcessAlgorithm = v},
					{"platform=", v => Platform = v},
					{"logverbosity=", v => LogVerbosity = int.Parse (v.Trim ())},
					{"siteskin=", v => SiteSkin = v},
					{"uploadport=", v => UploadPort = int.Parse (v.Trim ())},
					{"allowanonymousaccess=", v => AllowAnonymousAccess = bool.Parse (v.Trim ())},
					{"openidprovider=", v => OpenIdProvider = v },
					{"openidroles=", v => OpenIdRoles = v },
					{"automaticscheduler=", v => Boolean.Parse (v.Trim ())},
					{"automaticschedulerinterval=", v => int.Parse (v.Trim ())},
					{"allowpasswordlogin=", v => bool.Parse (v.Trim ())},

					// values for the database manager
					{"compress-files", v => CompressFiles = true},
					{"clean-large-objects", v => CleanLargeObjects = true},
					{"execute-deletion-directives", v => ExecuteDeletionDirectives = true},
					{"move-files-to-file-system", v => MoveFilesToFileSystem = true},
					{"move-files-to-database", v => MoveFilesToDatabase = true},
					{"allowed-commit-reporter-ips", v => AllowedCommitReporterIPs = v},
				};
				List<string> extra = null;
				try {
					extra = set.Parse (arguments);
				} catch (Exception ex) {
					Console.WriteLine ("Error: {0}", ex.Message);
					ShowHelp (set);
					Environment.Exit (1);
				}
				if (help) {
					ShowHelp (set);
					Environment.Exit (0);
				}
				if (extra != null && extra.Count > 0) {
					Console.WriteLine ("Error: unexpected argument(s): {0}", string.Join (" ", extra.ToArray ()));
					ShowHelp (set);
					Environment.Exit (1);
				}

				if (!string.IsNullOrEmpty (DataDirectory) && !Directory.Exists (DataDirectory))
					Directory.CreateDirectory (DataDirectory);

				if (!string.IsNullOrEmpty (RevDataDirectory) && !Directory.Exists (RevDataDirectory))
					Directory.CreateDirectory (RevDataDirectory);

				if (!string.IsNullOrEmpty (GetReleaseDirectory ()) && !Directory.Exists (GetReleaseDirectory ()))
					Directory.CreateDirectory (GetReleaseDirectory ());

			} catch (Exception ex) {
				Console.Error.WriteLine ("MonkeyWrench: Fatal error: Could not load configuration file from: {0}: {1}", file, ex.Message);
				Console.Error.WriteLine (ex.StackTrace);
				Environment.Exit (1);
			}

			Logger.Log (2, "MonkeyWrench: Initialized {1} and loaded configuration file from: {0}", file, ApplicationName);

			return true;
		}

		/// <summary>
		/// Loads any configuration. 
		/// Returns false if loading fails, in which case the application must exit asap.
		/// There's no need to report errors, an error message have been printed to stderr.
		/// </summary>
		/// <returns></returns>
		public static bool LoadConfiguration (string [] arguments)
		{
			if (!LoadConfiguration (arguments, Environment.GetEnvironmentVariable ("MONKEYWRENCH_CONFIG_FILE"))) {
				if (!LoadConfiguration (arguments, Path.Combine (Environment.CurrentDirectory, "MonkeyWrench.xml"))) {
					switch (Environment.OSVersion.Platform) {
					case PlatformID.Win32NT:
					case PlatformID.Win32S:
					case PlatformID.Win32Windows:
					case PlatformID.WinCE:
					//case PlatformID.Xbox:
						if (!LoadConfiguration (arguments, Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "MonkeyWrench.xml"))) {
							Console.Error.WriteLine ("MonkeyWrench: Fatal error: Could not find the configuration file 'MonkeyWrench.xml'.");
							return false;
						}
						break;
					default:
						// if not windows, we assume linux
						if (!LoadConfiguration (arguments, Path.Combine (Environment.GetEnvironmentVariable ("HOME"), Path.Combine (Path.Combine (".config", "MonkeyWrench"), "MonkeyWrench.xml")))) {
							if (!LoadConfiguration (arguments, "/etc/MonkeyWrench.xml")) {
								Console.Error.WriteLine ("MonkeyWrench: Fatal error: Could not find the configuration file 'MonkeyWrench.xml'.");
								return false;
							}
						}
						break;
					}
				}
			}

			return true;
		}


		public static string ApplicationName
		{
			get
			{
				if (System.Reflection.Assembly.GetEntryAssembly () != null)
					return System.Reflection.Assembly.GetEntryAssembly ().GetName ().Name;
				if (System.Reflection.Assembly.GetCallingAssembly () != null)
					return System.Reflection.Assembly.GetCallingAssembly ().GetName ().Name;
				return "MonkeyWrench";
			}
		}

		public static string GetDataLane (int lane_id)
		{
			return Path.Combine (Path.Combine (DataDirectory, "lanes"), lane_id.ToString ());
		}

		/// <summary>
		/// BUILD_DATA/lanes/BUILD_LANE/BUILD_REVISION
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="revision"></param>
		/// <returns></returns>
		public static string GetDataRevisionDir (int lane_id, string revision)
		{
			return Path.Combine (Path.Combine (RevDataDirectory, lane_id.ToString ()), revision.Length > 8 ? revision.Substring (0, 8) : revision);
		}

		/// <summary>
		/// BUILD_DATA/lanes/BUILD_LANE/BUILD_REVISION/logs
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="revision"></param>
		/// <returns></returns>
		public static string GetDataLogDir (int lane_id, string revision)
		{
			return Path.Combine (GetDataRevisionDir (lane_id, revision), "logs");
		}

		public static string GetDependentDownloadDirectory (int lane_id, string dependent_lane, string revision)
		{
			return Path.Combine (Path.Combine (GetDataRevisionDir (lane_id, revision), "dependencies"), dependent_lane);
		}

		/// <summary>
		/// BUILD_DATA/lanes/BUILD_LANE/BUILD_REVISION/source
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="revision"></param>
		/// <returns></returns>
		public static string GetDataSourceDir (int lane_id, string revision)
		{
			return Path.Combine (GetDataRevisionDir (lane_id, revision), "source");
		}

		/// <summary>
		/// BUILD_DATA/lanes/BUILD_LANE/BUILD_REVISION/install
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="revision"></param>
		/// <returns></returns>
		public static string GetDataInstallDir (int lane_id, string revision)
		{
			return Path.Combine (GetDataRevisionDir (lane_id, revision), "install");
		}

		public static string GetPkgConfigPath (int lane_id, string revision)
		{
			string current = Environment.GetEnvironmentVariable ("PKG_CONFIG_PATH");
			string result = Path.Combine (Path.Combine (GetDataInstallDir (lane_id, revision), "lib"), "pkgconfig");
			if (!string.IsNullOrEmpty (current))
				result += ":" + current;
			return result;
		}

		public static string GetLdLibraryPath (int lane_id, string revision)
		{
			string current = Environment.GetEnvironmentVariable ("LD_LIBRARY_PATH");
			string result = Path.Combine (GetDataInstallDir (lane_id, revision), "lib");
			if (!string.IsNullOrEmpty (current))
				result += ":" + current;
			return result;
		}

		public static string GetPath (int lane_id, string revision)
		{
			string current = Environment.GetEnvironmentVariable ("PATH");
			string result = Path.Combine (GetDataInstallDir (lane_id, revision), "bin");
			if (!string.IsNullOrEmpty (current))
				result += ":" + current;
			return result;
		}

		public static string GetCIncludePath (int lane_id, string revision)
		{
			string current = Environment.GetEnvironmentVariable ("C_INCLUDE_PATH");
			string result = Path.Combine (GetDataInstallDir (lane_id, revision), "include");
			if (!string.IsNullOrEmpty (current))
				result += ":" + current;
			return result;
		}

		public static string GetCPlusIncludePath (int lane_id, string revision)
		{
			string current = Environment.GetEnvironmentVariable ("CPLUS_INCLUDE_PATH");
			string result = Path.Combine (GetDataInstallDir (lane_id, revision), "include");
			if (!string.IsNullOrEmpty (current))
				result += ":" + current;
			return result;
		}
		
		/// <summary>
		/// Get the host name of the machine we're running on.
		/// Either BUILDER_HOST or 'Default'
		/// </summary>
		/// <returns></returns>
		public static string GetHost ()
		{
			string result;
			result = Environment.GetEnvironmentVariable ("BUILDER_HOST");
			if (string.IsNullOrEmpty (result))
				result = "Default";
			return result;
		}

		/// <summary>
		/// Get the architecture of the machine we're running on.
		/// Either BUILDER_ARCH or 'Default'
		/// </summary>
		/// <returns></returns>
		public static string GetArch ()
		{
			string result;
			result = Environment.GetEnvironmentVariable ("BUILDER_ARCH");
			if (string.IsNullOrEmpty (result))
				result = "Default";
			return result;
		}

		/// <summary>
		/// The web frontend receives files to ReportCommit.aspx, these files are written into this directory.
		/// The scheduler looks for files in this directory and only accesses the remote repository when
		/// it determines that something has been committed.
		/// </summary>
		/// <returns></returns>
		public static string GetSchedulerCommitsDirectory ()
		{
			return Path.Combine (DataDirectory, "commits");
		}

		/// <summary>
		/// A repository cache directory for the schedulers which may need it (git)
		/// </summary>
		/// <param name="repository"></param>
		/// <returns></returns>
		public static string GetSchedulerRepositoryCacheDirectory (string repository)
		{
			return Path.Combine (Path.Combine (DataDirectory, "SchedulerCache"), repository.Replace (':', '_').Replace ('/', '_').Replace ('\\', '_'));
		}

		/// <summary>
		/// The directory where the monkeywrench releases are stored
		/// </summary>
		/// <returns></returns>
		public static string GetReleaseDirectory ()
		{
			return Path.Combine (DataDirectory, "Releases");
		}

		/// <summary>
		/// The directory where the database stores the files (as opposed to storing the files in the db itself)
		/// </summary>
		/// <returns></returns>
		public static string GetFilesDirectory ()
		{
			return Path.Combine (Path.Combine (DataDirectory, "db"), "files");
		}

		/// <summary>
		/// The platform we're currently executing on.
		/// </summary>
		/// <returns></returns>
		public static Platform GetPlatform ()
		{
			if (string.IsNullOrEmpty (Platform)) {
				switch (Environment.OSVersion.Platform) {
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.WinCE:
					return MonkeyWrench.Platform.Windows;
				case PlatformID.MacOSX:
					return MonkeyWrench.Platform.Mac;
				case PlatformID.Unix:
				case (PlatformID) 128:
				default:
					// from here: http://stackoverflow.com/q/10138040/183422
					// Well, there are chances MacOSX is reported as Unix instead of MacOSX.
					// Instead of platform check, we'll do a feature checks (Mac specific root folders)
					if (Directory.Exists("/Applications")
					    & Directory.Exists("/System")
					    & Directory.Exists("/Users")
					    & Directory.Exists("/Volumes"))
						return MonkeyWrench.Platform.Mac;
					else
						return MonkeyWrench.Platform.Linux;
				}
			} else {
				switch (Platform.ToLowerInvariant ()) {
				case "windows":
					return MonkeyWrench.Platform.Windows;
				case "mac":
					return MonkeyWrench.Platform.Mac;
				case "linux":
				default:
					return MonkeyWrench.Platform.Linux;
				}
			}
		}

		public static bool IsCygwin 
		{
			get { 
				if (string.Equals ("cygwin", Environment.GetEnvironmentVariable ("OSTYPE")))
					return true;
				if (!string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("CYGWIN")))
					return true;
				/* the above doesn't seem to be exported from the cygwin shell, so have a last guess about the paths */
				if (Environment.GetEnvironmentVariable ("PATH").IndexOf ("cygwin") >= 0)
					return true;
				return false;
			}
		}

		public static string CygwinizePath (string path)
		{
			if (!IsCygwin)
				return path;
			return path.Replace ('\\', '/');
		}

		public static string GetWebSiteUrl ()
		{
			if (string.IsNullOrEmpty (WebSiteUrl)) {
				WebSiteUrl = new Uri (WebServiceUrl).GetComponents (UriComponents.SchemeAndServer, UriFormat.UriEscaped);	
			}
			return WebSiteUrl;
		}
	}
}

