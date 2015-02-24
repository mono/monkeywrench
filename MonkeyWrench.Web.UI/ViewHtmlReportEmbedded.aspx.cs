/*
 * ViewHtmlReport.aspx.cs
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
using System.Web;
using System.Web.UI;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class ViewHtmlReportEmbedded : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		GetViewLaneDataResponse response;

		response = Master.WebService.GetViewLaneData2 (Master.WebServiceLogin,
			Utils.TryParseInt32 (Request ["lane_id"]), Request ["lane"],
			Utils.TryParseInt32 (Request ["host_id"]), Request ["host"],
			Utils.TryParseInt32 (Request ["revision_id"]), Request ["revision"], false);

		DBHost host = response.Host;
		DBLane lane = response.Lane;
		DBRevision revision = response.Revision;

		if (lane == null || host == null || revision == null) {
			Response.Redirect ("index.aspx", false);
			return;
		}

		header.InnerHtml = ViewLane.GenerateHeader (response, lane, host, revision, "Html report for");
		htmlreport.Attributes ["src"] = Request.Url.ToString ().Replace ("Embedded", "");
		htmlreport.Attributes ["onload"] = "javascript: resizeToFillIFrame (document.getElementById ('" + htmlreport.ClientID + "'));";
		ClientScript.RegisterStartupScript (GetType (), "resizeIFrame", "<script type='text/javascript'>resizeToFillIFrame (document.getElementById ('" + htmlreport.ClientID + "'));</script>");
	}
}
