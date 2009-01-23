/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Builder;

public partial class Login : System.Web.UI.Page
{
	protected void Page_Load (object sender, EventArgs e)
	{
		string action = Request ["action"];

		if (!this.IsPostBack && Request.UrlReferrer != null)
			txtReferrer.Value = Request.UrlReferrer.AbsoluteUri;

		if (!string.IsNullOrEmpty (action) && action == "logout") {
			Authentication.DeletePassword (Request, Response);
			Response.Redirect (txtReferrer.Value, false);
			return;
		}
	}

	protected void cmdLogin_Click (object sender, EventArgs e)
	{
		DBLogin login;

		using (DB db = new DB (true)) {
			login = DBLogin.Login (db, txtUser.Text, txtPassword.Text, Request.UserHostAddress);
			if (login == null) {
				lblMessage.Text = "Invalid user/password.";
				txtPassword.Text = "";
			} else {
				Authentication.SavePassword (Response, login, txtUser.Text);
				if (string.IsNullOrEmpty (txtReferrer.Value)) {
					Response.Redirect ("index.aspx", false);
				}  else {
					Response.Redirect (txtReferrer.Value, false);
				}
			}
		}
	}
}
