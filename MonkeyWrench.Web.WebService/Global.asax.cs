/*
 * Global.asax.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2010 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace MonkeyWrench.WebServices
{
	public class Global : System.Web.HttpApplication
	{
		private Timer scheduler;

		protected void Application_Start (object sender, EventArgs e)
		{
			Logger.Log ("Web service started"); 
			Configuration.LoadConfiguration (new string [] {});
			Notifications.Start ();
			Maintenance.Start ();

			if (Configuration.AutomaticScheduler)
				scheduler = new System.Threading.Timer (Schedule, null, 0, Configuration.AutomaticSchedulerInterval * 1000);
		}

		private void Schedule (object state)
		{
			try {
				MonkeyWrench.Scheduler.Scheduler.ExecuteSchedulerAsync (false);
			} catch (Exception e) {
				Logger.Log ("Automatic scheduler failed: {0}", e.Message);
			}
		}

		protected void Session_Start (object sender, EventArgs e)
		{
		}

		protected void Application_BeginRequest (object sender, EventArgs e)
		{
		}

		protected void Application_AuthenticateRequest (object sender, EventArgs e)
		{
		}

		protected void Application_Error (object sender, EventArgs e)
		{
		}

		protected void Session_End (object sender, EventArgs e)
		{
		}

		protected void Application_End (object sender, EventArgs e)
		{
			if (scheduler != null) {
				scheduler.Dispose ();
				scheduler = null;
			}

			Upload.StopListening ();
		}
	}
}

