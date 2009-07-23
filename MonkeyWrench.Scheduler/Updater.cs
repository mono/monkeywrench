/*
 * Updater.cs
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
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

using MonkeyWrench.Web.WebServices;
using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Scheduler
{
	public static class Updater
	{
		public static void Main (string [] args)
		{
			ProcessHelper.Exit (Main2 (args)); // Work around #499702
		}

		public static int Main2 (string [] args)
		{
			try {
				if (!Configuration.LoadConfiguration (args))
					return 1;

				WebServices WebService = WebServices.Create ();
				WebService.CreateLogin (Configuration.SchedulerAccount, Configuration.SchedulerPassword);
				WebService.ExecuteScheduler (WebService.WebServiceLogin, Configuration.ForceFullUpdate);
			} catch (Exception ex) {
				Logger.Log ("Scheduler exception: {0}", ex);
			}

			return 0;
		}
	}
}
