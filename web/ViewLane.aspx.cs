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
using System.Xml;
using System.Net;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

using Builder;

public partial class ViewLane : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected override void OnLoad (EventArgs e)
	{
		base.OnLoad (e);

		try {
			string action = Master.Login == null ? null : Request ["action"];
			int id;
			DBHost host;
			DBLane lane;
			DBRevision revision = null;

			DB db = Master.DB;

			if (int.TryParse (Request ["lane_id"], out id))
				lane = new DBLane (db, id);
			else
				lane = db.LookupLane (Request ["lane"]);

			if (int.TryParse (Request ["host_id"], out id))
				host = new DBHost (db, id);
			else
				host = db.LookupHost (Request ["host"]);

			if (int.TryParse (Request ["revision_id"], out id))
				revision = new DBRevision (db, id);

			if (lane == null || host == null || revision == null)
				Response.Redirect ("index.aspx");

			if (!string.IsNullOrEmpty (action)) {
				switch (action) {
				case "clearrevision":
					db.DeleteFiles (host, lane, revision.id);
					db.ClearWork (lane.id, revision.id, host.id);
					break;
				case "deleterevision":
					db.DeleteWork (lane.id, revision.id, host.id);
					break;
				case "clearstep":
					if (int.TryParse (Request ["work_id"], out id))
						DBWork.Clear (db, id);
					break;
				case "abortstep":
					if (int.TryParse (Request ["work_id"], out id))
						DBWork.Abort (db, id);
					break;
				case "pausestep":
					if (int.TryParse (Request ["work_id"], out id))
						DBWork.Pause (db, id);
					break;
				case "resumestep":
					if (int.TryParse (Request ["work_id"], out id))
						DBWork.Resume (db, id);
					break;
				case "updatestate":
					DBRevisionWork revisionwork = DBRevisionWork.Find (db, lane, host, revision);
					if (revisionwork != null)
						revisionwork.UpdateState (db);
					break;
				}

				Response.Redirect (string.Format ("ViewLane.aspx?lane_id={0}&host_id={1}&revision_id={2}", lane.id, host.id, revision.id));
				Page.Visible = false;
				return;
			}

			header.InnerHtml = GenerateHeader (db, lane, host, revision);
			buildtable.InnerHtml = GenerateLane (db, lane, host, revision);
		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}
	public string GenerateHeader (DB db, DBLane lane, DBHost host, DBRevision revision)
	{
		if (Master.Login == null) {
			return string.Format (@"
<h2>Builds for lane '{2}' on '{3}' (<a href='ViewTable.aspx?lane_id={0}&amp;host_id={1}'>table</a>)</h2><br/>", lane.id, host.id, lane.lane, host.host);
		} else {
			return string.Format (@"
<h2>Builds for lane '<a href='EditLane.aspx?lane_id={0}'>{2}</a>' on '<a href='EditHost.aspx?host_id={1}'>{3}</a>' 
(<a href='ViewTable.aspx?lane_id={0}&amp;host_id={1}'>table</a>)</h2><br/>", lane.id, host.id, lane.lane, host.host);
		}
	}

	public string GenerateLane (DB db, DBLane lane, DBHost host, DBRevision revision)
	{
		StringBuilder matrix = new StringBuilder ();
		List<DBRevision> revisions;
		List<DBWorkView2> steps;
		DateTime beginning = new DateTime (2001, 1, 1, 0, 0, 0);

		revisions = new List<DBRevision> ();
		revisions.Add (revision);

		for (int i = 0; i < revisions.Count; i++) {
			DBRevision dbr = revisions [i];
			DBRevisionWork revisionwork = DBRevisionWork.Find (db, lane, host, revision);

			steps = db.GetWork (revisionwork);

			StringBuilder header = new StringBuilder ();
			header.AppendFormat ("Revision: <a href='GetRevisionLog.aspx?id={0}'>{1}</a>", dbr.id, dbr.revision);
			header.AppendFormat (" - Status: {0}", revisionwork.State);
			header.AppendFormat (" - Author: {0}", dbr.author);
			header.AppendFormat (" - Commit date: {0}", dbr.date.ToString ("yyyy/MM/dd HH:mm:ss UTC"));
			if (Master.Login != null) {
				header.AppendFormat (" - <a href='ViewLane.aspx?lane_id={0}&amp;host_id={2}&amp;revision_id={1}&amp;action=clearrevision'>reset work</a>", lane.id, dbr.id, host.id);
				header.AppendFormat (" - <a href='ViewLane.aspx?lane_id={0}&amp;host_id={2}&amp;revision_id={1}&amp;action=deleterevision'>delete work</a>", lane.id, dbr.id, host.id);
				header.AppendFormat (" - <a href='ViewLane.aspx?lane_id={0}&amp;host_id={2}&amp;revision_id={1}&amp;action=updatestate'>update state</a>", lane.id, dbr.id, host.id);
			}
			if (revisionwork.workhost_id.HasValue) {
				string h;
				if (revisionwork.workhost_id.Value == host.id) {
					h = host.host;
				} else {
					DBHost tmp = new DBHost (db, revisionwork.workhost_id.Value);
					h = tmp.host;
				}
				header.AppendFormat (" - Assigned to {0}", h);
			}  else {
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
			matrix.AppendLine ("\t<th>Actions</th>");
			matrix.AppendLine ("\t<th>Html report</th>");
			matrix.AppendLine ("\t<th>Summary</th>");
			matrix.AppendLine ("\t<th>Files</th>");
			matrix.AppendLine ("\t<th>Host</th>");
			matrix.AppendLine ("</tr>");

			bool failed = false;
			for (int s = 0; s < steps.Count; s++) {
				DBWorkView2 step = steps [s];
				bool done = step.State > DBState.Executing;
				DateTime starttime = step.starttime.ToLocalTime ();
				DateTime endtime = step.endtime.ToLocalTime ();
				int duration = (int) (endtime - starttime).TotalSeconds;
				bool nonfatal = step.nonfatal;
				bool alwaysexecute = step.alwaysexecute;
				string command = step.command;
				List<DBWorkFileView> files = DBWork.GetFiles (db, step.id);

				Console.WriteLine ("starttime: {0}, endtime: {1}, db.now: {2}, duration: {3} s", starttime.ToLongTimeString (), endtime.ToLongTimeString (), db.Now.ToLongTimeString (), duration);

				if (step.endtime.Year < DateTime.Now.Year - 1 && step.duration == 0) {// Not ended, endtime defaults to year 2000
					duration = (int) (db.Now - starttime).TotalSeconds;
					Console.WriteLine (" 1>duration: {0}", duration);
				} else if (step.endtime == DateTime.MinValue) {
					duration = step.duration;
				}

				if (step.State == DBState.Failed && !nonfatal)
					failed = true;

				matrix.AppendLine ("<tr>");

				// step
				matrix.AppendFormat ("\t<td><a href='ViewWorkTable.aspx?lane_id={1}&amp;host_id={2}&amp;command_id={3}'>{0}</a></td>", command.Replace (".sh", ""), lane.id, host.id, step.command_id);				

				// result
				string result;
				switch (step.State) {
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
					result = step.State.ToString ().ToLowerInvariant ();
					break;
				}

				// result
				matrix.AppendFormat ("\t<td class='{0}'>{1}</td>", result, result);

				if (step.State > DBState.NotDone && step.State != DBState.Paused) {
					matrix.AppendFormat ("<td>{0}</td>", step.starttime.ToString ("yyyy/MM/dd HH:mm:ss UTC"));
				} else {
					matrix.AppendLine ("<td>-</td>");
				}
				// duration
				matrix.Append ("\t<td>");
				if (step.State >= DBState.Executing && step.State != DBState.Paused) {
					matrix.Append ("[");
					matrix.Append (TimeSpan.FromSeconds (duration).ToString ());
					matrix.Append ("]");
				} else {
					matrix.Append ("-");
				}
				matrix.AppendLine ("</td>");

				// action
				StringBuilder action = new StringBuilder ();
				if (Master.Login != null) {
					if (step.State == DBState.NotDone && !failed) {
						action.AppendFormat ("<a href='ViewLane.aspx?lane_id={0}&amp;host_id={1}&amp;revision_id={3}&amp;action=pausestep&amp;work_id={2}'>pause</a>", lane.id, host.id, step.id, revision.id);
					} else if (step.State == DBState.Paused) {
						action.AppendFormat ("<a href='ViewLane.aspx?lane_id={0}&amp;host_id={1}&amp;revision_id={3}&amp;action=resumestep&amp;work_id={2}'>resume</a>", lane.id, host.id, step.id, revision.id);
					} else if (step.State > DBState.Executing) {
						action.AppendFormat ("<a href='ViewLane.aspx?lane_id={0}&amp;host_id={1}&amp;revision_id={3}&amp;action=clearstep&amp;work_id={2}'>clear</a>", lane.id, host.id, step.id, revision.id);
					} else if (step.State == DBState.Executing) {
						action.AppendFormat ("<a href='ViewLane.aspx?lane_id={0}&amp;host_id={1}&amp;revision_id={3}&amp;action=abortstep&amp;work_id={2}'>abort</a>", lane.id, host.id, step.id, revision.id);
					}
				}

				matrix.AppendFormat (string.Format ("<td>{0}</td>", action.Length == 0 ? "-" : action.ToString ()));

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
					matrix.AppendFormat ("<a href='ViewHtmlReport.aspx?workfile_id={0}'>View html report</a>", index_html.id);
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
		}

		return matrix.ToString ();
	}
}
