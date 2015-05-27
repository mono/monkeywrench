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
using log4net;

using MonkeyWrench.Web.WebServices;

namespace MonkeyWrench.Scheduler
{
	public static class Updater
	{
		static readonly ILog log = LogManager.GetLogger (typeof (Updater));

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
				log.ErrorFormat ("Scheduler exception: {0}", ex);
				return 2;
			}

			return 0;
		}
	}
}
