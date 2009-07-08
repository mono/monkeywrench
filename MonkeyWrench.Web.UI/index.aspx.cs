/*
 * index.aspx.cs
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
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class index : System.Web.UI.Page
{
	int limit = 10;

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected override void OnLoad (EventArgs e)
	{
		base.OnLoad (e);

		try {
			FrontPageResponse data;

			if (string.IsNullOrEmpty (Request ["limit"])) {
				if (Request.Cookies ["limit"] != null)
					int.TryParse (Request.Cookies ["limit"].Value, out limit);
			} else {
				int.TryParse (Request ["limit"], out limit);
			}
			if (limit <= 0)
				limit = 10;
			Response.Cookies.Add (new HttpCookie ("limit", limit.ToString ()));

			data = Master.WebService.GetFrontPageData (Master.WebServiceLogin, limit, Request ["lane"], Utils.TryParseInt32 (Request ["lane_id"]));

			this.buildtable.InnerHtml = GenerateOverview (data);

			if (Utils.IsInRole (MonkeyWrench.DataClasses.Logic.Roles.Administrator)) {
				this.adminlinksheader.InnerHtml = "Admin";
				this.adminlinks.InnerHtml = "<a href='index.aspx?action=updatestate'>Update states</a>";
			}
		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}

	private void WriteLanes (List<StringBuilder> header_rows, LaneTreeNode node, int level, int depth)
	{
		if (header_rows.Count <= level)
			header_rows.Add (new StringBuilder ());

		foreach (LaneTreeNode n in node.Children) {
			header_rows [level].AppendFormat ("<td colspan='{0}'>{1}</td>", n.Leafs == 0 ? 1 : n.Leafs, n.Lane.lane);

			WriteLanes (header_rows, n, level + 1, depth);
		}

		if (node.Children.Count == 0) {
			for (int i = level; i < depth; i++) {
				if (header_rows.Count <= i)
					header_rows.Add (new StringBuilder ());
				header_rows [i].Append ("<td colspan='1'>-</td>");
			}
		}
	}

	private void WriteHostLanes (StringBuilder matrix, LaneTreeNode node, IEnumerable<DBHost> hosts, List<int> hostlane_order)
	{
		node.ForEach (new Action<LaneTreeNode> (delegate (LaneTreeNode target)
		{
			if (target.Children.Count != 0)
				return;

			if (target.HostLanes.Count == 0) {
				matrix.Append ("<td>-</td>");
			} else {
				foreach (DBHostLane hl in target.HostLanes) {
					hostlane_order.Add (hl.id);
					matrix.AppendFormat ("<td><a href='ViewTable.aspx?lane_id={1}&host_id={2}'>{0}</a></td>", Utils.FindHost (hosts, hl.host_id).host, hl.lane_id, hl.host_id);
				}
			}
		}));
	}

	private LaneTreeNode BuildTree (FrontPageResponse data)
	{
		LaneTreeNode result = LaneTreeNode.BuildTree (data.Lanes, data.HostLanes);
		if (data.Lane != null)
			result = result.Find (v => v.Lane != null && v.Lane.id == data.Lane.id);
		return result;
	}

	public string GenerateOverview (FrontPageResponse data)
	{
		StringBuilder matrix = new StringBuilder ();
		StringBuilder lane_row = new StringBuilder ();
		StringBuilder host_row = new StringBuilder ();
		List<StringBuilder> rows = new List<StringBuilder> ();
		Dictionary<long, int> col_indices = new Dictionary<long, int> ();
		LaneTreeNode tree = BuildTree (data);
		List<StringBuilder> header_rows = new List<StringBuilder> ();
		List<int> hostlane_order = new List<int> ();

		WriteLanes (header_rows, tree, 0, tree.Depth);

		matrix.AppendLine ("<table class='buildstatus'>");
		for (int i = 0; i < header_rows.Count; i++) {
			matrix.Append ("<tr>");
			matrix.Append (header_rows [i]);
			matrix.AppendLine ("</tr>");
		}

		matrix.AppendLine ("<tr>");
		WriteHostLanes (matrix, tree, data.Hosts, hostlane_order);
		matrix.AppendLine ("</tr>");

		int counter = 0;
		int added = 0;
		StringBuilder row = new StringBuilder ();
		do {
			added = 0;
			row.Length = 0;

			for (int i = 0; i < hostlane_order.Count; i++) {
				int hl_id = hostlane_order [i];

				List<DBRevisionWorkView2> rev = null;
				DBRevisionWorkView2 work = null;

				for (int k = 0; k < data.RevisionWorkHostLaneRelation.Count; k++) {
					if (data.RevisionWorkHostLaneRelation [k] == hl_id) {
						rev = data.RevisionWorkViews [k];
						break;
					}
				}

				if (rev != null && rev.Count > counter) {
					work = rev [counter];
					added++;
				}

				if (work != null) {
					string revision = work.revision;
					int lane_id = work.lane_id;
					int host_id = work.host_id;
					int revision_id = work.revision_id;
					DBState state = work.State;
					bool completed = work.completed;
					string state_str = state.ToString ().ToLowerInvariant ();
					bool is_working;

					switch (state) {
					case DBState.Executing:
						is_working = true;
						break;
					case DBState.NotDone:
					case DBState.Paused:
					case DBState.DependencyNotFulfilled:
						is_working = false;
						break;
					default:
						is_working = !completed;
						break;
					}

					if (is_working) {
						row.AppendFormat (
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
						row.AppendFormat ("<td class='{1}'><a href='ViewLane.aspx?lane_id={2}&amp;host_id={3}&amp;revision_id={4}' title='{5}'>{0}</a></td>",
							revision, state_str, lane_id, host_id, revision_id, "");
					}
				} else {
					row.Append ("<td>-</td>");
				}
			}

			if (added > 0) {
				matrix.Append ("<tr>");
				matrix.Append (row.ToString ());
				matrix.Append ("</tr>");
			}

			counter++;
		} while (counter <= limit && added > 0);

		matrix.AppendLine ("</table>");

		return matrix.ToString ();
		/*
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
					lane_row.AppendFormat ("\t<th class='{2}'><a href='ViewTable.aspx?lane={0}&amp;host={1}'>{0}</a></th>", lane, host, enabled ? "enabled" : "disabled");

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

					if (!completed && state != DBState.Executing && state != DBState.NotDone && state != DBState.Paused && state != DBState.DependencyNotFulfilled) {
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
		 * */
	}
}
