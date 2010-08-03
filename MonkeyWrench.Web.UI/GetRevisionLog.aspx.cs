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
using System.Net;
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
		int? revision_id = Utils.TryParseInt32 (Request ["id"]);

		if (revision_id != null) {
			try {
				log.InnerText = WebServices.DownloadString (WebServices.CreateWebServiceDownloadRevisionUrl (revision_id.Value, false, Master.WebServiceLogin));
			} catch (Exception ex) {
				log.InnerText = "Exception while fetching log: " + ex.ToString ();
			}
			try {
				diff.InnerText = WebServices.DownloadString (WebServices.CreateWebServiceDownloadRevisionUrl (revision_id.Value, true, Master.WebServiceLogin));
			} catch (Exception ex) {
				diff.InnerText = "Exception while fetching diff: " + ex.ToString ();
			}
		}
	}
}
