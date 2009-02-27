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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Builder
{
	public static class Configuration
	{
		public static string DEFAULT_BUILDER_LOG = Path.Combine (Path.GetTempPath (), "Builder.Builder.Builder.log");
		public static string DEFAULT_BUILDER_CRON_LOG = Path.Combine (Path.GetTempPath (), "Builder.Builder.Builder.cronlog");
		public static string DEFAULT_UPDATER_LOG = Path.Combine (Path.GetTempPath (), "Builder.Database.Updater.log");
		public static string DEFAULT_UPDATER_CRON_LOG = Path.Combine (Path.GetTempPath (), "Builder.Database.Updater.cronlog");

		public static string BUILDER_DATA;
		public static string BUILDER_CONFIG;
		
        public static string Host;
		public static string MasterHost;

		static Configuration ()
		{
			BUILDER_DATA = Environment.GetEnvironmentVariable ("BUILDER_DATA");
			BUILDER_CONFIG = Environment.GetEnvironmentVariable ("BUILDER_CONFIG");
			Host = Environment.GetEnvironmentVariable ("BUILDER_HOST");
			MasterHost = Environment.GetEnvironmentVariable ("BUILDER_HOST_MASTER");

			if (string.IsNullOrEmpty (Host))
				Host = Environment.GetEnvironmentVariable ("BUILD_HOST");

			if (string.IsNullOrEmpty (MasterHost))
				MasterHost = Host;

			if (string.IsNullOrEmpty (Logger.LogFile))
				Logger.LogFile = Path.Combine (Path.GetTempPath (), "Builder.log");
		}

		public static void InitializeApp (string [] commandline_arguments, string appname)
		{
			if (commandline_arguments != null) {
				foreach (string arg in commandline_arguments) {
					if (arg.StartsWith ("-logfile:")) {
						Logger.LogFile = arg.Substring (9);
					} else if (arg.StartsWith ("-datadir:")) {
						BUILDER_DATA = arg.Substring (9);
					} else if (arg.StartsWith ("-configdir:")) {
						BUILDER_CONFIG = arg.Substring (11);
                    }else if (arg.StartsWith ("-host:")) {
                        Host = arg.Substring(6);
					} else {
						throw new Exception (string.Format ("Invalid argument: {0}", arg));
					}
				}
			}

			Logger.Log ("Initialized {0}", appname);
			Logger.Log ("BUILDER_DATA: '{0}'", BUILDER_DATA);
			Logger.Log ("BUILDER_CONFIG: '{0}'", BUILDER_CONFIG);

			if (!Directory.Exists (BUILDER_DATA))
				throw new DirectoryNotFoundException (string.Format ("The data directory '{0}' could not be found.", BUILDER_DATA));

			if (!Directory.Exists (BUILDER_CONFIG))
				throw new DirectoryNotFoundException (string.Format ("The config directory '{0}' could not be found.", BUILDER_CONFIG));

            if (string.IsNullOrEmpty(Host))
                throw new Exception("No host specified.");
		}

		public static bool IsStopSignaled
		{
			get
			{
				return File.Exists (GetStopFile ());
			}
		}

		public static string GetStopFile ()
		{
			return Path.Combine (BUILDER_DATA, "stop");
		}

		public static string GetDataLane (string lane)
		{
			return Path.Combine (Path.Combine (BUILDER_DATA, "lanes"), lane);
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
			return Path.Combine (BUILDER_DATA, "commits");
		}
	}
}
