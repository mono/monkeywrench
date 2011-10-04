/*
 * ViewLane.aspx.cs
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
using System.Xml;
using System.Net;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class ViewLane : System.Web.UI.Page
{
	GetViewLaneDataResponse response;

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected override void OnLoad (EventArgs e)
	{
		base.OnLoad (e);

		try {
			string action = null;

			int id;

			response = Master.WebService.GetViewLaneData2 (Master.WebServiceLogin,
				Utils.TryParseInt32 (Request ["lane_id"]), Request ["lane"],
				Utils.TryParseInt32 (Request ["host_id"]), Request ["host"],
				Utils.TryParseInt32 (Request ["revision_id"]), Request ["revision"], false);

			if (Authentication.IsInRole (response, MonkeyWrench.DataClasses.Logic.Roles.Administrator))
				action = Request ["action"];


			DBHost host = response.Host;
			DBLane lane = response.Lane;
			DBRevision revision = response.Revision;

			if (lane == null || host == null || revision == null) {
				Response.Redirect ("index.aspx", false);
				return;
			}

			if (!string.IsNullOrEmpty (action)) {
				switch (action) {
				case "clearrevision":
					Master.WebService.ClearRevision (Master.WebServiceLogin, lane.id, host.id, revision.id);
					break;
				case "deleterevision":
					Master.WebService.RescheduleRevision (Master.WebServiceLogin, lane.id, host.id, revision.id);
					break;
				case "abortrevision":
					Master.WebService.AbortRevision (Master.WebServiceLogin, lane.id, host.id, revision.id);
					break;
				case "clearstep":
					if (int.TryParse (Request ["work_id"], out id))
						Master.WebService.ClearWork (Master.WebServiceLogin, id);
					break;
				case "abortstep":
					if (int.TryParse (Request ["work_id"], out id))
						Master.WebService.AbortWork (Master.WebServiceLogin, id);
					break;
				case "pausestep":
					if (int.TryParse (Request ["work_id"], out id))
						Master.WebService.PauseWork (Master.WebServiceLogin, id);
					break;
				case "resumestep":
					if (int.TryParse (Request ["work_id"], out id))
						Master.WebService.ResumeWork (Master.WebServiceLogin, id);
					break;
				}

				Response.Redirect (string.Format ("ViewLane.aspx?lane_id={0}&host_id={1}&revision_id={2}", lane.id, host.id, revision.id), false);
				Page.Visible = false;
				return;
			}

			header.InnerHtml = GenerateHeader (response, lane, host, revision, "Build of");
			buildtable.InnerHtml = GenerateLane (response);
		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}

	public static string GenerateHeader (GetViewLaneDataResponse response, DBLane lane, DBHost host, DBRevision revision, string description)
	{
		if (!Authentication.IsInRole (response, MonkeyWrench.DataClasses.Logic.Roles.Administrator)) {
			return string.Format (@"
<h2>{4} revision <a href='ViewLane.aspx?lane_id={0}&host_id={1}&revision_id={6}'>{5}</a> on lane '{2}' on '<a href='ViewHostHistory.aspx?host_id={1}'>{3}</a>' 
(<a href='ViewTable.aspx?lane_id={0}&amp;host_id={1}'>table</a>)</h2><br/>", lane.id, host.id, lane.lane, host.host, description, revision.revision, revision.id);
		} else {
			return string.Format (@"
<h2>{4} revision <a href='ViewLane.aspx?lane_id={0}&host_id={1}&revision_id={6}'>{5}</a> on lane '<a href='EditLane.aspx?lane_id={0}'>{2}</a>' on '<a href='ViewHostHistory.aspx?host_id={1}'>{3}</a>' 
(<a href='ViewTable.aspx?lane_id={0}&amp;host_id={1}'>table</a>)</h2><br/>", lane.id, host.id, lane.lane, host.host, description, revision.revision, revision.id);
		}
	}

	public string GenerateLane (GetViewLaneDataResponse response)
	{
		StringBuilder matrix = new StringBuilder ();
		List<DBWorkView2> steps = response.WorkViews;
		DBRevision dbr = response.Revision;
		DBRevisionWork revisionwork = response.RevisionWork;
		DBLane lane = response.Lane;
		DBHost host = response.Host;
		DBRevision revision = response.Revision;

		StringBuilder header = new StringBuilder ();
		header.AppendFormat ("Revision: <a href='GetRevisionLog.aspx?id={0}'>{1}</a>", dbr.id, dbr.revision);
		header.AppendFormat (" - Status: {0}", revisionwork.State);
		header.AppendFormat (" - Author: {0}", dbr.author);
		header.AppendFormat (" - Commit date: {0}", dbr.date.ToString ("yyyy/MM/dd HH:mm:ss UTC"));

		if (Authentication.IsInRole (response, MonkeyWrench.DataClasses.Logic.Roles.Administrator)) {
			header.AppendFormat (" - <a href='ViewLane.aspx?lane_id={0}&amp;host_id={2}&amp;revision_id={1}&amp;action=clearrevision'>reset work</a>", lane.id, dbr.id, host.id);
			header.AppendFormat (" - <a href='ViewLane.aspx?lane_id={0}&amp;host_id={2}&amp;revision_id={1}&amp;action=deleterevision'>delete work</a>", lane.id, dbr.id, host.id);
			header.AppendFormat (" - <a href='ViewLane.aspx?lane_id={0}&amp;host_id={2}&amp;revision_id={1}&amp;action=abortrevision'>abort work</a>", lane.id, dbr.id, host.id);
		}

		if (response.WorkHost != null) {
			header.AppendFormat (" - Assigned to <a href='ViewHostHistory.aspx?host_id={1}'>{0}</a>", response.WorkHost.host, response.WorkHost.id);
		} else {
			header.AppendFormat (" - Unassigned.");
		}

		if (!revisionwork.completed && revisionwork.State != DBState.NotDone && revisionwork.State != DBState.Paused) {
			header.Insert (0, "<center><table class='executing'><td>");
			header.Append ("</td></table></center>");
		}

		// matrix.AppendFormat ("<div class='buildstatus {0}'>Status: {0}</div>", revisionwork.State.ToString ().ToLowerInvariant ());

		matrix.AppendLine ("<table class='buildstatus'>");

		matrix.AppendFormat ("<tr class='{0}'>", revisionwork.State.ToString ().ToLowerInvariant ());
		matrix.Append ("<th colspan='9'>");
		matrix.Append (header.ToString ());
		matrix.Append ("</th>");
		matrix.AppendLine ("</tr>");

		matrix.AppendLine ("<tr>");
		matrix.AppendLine ("\t<th>Step</th>");
		matrix.AppendLine ("\t<th>Result</th>");
		matrix.AppendLine ("\t<th>Start Time</th>");
		matrix.AppendLine ("\t<th>Duration</th>");
		matrix.AppendLine ("\t<th>Html report</th>");
		matrix.AppendLine ("\t<th>Summary</th>");
		matrix.AppendLine ("\t<th>Files</th>");
		matrix.AppendLine ("\t<th>Host</th>");
		matrix.AppendLine ("</tr>");

		bool failed = false;
		for (int s = 0; s < steps.Count; s++) {
			DBWorkView2 step = steps [s];
			DBState state = (DBState) step.state;
			DateTime starttime = step.starttime.ToLocalTime ();
			DateTime endtime = step.endtime.ToLocalTime ();
			int duration = (int) (endtime - starttime).TotalSeconds;
			bool nonfatal = step.nonfatal;
			string command = step.command;
			List<DBWorkFileView> files = response.WorkFileViews [s];

			if (step.endtime.Year < DateTime.Now.Year - 1 && step.duration == 0) {// Not ended, endtime defaults to year 2000
				duration = (int) (response.Now - starttime).TotalSeconds;
			} else if (step.endtime == DateTime.MinValue) {
				duration = step.duration;
			}

			if (state == DBState.Failed && !nonfatal)
				failed = true;

			matrix.AppendLine ("<tr>");

			// step
			DBWorkFileView step_log = files.Find ((v) => v.filename == command + ".log");
			matrix.Append ("\t<td>");
			if (step_log != null) {
				matrix.AppendFormat ("<a href='GetFile.aspx?id={0}'>{1}</a> ", step_log.id, Path.GetFileNameWithoutExtension (command));
			} else {
				matrix.Append (Path.GetFileNameWithoutExtension (command));
			}
			matrix.Append ("</td>");

			// result
			string result;
			switch (state) {
			case DBState.NotDone:
				result = failed ? "skipped" : "queued"; break;
			case DBState.Executing:
				result = "running"; break;
			case DBState.Failed:
				result = nonfatal ? "issues" : "failure"; break;
			case DBState.Success:
			case DBState.Aborted:
			case DBState.Timeout:
			case DBState.Paused:
			default:
				result = state.ToString ().ToLowerInvariant ();
				break;
			}

			// result
			matrix.AppendFormat ("\t<td class='{0}'><a href='ViewWorkTable.aspx?lane_id={1}&amp;host_id={2}&amp;command_id={3}'>{0}</a></td>", result, lane.id, host.id, step.command_id);

			if (state > DBState.NotDone && state != DBState.Paused) {
				matrix.AppendFormat ("<td>{0}</td>", step.starttime.ToString ("yyyy/MM/dd HH:mm:ss UTC"));
			} else {
				matrix.AppendLine ("<td>-</td>");
			}
			// duration
			matrix.Append ("\t<td>");
			if (state >= DBState.Executing && state != DBState.Paused) {
				matrix.Append ("[");
				matrix.Append (TimeSpan.FromSeconds (duration).ToString ());
				matrix.Append ("]");
			} else {
				matrix.Append ("-");
			}
			matrix.AppendLine ("</td>");

			// html report
			matrix.AppendLine ("<td>");
			DBWorkFileView index_html = null;

			foreach (DBWorkFileView file in files) {
				if (file.filename == "index.html") {
					index_html = file;
					break;
				}
			}

			if (index_html != null) {
				matrix.AppendFormat ("<a href='ViewHtmlReportEmbedded.aspx?workfile_id={0}&lane_id={1}&host_id={2}&revision_id={3}'>View html report</a>", index_html.id, lane.id, host.id, revision.id);
			} else {
				matrix.AppendLine ("-");
			}
			matrix.AppendLine ("</td>");

			// summary
			matrix.AppendLine ("<td>");
			matrix.AppendLine (step.summary);
			matrix.AppendLine ("</td>");

			// files
			matrix.AppendLine ("<td>");
			bool did_first = false;
			foreach (DBWorkFileView file in files) {
				if (file.hidden)
					continue;
				if (file.hidden)
					continue;
				if (did_first)
					matrix.Append (", ");
				matrix.AppendFormat ("<a href='GetFile.aspx?id={0}'>{1}</a> ", file.id, file.filename);
				did_first = true;
			}
			matrix.AppendLine ("</td>");

			// host
			matrix.AppendFormat ("<td>{0}</td>", step.workhost);

			matrix.AppendLine ("</tr>");
		}
		matrix.AppendLine ("</table>");

		return matrix.ToString ();
	}
}
