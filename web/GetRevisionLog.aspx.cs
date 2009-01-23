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
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Builder;

public partial class GetRevisionLog : System.Web.UI.Page
{
	DBLoginView login;

	protected void Page_Load (object sender, EventArgs e) {

		int id;
		DBRevision rev;

		Response.ContentType = "text/plain";

		if (!int.TryParse (Request ["id"], out id)) {
			Response.Write (string.Format ("Invalid id: '{0}'\n", Request ["id"]));
			return;
		} 

		using (DB db = new DB (true)) {
			login = Authentication.GetLogin (db, Request, Response);
			rev = new DBRevision (db, id);
			if (!string.IsNullOrEmpty (rev.log)) {
				Response.Write (rev.log);
			} else {
				Response.Write (string.Format ("No log yet."));
			}
			Response.Write ('\n');
			if (!string.IsNullOrEmpty (rev.diff)) {
				Response.Write (rev.diff);
			} else {
				Response.Write (string.Format ("No diff yet."));
			}
			Response.Write ('\n');
		}
	}
}
