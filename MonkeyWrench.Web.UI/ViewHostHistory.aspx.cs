/*
 * ViewHostHistory.aspx.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2010 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class ViewHostHistory : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		GetWorkHostHistoryResponse response;
		int host_id;
		int limit;
		int offset;
		string action = null;
		int lane_id = 0;
		int revision_id = 0;
		int masterhost_id = 0;

		try {
			if (!int.TryParse (Request ["host_id"], out host_id)) {
				// ?
				return;
			}

			int.TryParse (Request ["lane_id"], out lane_id);
			int.TryParse (Request ["revision_id"], out revision_id);
			int.TryParse (Request ["masterhost_id"], out masterhost_id);

			if (Utils.IsInRole (MonkeyWrench.DataClasses.Logic.Roles.Administrator))
				action = Request ["action"];

			if (!string.IsNullOrEmpty (action) && masterhost_id > 0 && lane_id > 0 && revision_id > 0) {
				switch (action) {
				case "clearrevision":
					Master.WebService.ClearRevision (Master.WebServiceLogin, lane_id, masterhost_id, revision_id);
					break;
				case "deleterevision":
					Master.WebService.RescheduleRevision (Master.WebServiceLogin, lane_id, masterhost_id, revision_id);
					break;
				case "abortrevision":
					Master.WebService.AbortRevision (Master.WebServiceLogin, lane_id, masterhost_id, revision_id);
					break;
				}

				Response.Redirect (string.Format ("ViewHostHistory.aspx?host_id={0}", host_id), false);
				Page.Visible = false;
				return;
			}


			if (!int.TryParse (Request.QueryString ["limit"], out limit))
				limit = 25;
			if (!int.TryParse (Request.QueryString ["offset"], out offset))
				offset = 0;

			response = Master.WebService.GetWorkHostHistory (Master.WebServiceLogin, Utils.TryParseInt32 (Request ["host_id"]), Request ["host"], limit, offset);

			string hdr;
			if (Utils.IsInRole (MonkeyWrench.DataClasses.Logic.Roles.Administrator)) {
				hdr = string.Format ("History for <a href='EditHost.aspx?host_id={1}'>{0}</a>", response.Host.host, response.Host.id);
			} else {
				hdr = string.Format ("History for {0}", response.Host.host);
			}
			hostheader.InnerHtml = "<h2>" + hdr + "</h2>";

			StringBuilder table = new StringBuilder ();
			table.AppendLine ("<table class='buildstatus'>");
			table.AppendLine ("<tr><td>Lane</td><td>Host</td><td>Revision</td><td>State</td><td>StartTime</td><td>Completed</td><td>Duration</td><td>Commands</td></tr>");
			for (int i = 0; i < response.RevisionWorks.Count; i++) {
				DBRevisionWork rw = response.RevisionWorks [i];
				string lane = response.Lanes [i];
				string revision = response.Revisions [i];
				string host = response.Hosts [i];
				DateTime starttime = response.StartTime [i].ToLocalTime ();
				int duration = response.Durations [i];
				table.Append ("<tr>");
				table.AppendFormat ("<td><a href='ViewTable.aspx?lane_id={1}&host_id={2}'>{0}</a></td>", lane, rw.lane_id, rw.host_id);
				table.AppendFormat ("<td>{0}</td>", host);
				table.AppendFormat ("<td><a href='ViewLane.aspx?lane_id={0}&host_id={1}&revision_id={2}'>{3}</a></td>", rw.lane_id, rw.host_id, rw.revision_id, revision);
				table.AppendFormat ("<td class='{0}'>{1}</td>", rw.State.ToString ().ToLower (), rw.State.ToString ());
				table.AppendFormat ("<td>{0}</td>", starttime.ToString ("yyyy/MM/dd HH:mm:ss UTC"));
				table.AppendFormat ("<td>{0}</td>", rw.completed ? "Yes" : "No");
				table.AppendFormat ("<td>{0}</td>", TimeSpan.FromSeconds (duration).ToString ());
				table.AppendFormat ("<td>" +
					 "<a href='ViewHostHistory.aspx?lane_id={0}&host_id={1}&revision_id={2}&masterhost_id={3}&action=clearrevision'>Reset work</a> " +
					 "<a href='ViewHostHistory.aspx?lane_id={0}&host_id={1}&revision_id={2}&masterhost_id={3}&action=deleterevision'>Delete work</a> " +
					 "<a href='ViewHostHistory.aspx?lane_id={0}&host_id={1}&revision_id={2}&masterhost_id={3}&action=abortrevision'>Abort work</a> " + 
					 "</td>", rw.lane_id, rw.workhost_id, rw.revision_id, rw.host_id);
				table.AppendLine ("</tr>");
			}
			table.AppendLine ("</table>");
			hosthistory.InnerHtml = table.ToString ();
		} catch (Exception ex) {
			Response.Write (HttpUtility.HtmlEncode (ex.ToString ()).Replace ("\n", "<br/>"));
		}
	}
}
