/*
 * User.aspx.cs
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
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class User : System.Web.UI.Page
{
	GetUserResponse response;

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	private string GetSelfLink ()
	{
		if (!string.IsNullOrEmpty (Request ["username"])) {
			return "User.aspx?username=" + HttpUtility.UrlEncode (Request ["username"]);
		} else {
			return "User.aspx?id=" + HttpUtility.UrlEncode (Request ["id"]);
		}
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		try {
			int? id = null;
			string username;

			if (!string.IsNullOrEmpty (Request ["id"])) {
				int i;
				if (int.TryParse (Request ["id"], out i)) {
					id = i;
				}
			}

			username = Request ["username"];

			rowRoles.Visible = Authentication.IsInCookieRole (Request, Roles.Administrator);
			if (!string.IsNullOrEmpty (username) || id.HasValue) {
				response = Master.WebService.GetUser (Master.WebServiceLogin, id, username);

				if (response.Exception == null) {
					if (!IsPostBack) {
						txtFullName.Text = response.User.fullname;
						txtUserName.Text = response.User.login;
						txtPassword.Text = response.User.password;
						txtRoles.Text = response.User.roles;

						txtUserName.Attributes ["readonly"] = "readonly"; // asp.net sets readonly="ReadOnly", which fails w3 validation since casing isn't right
					}

				} else {
					lblMessage.Text = response.Exception.Message;
				}
			} else {
				cmdSave.Text = "Create new user";
			}

		} catch (Exception ex) {
			lblMessage.Text = ex.Message.Replace ("\n", "<br />");
		}
	}

	protected void cmdSave_OnClick (object sender, EventArgs e)
	{
		try {
			WebServiceResponse rsp;
			DBPerson user;
			bool created = false;

			if (response == null) {
				user = new DBPerson ();
				user.login = txtUserName.Text;
				created = true;
			} else {
				user = response.User;
			}
			user.fullname = txtFullName.Text;
			user.password = txtPassword.Text;
			user.roles = txtRoles.Text;
			rsp = Master.WebService.EditUser (Master.WebServiceLogin, user);
			if (rsp.Exception != null) {
				lblMessage.Text = rsp.Exception.Message;
			} else {
				if (!Authentication.IsLoggedIn (rsp) && created) {
					Authentication.Login (user.login, user.password, Request, Response);
				}
				Response.Redirect ("User.aspx?username=" + HttpUtility.UrlEncode (user.login), false);
			}
		} catch (Exception ex) {
			lblMessage.Text = ex.ToString ().Replace ("\n", "<br />");
		}
	}
}
