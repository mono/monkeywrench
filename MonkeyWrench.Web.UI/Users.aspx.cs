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
		response = Master.WebService.GetUsers (Master.WebServiceLogin);

		tblUsers.Rows.Add (Utils.CreateTableHeaderRow ("User", "FullName"));
		foreach (DBPerson person in response.Users) {
			tblUsers.Rows.Add (Utils.CreateTableRow (person.login, person.fullname));
		}
	}
}
