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
		/*
		 * The configuration is stored in an xml file whose schema looks like this:
		 * (see xml comments for more information about each entry)
		 * <MonkeyWrench Version="2">
		 *   <Configuration>
		 *	   <DataDirectory />
		 *	   <DatabaseDirectory />
		 *	   <Host />
		 *	   <LogFile />
		 *	   <WebServiceUrl />
		 *	   <ForceFullUpdate>true|false</ForceFullUpdate>
		 *	   <WebServicePassword />
		 *	   <DatabaseHost />
		 *	   <DatabasePort />
		 *   </Configuration>
		 * </MonkeyWrench>
		 * 
		 * All values can also be set with the equivalent (case insensitive) command line arguments (which override the configuration file)
		 */

		/// <summary>
		/// The log file. Defaults to MonkeyWrench.log in the tmp directory (which is platform specific).
		/// </summary>
		public static string LogFile = Path.Combine (Path.GetTempPath (), "MonkeyWrench.log");

		/// <summary>
		/// Data directory. Defaults to ~/monkeywrench/data.
		/// For buildbots: The directory of the build data.
		/// For web server: Used to derive the paths where the post commit hook reports are stored.
		/// For database server: User to derive the paths where the database is.
		/// </summary>
		public static string DataDirectory = Path.Combine (Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "monkeywrench"), "data");

		/// <summary>
		/// The name of the current host. Only relevant for buildbots (in which case it is required).
		/// </summary>
		public static string Host;

		/// <summary>
		/// The url (http://host[:port]/...) for the web service. Required for buildbots and web server.
		/// </summary>
		public static string WebServiceUrl;

		/// <summary>
		/// Specifies if a full update should be performed on the database (as opposed to only update what was reported through post-commit hooks, etc).
		/// Only relevant for database/web server. Defaults to true.
		/// </summary>
		public static bool ForceFullUpdate = true;

		/// <summary>
		/// If required, a password to log into the web service.
		/// </summary>
		public static string WebServicePassword;

		/// <summary>
		/// The host machine of the database. Defaults to 'localhost'.
		/// </summary>
		public static string DatabaseHost = "localhost";

		/// <summary>
		/// The port to use to connect to the database.
		/// </summary>
		public static string DatabasePort;

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

		static bool LoadConfiguration (string [] arguments, string file)
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

				// override from command line

				bool help = false;
				OptionSet set = new OptionSet ()
				{
					{"h|help|?", v => help = true},
					{"datadirectory", v => DataDirectory = v},
					{"host", v => Host = v},
					{"logfile", v => LogFile = v},
					{"forcefullupdate", v => ForceFullUpdate = Boolean.Parse (v)},
					{"webserviceurl", v => WebServiceUrl = v},
					{"webservicepassword", v => WebServicePassword = v},
					{"databasehost", v => DatabaseHost = v},
					{"databaseport", v => DatabasePort = v},
				};
				if (help) {
					ShowHelp (set);
					Environment.Exit (0);
				}
				List<string> extra = null;
				try {
					extra = set.Parse (arguments);
				} catch (OptionException ex) {
					Console.WriteLine ("Error: {0}", ex.Message);
					ShowHelp (set);
					Environment.Exit (1);
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
			//
			// Possible locations of the configuration file (MonkeyWrench.xml) - in this order:
			// 
			// $MONKEYWRENCH_CONFIG_FILE (with this env variable the file can be named anything)
			// $(PWD)/MonkeyWrench.xml
			// linux specifics paths:
			// $(HOME)/.config/MonkeyWrench/MonkeyWrench.xml
			// /etc/MonkeyWrench.xml
			// windows specific paths:
			// (todo)

			if (!LoadConfiguration (arguments, Environment.GetEnvironmentVariable ("MONKEYWRENCH_CONFIG_FILE"))) {
				if (!LoadConfiguration (arguments, Path.Combine (Environment.CurrentDirectory, "MonkeyWrench.xml"))) {
					switch (Environment.OSVersion.Platform) {
					case PlatformID.Win32NT:
					case PlatformID.Win32S:
					case PlatformID.Win32Windows:
					case PlatformID.WinCE:
					case PlatformID.Xbox:
						Console.Error.WriteLine ("MonkeyWrench: Fatal error: Could not find the configuration file 'MonkeyWrench.xml'.");
						return false;
					default:
						// if not windows, we assume linux
						if (!LoadConfiguration (arguments, Path.Combine (Environment.GetEnvironmentVariable ("HOME"), Path.Combine (Path.Combine (".config","MonkeyWrench"), "MonkeyWrench.xml")))) {
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
			get { 
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
	}
}
