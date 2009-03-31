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
using System.Data;
using System.Data.Common;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Builder;

public partial class ViewWorkTable : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected override void OnLoad (EventArgs e)
	{
		base.OnLoad (e);

		try {
			int id;
			DBHost host;
			DBLane lane;
			DBCommand command = null;

			DB db = Master.DB;

			if (int.TryParse (Request ["lane_id"], out id))
				lane = new DBLane (db, id);
			else
				lane = db.LookupLane (Request ["lane"]);

			if (int.TryParse (Request ["host_id"], out id))
				host = new DBHost (db, id);
			else
				host = db.LookupHost (Request ["host"]);

			if (int.TryParse (Request ["command_id"], out id))
				command = new DBCommand (db, id);

			if (lane == null || host == null || command == null)
				Response.Redirect ("index.aspx");

			header.InnerHtml = GenerateHeader (db, lane, host, command);
			buildtable.InnerHtml = GenerateLane (db, lane, host, command);
		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}
	public string GenerateHeader (DB db, DBLane lane, DBHost host, DBCommand command)
	{
		if (Master.Login == null) {
			return string.Format (@"
<h2>Step {4} on lane '{2}' on '{3}' (<a href='ViewTable2.aspx?lane_id={0}&amp;host_id={1}'>table</a>)</h2><br/>", lane.id, host.id, lane.lane, host.host, command.command);
		} else {
			return string.Format (@"
<h2>Step {4} on lane '<a href='EditLane.aspx?lane_id={0}'>{2}</a>' on '<a href='EditHost.aspx?host_id={1}'>{3}</a>' 
(<a href='ViewTable2.aspx?lane_id={0}&amp;host_id={1}'>table</a>)</h2><br/>", lane.id, host.id, lane.lane, host.host, command.command);
		}
	}

	public string GenerateLane (DB db, DBLane lane, DBHost host, DBCommand command)
	{
		StringBuilder matrix = new StringBuilder ();
		List<DBWorkView2> steps = new List<DBWorkView2> () ;
		DateTime beginning = new DateTime (2001, 1, 1, 0, 0, 0);

		using (IDbCommand cmd = db.Connection.CreateCommand ()) {
			cmd.CommandText = @"
SELECT * 
FROM WorkView2
WHERE command_id = @command_id AND masterhost_id = @host_id AND lane_id = @lane_id
ORDER BY revision DESC LIMIT 250;
";
			DB.CreateParameter (cmd, "command_id", command.id);
			DB.CreateParameter (cmd, "host_id", host.id);
			DB.CreateParameter (cmd, "lane_id", lane.id);
			using (IDataReader reader = cmd.ExecuteReader ()) {
				while (reader.Read ())
					steps.Add (new DBWorkView2 (reader));
			}
		}



		matrix.AppendLine ("<table class='buildstatus'>");
		matrix.AppendLine ("<tr>");
		matrix.AppendLine ("\t<th>Revision</th>");
		matrix.AppendLine ("\t<th>Start Time</th>");
		matrix.AppendLine ("\t<th>Duration</th>");;
		matrix.AppendLine ("\t<th>Html report</th>");
		matrix.AppendLine ("\t<th>Summary</th>");
		matrix.AppendLine ("\t<th>Files</th>");
		matrix.AppendLine ("</tr>");


		for (int i = 0; i < steps.Count; i++) {
			DBWorkView2 view = steps [i];
			List<DBWorkFileView> files = DBWork.GetFiles (db, view.id);

			matrix.Append ("<tr>");

			// revision
			string result;
			switch (view.State) {
			case DBState.NotDone:
				result = "queued"; break;
			case DBState.Executing:
				result = "running"; break;
			case DBState.Failed:
				result = view.nonfatal ? "issues" : "failure"; break;
			case DBState.Success:
			case DBState.Aborted:
			case DBState.Timeout:
			case DBState.Paused:
			default:
				result = view.State.ToString ().ToLowerInvariant ();
				break;
			}

			// result

			int file_id = 0;
			foreach (DBWorkFileView file in files) {
				if (!file.filename.StartsWith (view.command))
					continue;
				file_id = file.id;
				break;
			}
			if (file_id == 0) {
				matrix.AppendFormat ("\t<td class='{0}'>{1}</td>", result, view.revision);
			} else {
				matrix.AppendFormat ("\t<td class='{0}'><a href='GetFile.aspx?id={2}'>{1}</a></td>", result, view.revision, file_id);
			}

			if (view.State > DBState.NotDone && view.State != DBState.Paused) {
				matrix.AppendFormat ("<td>{0}</td>", view.starttime.ToString ("yyyy/MM/dd HH:mm:ss UTC"));
			} else {
				matrix.AppendLine ("<td>-</td>");
			}
			// duration
			DateTime starttime = view.starttime.ToLocalTime ();
			DateTime endtime = view.endtime.ToLocalTime ();
			int duration = (int) (endtime - starttime).TotalSeconds;
			matrix.Append ("\t<td>");
			if (view.State >= DBState.Executing && view.State != DBState.Paused) {
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
				matrix.AppendFormat ("<a href='ViewHtmlReport.aspx?workfile_id={0}'>View html report</a>", index_html.id);
			} else {
				matrix.AppendLine ("-");
			}
			matrix.AppendLine ("</td>");

			// summary
			matrix.AppendLine ("<td>");
			matrix.AppendLine (view.summary);
			matrix.AppendLine ("</td>");


			matrix.AppendLine ("</tr>");
		}
		
		matrix.AppendLine ("</table>");
		
		return matrix.ToString ();
	}
}
