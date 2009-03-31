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
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Builder;

public partial class ViewRevisions : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	private DBLoginView Login
	{
		get { return Master.Login; }
	}
	protected override void OnLoad (EventArgs e)
	{
		int id;
		DBHost dbhost;
		DBLane dblane;
		DB db;

		base.OnLoad (e);

		try {
			string lane_id = Request ["lane_id"];
			string host_id = Request ["host_id"];
			bool horizontal;

			if (!bool.TryParse (Request ["horizontal"], out horizontal)) {
				// Not specified. See if it's a cookie
				if (Request.Cookies ["horizontal"] != null)
					bool.TryParse (Request.Cookies ["horizontal"].Value, out horizontal);
			}
			Response.Cookies.Add (new HttpCookie ("horizontal", horizontal.ToString ()));

			db = Master.DB;

			if (int.TryParse (lane_id, out id))
				dblane = new DBLane (db, id);
			else
				dblane = db.LookupLane (Request ["lane"]);

			if (int.TryParse (host_id, out id))
				dbhost = new DBHost (db, id);
			else
				dbhost = db.LookupHost (Request ["host"]);

			if (Login != null && !string.IsNullOrEmpty (Request ["clearrevisions"])) {
				string [] revs = Request ["clearrevisions"].Split (';');
				foreach (string rev in revs) {
					if (!int.TryParse (rev, out id))
						break;
					//db.DeleteFiles (dbhost, dblane, id);
					//db.ClearWork (dblane.id, id, dbhost.id);
				}
				Response.Redirect (string.Format ("ViewTable2.aspx?host_id={0}&lane_id={1}", host_id, lane_id));
				return;
			}

			this.buildtable.InnerHtml = GenerateLaneTable (db, dblane, dbhost, horizontal);
		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}

	public string GenerateLaneTable (DB db, DBLane lane, DBHost host, bool horizontal)
	{
		StringBuilder matrix = new StringBuilder ();
		DateTime beginning = new DateTime (2001, 1, 1, 0, 0, 0);
		List<List<string>> table = new List<List<string>> ();
		List<string> row = new List<string> ();
		List<string> header = new List<string> ();

		using (IDbCommand cmd = db.Connection.CreateCommand ()) {
			cmd.CommandText = @"
SELECT RevisionWork.id, RevisionWork.revision_id, RevisionWork.lane_id, RevisionWork.host_id, RevisionWork.state, Revision.revision, CAST (Revision.revision AS int) AS r
FROM RevisionWork
INNER JOIN Host ON RevisionWork.host_id = Host.id
INNER JOIN Lane ON RevisionWork.lane_id = Lane.id
INNER JOIN Revision ON RevisionWork.revision_id = Revision.id
WHERE RevisionWork.lane_id = @lane_id AND RevisionWork.host_id = @host_id
ORDER BY r DESC
";
			DB.CreateParameter (cmd, "lane_id", lane.id);
			DB.CreateParameter (cmd, "host_id", host.id);

			using (IDataReader reader = cmd.ExecuteReader ()) {
				matrix.AppendLine ("<table class='buildstatus'>");
				while (reader.Read ()) {
					matrix.AppendLine ("<tr>");
					string revision = reader.GetString (reader.GetOrdinal ("revision"));
					DBState state = (DBState) reader.GetInt32 (reader.GetOrdinal ("state"));
					int revisionwork_id = reader.GetInt32 (reader.GetOrdinal ("id"));
					matrix.AppendFormat ("<td>{0}</td> <td>in database: {1}</td> <td>calculated: {2}</td>", revision, state, DBRevisionWork.EnsureState (db, revisionwork_id, state));
					matrix.AppendLine ("</tr>");
				}
				matrix.AppendLine ("</table>");
			}
		}

		return matrix.ToString ();

		/*
		List<DBWorkView> steps;
		DBWork work;
		bool new_revision = true;
		int revision_id = 0;
		int result_index;
		  
		steps = db.GetAllWork (lane, host);

		for (int i = 0; i < steps.Count; i++) {
			while (header.Count <= steps [i].sequence) {
				header.Add (null);
			}
			if (header [steps [i].sequence] != null)
				continue;

			header [steps [i].sequence] = steps [i].command;
		}
		header.RemoveAll (delegate (string match) { return match == null; });
		header.Insert (0, "Revision");
		header.Insert (1, "Author");
		if (Login != null)
			header.Insert (2, "Select");
		result_index = Login != null ? 3 : 2;
		table.Add (header);

		bool failed = false;
		for (int i = 0; i < steps.Count; i++) {
			DBWorkView view = steps [i];

			new_revision = revision_id != view.revision_id;
			revision_id = view.revision_id;

			if (new_revision) {
				if (i > 0) {
					// matrix.AppendLine ("</tr>");
					table.Add (row);
				}
				row = new List<string> ();
				row.Add (string.Format ("<a href='ViewLane.aspx?lane_id={0}&amp;host_id={1}&amp;revision_id={2}' title='{4}'>{3}</a></td>", lane.id, host.id, view.revision_id, view.revision, string.Format ("Author: {1} Build start date: {0}", view.starttime.ToUniversalTime ().ToString ("yyyy/MM/dd HH:mm:ss UTC"), view.author)));
				row.Add (string.Format ("<a href='GetRevisionLog.aspx?id={0}'>{1}</a></td>", view.revision_id, view.author));
				if (Login != null)
					row.Add (string.Format ("<input type=checkbox name='revision_id_{0}' />", view.revision_id));
				while (row.Count < header.Count)
					row.Add ("-");

				failed = false;
			}

			int exitcode = 0;

			if (view.State == DBState.Failed && !view.nonfatal)
				failed = true;

			// result
			string result;
			switch (view.State) {
			case DBState.NotDone:
				result = failed ? "skipped" : "queued"; break;
			case DBState.Executing:
				result = "running"; break;
			case DBState.Failed:
				result = "failure"; break;
			case DBState.Success:
				result = "success"; break;
			case DBState.Aborted:
				result = "aborted"; break;
			case DBState.Timeout:
				result = "timeout"; break;
			case DBState.Paused:
				result = "paused"; break;
			default:
				result = "unknown"; break;
			}

			for (int j = 2; j < header.Count; j++) {
				if (header [j] == view.command) {
					row [j] = result;
					break;
				}
			}
		}

		table.Add (row);

		matrix.AppendLine ("<table class='buildstatus'>");
		if (horizontal) {
			for (int i = 0; i < header.Count; i++) {
				matrix.Append ("<tr>");
				for (int j = 0; j < table.Count; j++) {
					string td = j == 0 ? "th" : "td";
					if (i >= 2 && j > 0) {
						matrix.AppendFormat ("<{0} class='{1}'>", td, table [j] [i]);
					} else {
						matrix.AppendFormat ("<{0}>", td);
					}
					matrix.AppendFormat (table [j] [i]);
					matrix.AppendFormat ("</{0}>", td);
				}
				matrix.AppendLine ("</tr>");
			}
		} else {
			for (int i = 0; i < table.Count; i++) {
				row = table [i];
				matrix.Append ("<tr>");
				for (int j = 0; j < row.Count; j++) {
					string td = j == 0 ? "th" : "td";
					if (j >= result_index && row [j] != "-") {
						matrix.AppendFormat ("<{0} class='{1}'>", td, row [j]);
					} else {
						matrix.AppendFormat ("<{0}>", td);
					}
					matrix.AppendFormat (row [j]);
					matrix.AppendFormat ("</{0}>", td);
				}
				matrix.AppendLine ("</tr>");
			}
		}
		matrix.AppendLine ("</table>");

		return matrix.ToString ();
		*/
	}
}
