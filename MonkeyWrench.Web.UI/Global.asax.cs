/*
 * Global.asax.cs
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
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace MonkeyWrench.Web.UI
{
	public class Global : System.Web.HttpApplication
	{

		protected void Application_Start (object sender, EventArgs e)
		{
			Configuration.LoadConfiguration (new string [] {});
		}

		protected void Session_Start (object sender, EventArgs e)
		{

		}

		protected void Application_BeginRequest (object sender, EventArgs e)
		{

		}

		protected void Application_AuthenticateRequest (object sender, EventArgs e)
		{
			if (HttpContext.Current.User != null) {
				if (HttpContext.Current.User.Identity.IsAuthenticated) {
					if (HttpContext.Current.User.Identity is FormsIdentity) {
						FormsIdentity id = (FormsIdentity) HttpContext.Current.User.Identity;
						HttpContext.Current.User = new System.Security.Principal.GenericPrincipal (id, Utils.WebService.GetRoles (id.Name));
					}
				}
			}
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