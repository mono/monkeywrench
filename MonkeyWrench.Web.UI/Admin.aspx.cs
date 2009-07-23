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

namespace MonkeyWrench.Web.UI
{
	public partial class Admin : System.Web.UI.Page
	{
		private new Master Master
		{
			get { return base.Master as Master; }
		}

		protected void Page_Load (object sender, EventArgs e)
		{
			cmdSchedule.Click += new EventHandler(cmdSchedule_Click);
		}

		private void cmdSchedule_Click (object sender, EventArgs e)
		{
			try {
				cmdSchedule.Enabled = false;
				Master.WebService.ExecuteScheduler (Master.WebServiceLogin, true);
				lblSchedule.Text = "Scheduler started. It's run asynchronously, so no updates will be shown here.";
			} catch  (Exception ex) {
				lblSchedule.Text = ex.Message;
			}
		}
	}
}
