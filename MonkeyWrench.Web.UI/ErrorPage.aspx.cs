using System;
using System.Web;
using System.Web.UI;

namespace MonkeyWrench.Web.UI
{
	public partial class ErrorPage : System.Web.UI.Page
	{

		/**
		 * Transfers control to the Error page using Server.Transfer, and displays some error information.
		 * 
		 * header: The error page header, ex. "Page not found". HTML in the string is not escaped.
		 * description: A description of the error, ex. "Path /foo/bar.txt not found". HTML in the string is not escaped.
		 * code: The response code of the page, ex. 404.
		 */
		public static void transferToError(HttpServerUtility server, HttpContext context, string header, string description, int code) {
			context.Items.Clear ();
			context.Items ["ErrorPage_title"] = header;
			context.Items ["ErrorPage_desc"] = description;
			context.Items ["ErrorPage_code"] = code;
			server.Transfer ("~/ErrorPage.aspx", false);
		}

		protected void Page_Load (object sender, EventArgs e) {
			if (!Context.Items.Contains ("ErrorPage_title")) {
				// User probably went to ErrorPage.aspx manually,
				// give them some bogus info
				Context.Items ["ErrorPage_title"] = "Page not found.";
				Context.Items ["ErrorPage_desc"] = "The page ErrorPage.aspx is not accessible.";
				Context.Items ["ErrorPage_code"] = 404;
			}

			errorHeader.Text = (string) Context.Items ["ErrorPage_title"];
			errorDescription.Text = (string) Context.Items ["ErrorPage_desc"];
			Response.StatusCode = (int) Context.Items ["ErrorPage_code"];
		}
	}
}

