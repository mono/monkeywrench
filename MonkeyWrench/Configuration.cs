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
		public static string Host;
		public static string WebServiceUrl;
		public static bool ForceFullUpdate = true;
		public static string WebServicePassword;
		public static string DatabaseHost = "localhost";
		public static string DatabasePort;
		public static bool StoreFilesInDB = false;
		public static int ConnectionRetryDuration = 1440;
		public static string LockingAlgorithm = "semaphore";
		public static string SchedulerAccount = "scheduler";
		public static string SchedulerPassword;
		public static string ChildProcessAlgorithm = "pgrep";
		public static string Platform = ""; // detect automatically

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

		public static bool LoadConfiguration (string [] arguments, string file)
		{
			if (string.IsNullOrEmpty (file))
				return false;

			if (!File.Exists (file))
				return false;


			try {
				XmlDocument xml = new XmlDocument ();
				xml.Load (file);

				// parse configuration file

				DataDirectory = xml.SelectSingleNode ("/MonkeyWrench/Configuration/DataDirectory").GetNodeValue (DataDirectory);
				Host = xml.SelectSingleNode ("/MonkeyWrench/Configuration/Host").GetNodeValue (Host);
				LogFile = xml.SelectSingleNode ("/MonkeyWrench/Configuration/LogFile").GetNodeValue (LogFile);
				ForceFullUpdate = Boolean.Parse (xml.SelectSingleNode ("/MonkeyWrench/Configuration/ForceFullUpdate").GetNodeValue (ForceFullUpdate.ToString ()));
				WebServiceUrl = xml.SelectSingleNode ("/MonkeyWrench/Configuration/WebServiceUrl").GetNodeValue (WebServiceUrl);
				WebServicePassword = xml.SelectSingleNode ("/MonkeyWrench/Configuration/WebServicePassword").GetNodeValue (WebServicePassword);
				DatabaseHost = xml.SelectSingleNode ("/MonkeyWrench/Configuration/DatabaseHost").GetNodeValue (DatabaseHost);
				DatabasePort = xml.SelectSingleNode ("/MonkeyWrench/Configuration/DatabasePort").GetNodeValue (DatabasePort);
				StoreFilesInDB = Boolean.Parse (xml.SelectSingleNode ("MonkeyWrench/Configuration/StoreFilesInDb").GetNodeValue (StoreFilesInDB.ToString ()));
				ConnectionRetryDuration = int.Parse (xml.SelectSingleNode ("MonkeyWrench/Configuration/ConnectionRetryDuration").GetNodeValue (ConnectionRetryDuration.ToString ()));
				LockingAlgorithm = xml.SelectSingleNode ("MonkeyWrench/Configuration/LockingAlgorithm").GetNodeValue (LockingAlgorithm);
				SchedulerAccount = xml.SelectSingleNode ("MonkeyWrench/Configuration/SchedulerAccount").GetNodeValue (SchedulerAccount);
				SchedulerPassword = xml.SelectSingleNode ("MonkeyWrench/Configuration/SchedulerPassword").GetNodeValue (SchedulerPassword);
				ChildProcessAlgorithm = xml.SelectSingleNode ("MonkeyWrench/Configuration/ChildProcessAlgorithm").GetNodeValue (ChildProcessAlgorithm);
				Platform = xml.SelectSingleNode ("MonkeyWrench/Configuration/Platform").GetNodeValue (Platform);

				// override from command line

				bool help = false;
				OptionSet set = new OptionSet ()
				{
					{"h|help|?", v => help = true},
					{"datadirectory=", v => DataDirectory = v},
					{"host=", v => Host = v},
					{"logfile=", v => LogFile = v},
					{"forcefullupdate=", v => ForceFullUpdate = Boolean.Parse (v.Trim ())},
					{"webserviceurl=", v => WebServiceUrl = v},
					{"webservicepassword=", v => WebServicePassword = v},
					{"databasehost=", v => DatabaseHost = v},
					{"databaseport=", v => DatabasePort = v},
					{"storefilesindb=", v => StoreFilesInDB = Boolean.Parse (v.Trim ())},
					{"connectionretryduration=", v => ConnectionRetryDuration = int.Parse (v.Trim ())},
					{"lockingalgorithm=", v => LockingAlgorithm = v},
					{"scheduleraccount=", v => SchedulerAccount = v},
					{"schedulerpassword=", v => SchedulerPassword = v},
					{"childprocessalgorithm=", v => ChildProcessAlgorithm = v},
					{"platform=", v => Platform = v},

					// values for the database manager
					{"compress-files", v => CompressFiles = true},
					{"clean-large-objects", v => CleanLargeObjects = true},
					{"execute-deletion-directives", v => ExecuteDeletionDirectives = true},
					{"move-files-to-file-system", v => MoveFilesToFileSystem = true},
					{"move-files-to-database", v => MoveFilesToDatabase = true},
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

			} catch (Exception ex) {
				Console.Error.WriteLine ("MonkeyWrench: Fatal error: Could not load configuration file from: {0}: {1}", file, ex.Message);
				Environment.Exit (1);
			}

			Logger.Log ("MonkeyWrench: Initialized {1} and loaded configuration file from: {0}", file, ApplicationName);

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
						Console.Error.WriteLine ("MonkeyWrench: Fatal error: Could not find the configuration file 'MonkeyWrench.xml'.");
						return false;
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

		public static string GetDataLane (string lane)
		{
			return Path.Combine (Path.Combine (DataDirectory, "lanes"), lane);
		}

		/// <summary>
		/// BUILD_DATA/lanes/BUILD_LANE/BUILD_REVISION
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="revision"></param>
		/// <returns></returns>
		public static string GetDataRevisionDir (string lane, string revision)
		{
			return Path.Combine (GetDataLane (lane), revision);
		}

		/// <summary>
		/// BUILD_DATA/lanes/BUILD_LANE/BUILD_REVISION/logs
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="revision"></param>
		/// <returns></returns>
		public static string GetDataLogDir (string lane, string revision)
		{
			return Path.Combine (GetDataRevisionDir (lane, revision), "logs");
		}

		public static string GetDependentDownloadDirectory (string lane, string dependent_lane, string revision)
		{
			return Path.Combine (Path.Combine (GetDataRevisionDir (lane, revision), "dependencies"), dependent_lane);
		}

		/// <summary>
		/// BUILD_DATA/lanes/BUILD_LANE/BUILD_REVISION/source
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="revision"></param>
		/// <returns></returns>
		public static string GetDataSourceDir (string lane, string revision)
		{
			return Path.Combine (GetDataRevisionDir (lane, revision), "source");
		}

		/// <summary>
		/// BUILD_DATA/lanes/BUILD_LANE/BUILD_REVISION/install
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="revision"></param>
		/// <returns></returns>
		public static string GetDataInstallDir (string lane, string revision)
		{
			return Path.Combine (GetDataRevisionDir (lane, revision), "install");
		}

		public static string GetPkgConfigPath (string lane, string revision)
		{
			string current = Environment.GetEnvironmentVariable ("PKG_CONFIG_PATH");
			string result = Path.Combine (Path.Combine (GetDataInstallDir (lane, revision), "lib"), "pkgconfig");
			if (!string.IsNullOrEmpty (current))
				result += ":" + current;
			return result;
		}

		public static string GetLdLibraryPath (string lane, string revision)
		{
			string current = Environment.GetEnvironmentVariable ("LD_LIBRARY_PATH");
			string result = Path.Combine (GetDataInstallDir (lane, revision), "lib");
			if (!string.IsNullOrEmpty (current))
				result += ":" + current;
			return result;
		}

		public static string GetPath (string lane, string revision)
		{
			string current = Environment.GetEnvironmentVariable ("PATH");
			string result = Path.Combine (GetDataInstallDir (lane, revision), "bin");
			if (!string.IsNullOrEmpty (current))
				result += ":" + current;
			return result;
		}

		public static string GetCIncludePath (string lane, string revision)
		{
			string current = Environment.GetEnvironmentVariable ("C_INCLUDE_PATH");
			string result = Path.Combine (GetDataInstallDir (lane, revision), "include");
			if (!string.IsNullOrEmpty (current))
				result += ":" + current;
			return result;
		}

		public static string GetCPlusIncludePath (string lane, string revision)
		{
			string current = Environment.GetEnvironmentVariable ("CPLUS_INCLUDE_PATH");
			string result = Path.Combine (GetDataInstallDir (lane, revision), "include");
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

	}
}
