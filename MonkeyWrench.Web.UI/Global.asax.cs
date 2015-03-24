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

using MonkeyWrench.WebServices;

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
		}

		protected void Application_Error (object sender, EventArgs e)
		{
			Response.Clear ();
			Exception ex = Server.GetLastError ();

			// Unwrap HttpUnhandledException
			Exception realException;
			if (ex is HttpUnhandledException)
				realException = ex.InnerException;
			else
				realException = ex;

			if (ex is HttpException && (ex as HttpException).GetHttpCode() == 404) {
				// Page not found.
				Response.StatusCode = 404;
				Response.Write (String.Format(@"
					<!DOCTYPE html>
					<html>
					<head>
						<title>Not Found</title>
					</head>
					<body>
					<h1>Page not found.</h1>
					<p>{0}</p>
					</body>
					</html>
				", HttpUtility.HtmlEncode(ex.Message)));
			} else if (realException is UnauthorizedException) {
				// User is not authorized to view this page.
				Response.StatusCode = 403;
				Response.Write (String.Format (@"
					<!DOCTYPE html>
					<html>
					<head>
						<title>Unauthorized</title>
					</head>
					<body>
					<h1>Unauthorized.</h1>
					<p>{0}</p>
					</body>
					</html>
				", HttpUtility.HtmlEncode (realException.Message)));
			} else {
				// Unhandled error. Log it and display an error page. 
				Logger.Log ("{0}: {1}", Request.Url.AbsoluteUri, ex);

				Response.StatusCode = 500;

				if (Request.IsLocal) {
					Response.Write ("<pre>");
					Response.Write (HttpUtility.HtmlEncode (ex.ToString ()));
					Response.Write ("</pre>");
				} else {
					Response.Write (String.Format (@"
						<!DOCTYPE html>
						<html>
						<head>
							<title>500 - Internal Server Error</title>
						</head>
						<body>
						<h1>Wrench encountered an error.</h1>
						<p>We're sorry about that. The error has been logged, and will hopefully be fixed soon!</p>
						<p>Error summary: <samp>{0}: {1}</samp></p>
						</body>
						</html>
					", HttpUtility.HtmlEncode (realException.GetType ().Name), HttpUtility.HtmlEncode (realException.Message)));
				}
			}

			Server.ClearError ();
		}

		protected void Session_End (object sender, EventArgs e)
		{

		}

		protected void Application_End (object sender, EventArgs e)
		{

		}

		public static void SaveInSession(string key, object value)
		{
			HttpSessionStateWrapper a = new HttpSessionStateWrapper(HttpContext.Current.Session);
			a.Add(key, value);
		}

		public static T ReadFromSession<T>(string key) where T : class
		{
			return HttpContext.Current.Session[key] as T;
		}

		public static void ClearFromSession(string key)
		{
			HttpSessionStateWrapper a = new HttpSessionStateWrapper(HttpContext.Current.Session);
			a.Remove(key);
		}
	}
}