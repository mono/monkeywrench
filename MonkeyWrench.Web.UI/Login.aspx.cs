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

using MonkeyWrench;
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
			if (Request.Cookies ["cookie"] != null) {
				Master.WebService.Logout (Master.WebServiceLogin);
				Response.Cookies.Add(new HttpCookie ("cookie", ""));
				Response.Cookies ["cookie"].Expires = DateTime.Now.AddYears (-20);
				Response.Cookies.Add (new HttpCookie ("user", ""));
				Response.Cookies ["user"].Expires = DateTime.Now.AddYears (-20);
				Response.Cookies.Add (new HttpCookie ("roles", ""));
				Response.Cookies ["roles"].Expires = DateTime.Now.AddYears (-20);
			}
			Response.Redirect (txtReferrer.Value, false);
			return;
		}
	}

	protected void cmdLogin_Click (object sender, EventArgs e)
	{
		Master.ClearLogin ();

		try {
			if (!Authentication.Login (txtUser.Text, txtPassword.Text, Request, Response)) {
				lblMessage.Text = "Could not log in";
				txtPassword.Text = "";
			} else {
				Response.Redirect (txtReferrer.Value, false);
			}
		} catch (Exception) {
			lblMessage.Text = "Invalid user/password.";
			txtPassword.Text = "";
		}
	}
}
