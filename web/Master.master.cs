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

public partial class Master : System.Web.UI.MasterPage
{
	private DB db;
	private DBLoginView login;
	private bool tried_login;

	public DB DB
	{
		get
		{
			if (db == null)
				db = new DB (true);
			return db;
		}
	}

	public DBLoginView Login
	{
		get
		{
			if (login == null && !tried_login) {
				tried_login = true;
				login = Authentication.GetLogin (DB, Request, Response);
			}

			return login;
		}
	}

	public override void Dispose ()
	{
		base.Dispose ();

		if (db != null) {
			db.Dispose ();
			db = null;
		}
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		TableRow row = new TableRow ();
		TableCell title = new TableCell ();
		TableCell user = new TableCell ();

		title.Text = "<a href='index.aspx'>Moonbuilder</a>";
		if (Login == null) {
			user.Text = "<a href='Login.aspx'>Login</a>";
		} else {
			user.Text = string.Format ("<a href='User.aspx?id={0}'>{1}</a> <a href='Login.aspx?action=logout'>Log out</a>", login.person_id, login.fullname);
		}
		user.CssClass = "headerlogin";
		row.Cells.Add (title);
		row.Cells.Add (user);

		tableHeader.Rows.Add (row);

		if (Login != null) {
			tableFooter.Rows.Add (Utils.CreateTableRow ("<a href='EditHosts.aspx'>Edit Hosts</a>"));
			tableFooter.Rows.Add (Utils.CreateTableRow ("<a href='EditLanes.aspx'>Edit Lanes</a>"));
		}
		tableFooter.Rows.Add (Utils.CreateTableRow ("<a href='doc/index.html'>Documentation</a>"));
	}
}
