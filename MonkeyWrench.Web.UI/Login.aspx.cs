/*
 * Login.aspx.cs
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
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class Login : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		string action = Request ["action"];

		if (!this.IsPostBack && Request.UrlReferrer != null)
			txtReferrer.Value = Request.UrlReferrer.AbsoluteUri;

		if (!string.IsNullOrEmpty (action) && action == "logout") {
			FormsAuthentication.SignOut ();
			Response.Redirect (txtReferrer.Value, false);
			return;
		}
	}

	protected void cmdLogin_Click (object sender, EventArgs e)
	{
		LoginResponse response;

		Master.ClearLogin ();

		try {
			WebServiceLogin login = new WebServiceLogin ();
			login.User = txtUser.Text;
			login.Password = txtPassword.Text;
			Console.WriteLine ("Trying to log in with {0}/{1}", login.User, login.Password);
			login.Ip4 = Utils.GetExternalIP (Context.Request);
			response = Master.WebService.Login (login);
			if (response == null) {
				lblMessage.Text = "Could not log in.";
				txtPassword.Text = "";
			} else {
				Console.WriteLine ("Login.aspx: Saved cookie!");
				FormsAuthenticationTicket cookie = new FormsAuthenticationTicket ("cookie", true, 60 * 24);
				Response.Cookies.Add (new HttpCookie ("cookie", response.Cookie));
				Response.Cookies ["cookie"].Expires = DateTime.Now.AddDays (1);
				FormsAuthentication.RedirectFromLoginPage (response.User, true);
			}
		} catch (Exception ex) {
			lblMessage.Text = ex.Message;
			txtPassword.Text = "";
		}
	}
}
