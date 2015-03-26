/*
 * Admin.aspx.cs
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

public partial class Admin : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		GetAdminInfoResponse response;
		string action = Request ["action"];

		if (!string.IsNullOrEmpty (action)) {
			switch (action) {
			case "schedule":
				Utils.WebService.ExecuteScheduler (Master.WebServiceLogin, true);
				Response.Redirect (Request.UrlReferrer == null ? "index.aspx" : Request.UrlReferrer.ToString (), false);
				return;
			}
		}

		response = Utils.LocalWebService.GetAdminInfo (Master.WebServiceLogin);

		lblDeletionDirectiveStatus.Text = response.IsDeletionDirectivesExecuting ? "Running" : "Not running";
		lblSchedulerStatus.Text = response.IsSchedulerExecuting ? "Running" : "Not running";
	}

	protected void cmdSchedule_Click (object sender, EventArgs e)
	{
		cmdSchedule.Enabled = false;
		Utils.WebService.ExecuteScheduler (Master.WebServiceLogin, true);
		lblSchedule.Text = "Scheduler started. It's run asynchronously, so no updates will be shown here.";
		lblSchedulerStatus.Text = "Running";
	}

	protected void cmdExecuteDeletionDirectives_Click (object sender, EventArgs e)
	{
		cmdExecuteDeletionDirectives.Enabled = false;
		Utils.LocalWebService.ExecuteDeletionDirectives (Master.WebServiceLogin);
		lblExecuteDeletionDirectives.Text = "Retention directives executed. They are run asynchronously, so no updates will be shown here.";
		lblDeletionDirectiveStatus.Text = "Running";
	}
}
