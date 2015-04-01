/*
 * Users.aspx.cs
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

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class Users : System.Web.UI.Page
{
	GetUsersResponse response;

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		string action = Request ["action"];
		int id;

		if (!string.IsNullOrEmpty (action)) {
			switch (action) {
			case "delete":
				if (int.TryParse (Request ["id"], out id)) {
					WebServiceResponse rsp = Utils.LocalWebService.DeleteUser (Master.WebServiceLogin, id);
					if (rsp.Exception != null) {
						lblMessage.Text = Utils.FormatException (response.Exception.Message);
					} else {
						Response.Redirect ("Users.aspx", false);
						return;
					}
				} else {
					lblMessage.Text = "Invalid id";
				}
				break;
			}
		}

		response = Utils.LocalWebService.GetUsers (Master.WebServiceLogin);

		if (response.Exception != null) {
			lblMessage.Text = Utils.FormatException (response.Exception.Message);
		} else if (response.Users != null) {
			foreach (DBPerson person in response.Users) {
				tblUsers.Rows.Add (Utils.CreateTableRow (
					string.Format ("<a href='User.aspx?username={0}'>{0}</a>", HttpUtility.HtmlEncode (person.login)),
					HttpUtility.HtmlEncode (person.fullname),
					HttpUtility.HtmlEncode (person.roles),
					HttpUtility.HtmlEncode (person.password),
					string.Format ("<a href='Users.aspx?id={0}&amp;action=delete'>Delete</a>", person.id)));
			}
		}
	}
}
