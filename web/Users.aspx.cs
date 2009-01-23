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

public partial class Users : System.Web.UI.Page
{
	protected void Page_Load (object sender, EventArgs e)
	{
		using (DB db = new DB (true)) {
			tblUsers.Rows.Add (Utils.CreateTableHeaderRow ("User", "FullName"));
			foreach (DBPerson person in DBPerson.GetAll (db)) {
				tblUsers.Rows.Add (Utils.CreateTableRow (person.login, person.fullname));
			}
		}
	}
}
