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

public partial class index : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected override void OnLoad (EventArgs e)
	{
		base.OnLoad (e);

		try {
			if (Master.Login != null) {
				switch (Request ["action"]) {
				case "updatestate":
					DBRevisionWork.UpdateStateAll (Master.DB);
					Response.Redirect ("index2.aspx");
					return;
				default:
					break;
				}
			}

			this.buildtable.InnerHtml = GenerateOverview (Master.DB);
			if (Master.Login != null) {
				this.adminlinksheader.InnerHtml = "Admin";
				this.adminlinks.InnerHtml = "<a href='index2.aspx?action=updatestate'>Update states</a>";
			}
		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}

	public string GenerateOverview (DB db)
	{
		StringBuilder matrix = new StringBuilder ();
		StringBuilder lane_row = new StringBuilder ();
		StringBuilder host_row = new StringBuilder ();
		List<StringBuilder> rows = new List<StringBuilder> ();
		Dictionary<long, int> col_indices = new Dictionary<long, int> ();
		int limit;

		int.TryParse (Request ["limit"], out limit);
		if (limit <= 0)
			limit = 10;

		using (IDbCommand cmd = db.Connection.CreateCommand ()) {
			cmd.CommandText = @"
SELECT HostLane.id, HostLane.enabled, HostLane.host_id, HostLane.lane_id, HostLane.enabled, Lane.lane, Host.host
FROM HostLane
INNER JOIN Lane ON Lane.id = HostLane.lane_id
INNER JOIN Host ON Host.id = HostLane.host_id
WHERE HostLane.enabled = true
ORDER BY host ASC, lane ASC;";

			using (IDataReader reader = cmd.ExecuteReader ()) {
				string prevhost = null;
				int lane_count = 0;
				int col_index = 0;
				while (reader.Read ()) {
					string host = reader.GetString (reader.GetOrdinal ("host"));
					string lane = reader.GetString (reader.GetOrdinal ("lane"));
					int host_id = reader.GetInt32 (reader.GetOrdinal ("host_id"));
					int lane_id = reader.GetInt32 (reader.GetOrdinal ("lane_id"));
					bool enabled = reader.GetBoolean (reader.GetOrdinal ("enabled"));

					if (host == prevhost) {
						lane_count++;
					} else {
						if (prevhost != null) {
							host_row.Replace ("%COLSPAN%", lane_count.ToString ());
						}
						if (Master.Login != null) {
							host_row.AppendFormat ("\t<th colspan='%COLSPAN%'><a href='EditHost.aspx?host_id={1}'>{0}</a></th>", host, host_id);
						} else {
							host_row.AppendFormat ("\t<th colspan='%COLSPAN%'>{0}</th>", host);
						}
						lane_count = 1;
					}
					lane_row.AppendFormat ("\t<th class='{2}'><a href='ViewTable2.aspx?lane={0}&amp;host={1}'>{0}</a></th>", lane, host, enabled ? "enabled" : "disabled");

					prevhost = host;

					col_indices [(((long) host_id) << 32) + lane_id] = col_index++;

				}
				host_row.Replace ("%COLSPAN%", lane_count.ToString ());
			}
		}

		using (IDbCommand cmd = db.Connection.CreateCommand ()) {
			cmd.CommandText = @"
SELECT
	RevisionWork.id, RevisionWork.lane_id, RevisionWork.host_id, RevisionWork.revision_id, 
	RevisionWork.state, RevisionWork.completed, RevisionWork.lock_expires > now () AS lock_expired,
	Revision.revision, Revision.date, Host.host, Lane.lane, HostLane.enabled
FROM RevisionWork
INNER JOIN Revision ON RevisionWork.revision_id = Revision.id
INNER JOIN Host ON RevisionWork.host_id = Host.id
INNER JOIN Lane ON RevisionWork.lane_id = Lane.id
INNER JOIN HostLane ON RevisionWork.host_id = HostLane.host_id AND RevisionWork.lane_id = HostLane.lane_id
WHERE HostLane.enabled = true
ORDER BY 
	Host ASC, Lane ASC, Revision.date DESC
";
			using (IDataReader reader = cmd.ExecuteReader ()) {
				int host_id_prev = 0, lane_id_prev = 0;
				int row_index = 0;
				StringBuilder row_builder;
				List<int> column_count = new List<int> ();
				while (reader.Read ()) {
					int host_id = reader.GetInt32 (reader.GetOrdinal ("host_id"));
					int lane_id = reader.GetInt32 (reader.GetOrdinal ("lane_id"));
					int col_index;

					if (host_id != host_id_prev || lane_id != lane_id_prev) {
						row_index = 0;
					} else if (row_index >= limit + 1) {
						continue;
					} else {
						row_index++;
					}

					if (rows.Count <= row_index) {
						rows.Add (new StringBuilder ());
						column_count.Add (0);
					}
					row_builder = rows [row_index];
					column_count [row_index]++;

					long key = (((long) host_id) << 32) + lane_id;
					if (!col_indices.TryGetValue ((((long) host_id) << 32) + lane_id, out col_index))
						continue;

					for (int i = column_count [row_index]; i <= col_index; i++) {
						row_builder.Append ("<td>?</td>");
						column_count [row_index]++;
					}

					string revision = reader.GetString (reader.GetOrdinal ("revision"));
					int revision_id = reader.GetInt32 (reader.GetOrdinal ("revision_id"));
					DBState state = (DBState) reader.GetInt32 (reader.GetOrdinal ("state"));
					string state_str = state.ToString ().ToLowerInvariant ();
					bool completed = reader.GetBoolean (reader.GetOrdinal ("completed"));

					if (!completed && state != DBState.Executing && state != DBState.NotDone && state != DBState.Paused) {
						row_builder.AppendFormat (
							@"<td class='{1}'>
								<center>
									<table class='executing'>
										<td>
											<a href='ViewLane.aspx?lane_id={2}&amp;host_id={3}&amp;revision_id={4}' title='{5}'>{0}</a>
										</td>
									</table>
								<center>
							  </td>",
							revision, state_str, lane_id, host_id, revision_id, "");
					} else {
						row_builder.AppendFormat ("<td class='{1}'><a href='ViewLane.aspx?lane_id={2}&amp;host_id={3}&amp;revision_id={4}' title='{5}'>{0}</a></td>",
							revision, state_str, lane_id, host_id, revision_id, "");
					}

					host_id_prev = host_id;
					lane_id_prev = lane_id;
				}
			}
		}

		// Generate the output
		matrix.AppendLine ("<table class='buildstatus'>");
		matrix.AppendLine ("<tr>");
		matrix.Append (host_row.ToString ());
		matrix.AppendLine ("</tr>");
		matrix.AppendLine ("<tr>");
		matrix.Append (lane_row.ToString ());
		matrix.AppendLine ("</tr>");

		for (int i = 0; i < rows.Count; i++) {
			matrix.Append ("<tr>");
			matrix.Append (rows [i].ToString ());
			matrix.Append ("</tr>");
		}

		matrix.AppendLine ("</table >");

		return matrix.ToString ();
	}
}
