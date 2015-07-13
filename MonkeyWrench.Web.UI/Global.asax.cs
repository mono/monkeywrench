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
using System.Web;
using log4net;

using MonkeyWrench.WebServices;

namespace MonkeyWrench.Web.UI
{
	public class Global : System.Web.HttpApplication
	{
		private static readonly ILog log = LogManager.GetLogger (typeof (Global));

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
		}

		protected void Application_Error (object sender, EventArgs e)
		{
			Response.Clear ();

			Response.ContentType = "text/html";

			Exception ex = Server.GetLastError ();
			Server.ClearError ();

			// Unwrap HttpUnhandledException
			Exception realException = ex.GetBaseException ();

			if (realException is HttpException && (realException as HttpException).GetHttpCode() == 404) {
				// Page not found.
				ErrorPage.transferToError (Server, Context, "Page not found.", HttpUtility.HtmlEncode (realException.Message), 404);
			} else if (realException is UnauthorizedException) {
				// User is not authorized to view this page.
				ErrorPage.transferToError (Server, Context, "Unauthorized", HttpUtility.HtmlEncode (realException.Message), 403);
			} else {
				// Unhandled error. Log it and display an error page. 
				log.ErrorFormat ("{0} {1}: {2}", Request.HttpMethod, Request.Url.AbsoluteUri, ex);
				if (Request.IsLocal) {
					Response.StatusCode = 500;
					Response.Write ("<pre>");
					Response.Write (HttpUtility.HtmlEncode (ex.ToString ()));
					Response.Write ("</pre>");
				} else {
					ErrorPage.transferToError (Server, Context, "Internal Server Error",
						HttpUtility.HtmlEncode (realException.GetType ().Name) + ": " + HttpUtility.HtmlEncode (realException.Message),
						500);
				}
			}
		}

		protected void Session_End (object sender, EventArgs e)
		{

		}

		protected void Application_End (object sender, EventArgs e)
		{

		}
	}
}