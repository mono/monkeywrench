using System;
using System.Collections;
using System.ComponentModel;
using System.Web;
using System.Web.SessionState;

using MonkeyWrench.Database;

namespace MonkeyWrench.Web.JSON
{
	public class Global : System.Web.HttpApplication
	{
		protected void Application_Start (Object sender, EventArgs e)
		{
			Configuration.LoadConfiguration (new string [] {});
		}

		protected void Session_Start (Object sender, EventArgs e)
		{

		}

		protected void Application_BeginRequest (Object sender, EventArgs e)
		{
			// Check if authorized to use the API.
			using (var db = new DB ()) {
				if (!Utils.isAuthorized (db, Request ["username"], Request ["password"])) {
					Response.StatusCode = 403;
					Response.ContentType = "text/plain";
					Response.Write ("Not authorized.");
					CompleteRequest ();
					return;
				}
			}

			// Allow third party access.
			Response.AddHeader ("Access-Control-Allow-Origin", "*");
		}

		protected void Application_EndRequest (Object sender, EventArgs e)
		{

		}

		protected void Application_AuthenticateRequest (Object sender, EventArgs e)
		{

		}

		protected void Application_Error (Object sender, EventArgs e)
		{
			Response.Clear ();
			Exception ex = Server.GetLastError ();
			Server.ClearError ();

			Exception inner = ex.GetBaseException ();

			Response.ContentType = "text/plain; charset=utf-8";

			if (inner is HttpException && (inner as HttpException).GetHttpCode () == 404) {
				Response.StatusCode = 404;
				Response.Write ("Not found.");
			} else if (inner is UnauthorizedException) {
				Response.StatusCode = 503;
				Response.Write (ex.Message);
			} else {
				Logger.Log ("{0}: {1}", Request.Url.AbsoluteUri, ex);

				Response.StatusCode = 500;
				Response.Write ("Internal server error");
			}
		}

		protected void Session_End (Object sender, EventArgs e)
		{

		}

		protected void Application_End (Object sender, EventArgs e)
		{

		}
	}
}
