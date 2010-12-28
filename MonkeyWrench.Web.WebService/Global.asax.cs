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
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace MonkeyWrench.Web.WebService
{
	public class Global : System.Web.HttpApplication
	{

		protected void Application_Start (object sender, EventArgs e)
		{
			Logger.Log ("Web service started"); 
			Configuration.LoadConfiguration (new string [] {});
			Notifications.Start ();
			
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
		}
	}
}