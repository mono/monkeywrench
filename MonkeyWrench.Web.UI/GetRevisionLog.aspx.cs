/*
 * GetRevisionLog.aspx.cs
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
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class GetRevisionLog : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{

		FindRevisionResponse response;

		DBRevision rev;

		response = Master.WebService.FindRevision (Master.WebServiceLogin, Utils.TryParseInt32 (Request ["id"]), null);

		rev = response.Revision;

		StringBuilder s = new StringBuilder ();
		if (rev != null) {
			if (!string.IsNullOrEmpty (rev.log)) {
				s.Append (rev.log);
			} else {
				s.Append ("No log yet.");
			}
			s.Append ('\n');
			if (!string.IsNullOrEmpty (rev.diff)) {
				s.Append (rev.diff);
			} else {
				s.Append ("No diff yet.");
			}
			s.Append ('\n');
		} else {
			s.Append ("Revision not found.");
		}
		log.InnerText = s.ToString ();
	}
}
