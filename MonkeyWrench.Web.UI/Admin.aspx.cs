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
		try {
			GetAdminInfoResponse response;

			response = Master.WebService.GetAdminInfo (Master.WebServiceLogin);

			lblDeletionDirectiveStatus.Text = response.IsDeletionDirectivesExecuting ? "Running" : "Not running";
			lblSchedulerStatus.Text = response.IsSchedulerExecuting ? "Running" : "Not running";
		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}

	protected void cmdSchedule_Click (object sender, EventArgs e)
	{
		try {
			cmdSchedule.Enabled = false;
			Master.WebService.ExecuteScheduler (Master.WebServiceLogin, true);
			lblSchedule.Text = "Scheduler started. It's run asynchronously, so no updates will be shown here.";
			lblSchedulerStatus.Text = "Running";
		} catch (Exception ex) {
			lblSchedule.Text = ex.Message;
		}
	}

	protected void cmdExecuteDeletionDirectives_Click (object sender, EventArgs e)
	{
		try {
			cmdExecuteDeletionDirectives.Enabled = false;
			Master.WebService.ExecuteDeletionDirectives (Master.WebServiceLogin);
			lblExecuteDeletionDirectives.Text = "Retention directives executed. They are run asynchronously, so no updates will be shown here.";
			lblDeletionDirectiveStatus.Text = "Running";
		} catch (Exception ex) {
			lblExecuteDeletionDirectives.Text = ex.Message;
		}
	}
}
