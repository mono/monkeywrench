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

public partial class EditHosts : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		lblMessage.Text = "";
		lblMessage.Visible = false;

		if (Master.Login == null) {
			Response.Redirect ("index.aspx");
			return;
		}

		if (!IsPostBack) {
			string action = Request ["action"];
			int host_id;

			DB db = Master.DB;

			if (!string.IsNullOrEmpty (action)) {
				switch (action) {
				case "remove":
					if (!int.TryParse (Request ["host_id"], out host_id))
						break;
					DBHost.Delete (db, host_id, DBHost.TableName);
					Response.Redirect ("EditHosts.aspx");
					return;
				case "add":
					string host = Request ["host"];
					bool valid;
					if (string.IsNullOrEmpty (host)) {
						valid = false;
						lblMessage.Text = "You have to provide a name for your host.";
					} else {
						valid = true;
						for (int i = 0; i < host.Length; i++) {
							if (char.IsLetterOrDigit (host [i])) {
								continue;
							} else if (host [i] == '-' || host [i] == '_') {
								continue;
							} else {
								lblMessage.Text = string.Format ("The character '{0}' isn't valid.", host [i]);
								valid = false;
								break;
							}
						}
						if (valid && db.LookupHost (host, false) != null) {
							lblMessage.Text = string.Format ("The host '{0}' already exists.", host);
							valid = false;
						}
					}
					if (valid) {
						DBHost dbhost = new DBHost ();
						dbhost.host = host;
						dbhost.Save (db);
						Response.Redirect (string.Format ("EditHost.aspx?host_id={0}", dbhost.id));
					} else {
						// Response.Redirect ("EditHosts.aspx");
						break;
					}
					return;
				default:
					// do nothing
					break;
				}
			}

			TableHeaderRow header = new TableHeaderRow ();
			TableHeaderCell cell = new TableHeaderCell ();
			TableRow row;
			cell.Text = "Hosts";
			cell.ColumnSpan = 4;
			header.Cells.Add (cell);
			tblHosts.Rows.Add (header);
			foreach (DBHost host in db.GetHosts ()) {
				row = new TableRow ();
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='EditHost.aspx?host_id={0}'>{1}</a>", host.id, host.host)));
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='EditHosts.aspx?host_id={0}&amp;action=remove'>Delete</a>", host.id)));
				row.Cells.Add (Utils.CreateTableCell (host.description));
				row.Cells.Add (Utils.CreateTableCell (host.architecture));
				tblHosts.Rows.Add (row);
			}
			row = new TableRow ();
			row.Cells.Add (Utils.CreateTableCell ("<input type='text' value='host' id='txtHost'></input>"));
			row.Cells.Add (Utils.CreateTableCell ("<a href='javascript:addHost ()'>Add</a>"));
			row.Cells.Add (Utils.CreateTableCell (""));
			row.Cells.Add (Utils.CreateTableCell (""));
			tblHosts.Rows.Add (row);
		}

		if (lblMessage.Text != string.Empty)
			lblMessage.Visible = true;

	}
}
