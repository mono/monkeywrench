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

			string lanes_str = null;
			string lane_ids_str = null;

			string [] lanes = null;
			List<int> lane_ids = null;

			if (!string.IsNullOrEmpty (Request ["show_all"])) {
				// do nothing, default is to show all
			} else {
				HttpCookie cookie;

				lanes_str = Request ["lane"];
				lane_ids_str = Request ["lane_id"];

				if (string.IsNullOrEmpty (lanes_str) && string.IsNullOrEmpty (lane_ids_str)) {
					if ((cookie = Request.Cookies ["index:lane"]) != null) {
						lanes_str = HttpUtility.UrlDecode (cookie.Value);
					}
					if ((cookie = Request.Cookies ["index:lane_id"]) != null) {
						lane_ids_str = HttpUtility.UrlDecode (cookie.Value);
					}
				}

				if (!string.IsNullOrEmpty (lanes_str))
					lanes = lanes_str.Split (';');
				if (!string.IsNullOrEmpty (lane_ids_str)) {
					lane_ids = new List<int> ();
					foreach (string str in lane_ids_str.Split (';')) {
						int? ii = Utils.TryParseInt32 (str);
						if (ii.HasValue)
							lane_ids.Add (ii.Value);
					}
				}

				Response.Cookies.Set (new HttpCookie ("index:lane", HttpUtility.UrlEncode (lanes_str)));
				Response.Cookies.Set (new HttpCookie ("index:lane_id", HttpUtility.UrlEncode (lane_ids_str)));
			}

			data = Master.WebService.GetFrontPageData2 (Master.WebServiceLogin, limit, lanes, lane_ids != null ? lane_ids.ToArray () : null);

			if (data.Exception != null) {
				if (data.Exception.HttpCode == 403) {
					Master.RequestLogin ();
					return;
				}
				lblMessage.Text = data.Exception.Message;
				return;
			}

			this.buildtable.InnerHtml = GenerateOverview (data);
		} catch (Exception ex) {
			lblMessage.Text = Utils.FormatException (ex, true);
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
			for (int hl = 0; hl < node.HostLanes.Count; hl++) {
				for (int i = level; i < depth; i++) {
					if (header_rows.Count <= i)
						header_rows.Add (new StringBuilder ());
					header_rows [i].Append ("<td colspan='1'>-</td>");
				}
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
					matrix.AppendFormat ("<td><a href='ViewTable.aspx?lane_id={1}&amp;host_id={2}' class='{3}'>{0}</a></td>", Utils.FindHost (hosts, hl.host_id).host, hl.lane_id, hl.host_id, hl.enabled ? "enabled-hostlane" : "disabled-hostlane");
				}
			}
		}));
	}

	private LaneTreeNode BuildTree (FrontPageResponse data)
	{
		LaneTreeNode result = LaneTreeNode.BuildTree (data.Lanes, data.HostLanes);
		if (data.Lane != null) {
			result = result.Find (v => v.Lane != null && v.Lane.id == data.Lane.id);
		} else if (data.SelectedLanes.Count > 1) {
			for (int i = result.Children.Count - 1; i >= 0; i--) {
				LaneTreeNode ltn = result.Children [i];
				if (!data.SelectedLanes.Exists ((DBLane l) => l.id == ltn.Lane.id)) {
					result.Children.RemoveAt (i);
				}
			}
		}
		return result;
	}

	public static string TimeDiffToString (DateTime from, DateTime to)
	{
		int value;
		TimeSpan diff = to - from;

		if (from == DateTime.MinValue)
			return "Never";

		if (diff.TotalHours < 1) {
			value = (int) diff.TotalMinutes;
			if (value == 1) {
				return "1 minute ago";
			} else {
				return string.Format ("{0} minutes ago", value);
			}
		} else if (diff.TotalDays < 1) {
			value = (int) diff.TotalHours;
			if (value == 1) {
				return "1 hour ago";
			} else {
				return string.Format ("{0} hours ago", value);
			}
		} else if (diff.TotalDays < 3) {
			value = (int) diff.TotalDays;
			if (value == 1) {
				return "1 day ago";
			} else {
				return string.Format ("{0} days ago", value);
			}
		} else {
			return from.ToString ("yyyy-MM-dd");
		}
	}

	public string GenerateOverview (FrontPageResponse data)
	{
		StringBuilder matrix = new StringBuilder ();
		LaneTreeNode tree = BuildTree (data);
		List<StringBuilder> header_rows = new List<StringBuilder> ();
		List<int> hostlane_order = new List<int> ();

		if (tree == null)
			return string.Empty;

		WriteLanes (header_rows, tree, 0, tree.Depth);

		matrix.AppendLine ("<table class='buildstatus'>");
		for (int i = 0; i < header_rows.Count; i++) {
			if (header_rows [i].Length == 0)
				continue;

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
					string str_date = string.Empty;

					if (work.endtime.Year > 2000)
						str_date = "</br>" + TimeDiffToString (work.endtime, DateTime.UtcNow);
					
					switch (state) {
					case DBState.Executing:
						is_working = true;
						break;
					case DBState.NotDone:
					case DBState.Paused:
					case DBState.DependencyNotFulfilled:
					case DBState.Ignore:
						is_working = false;
						break;
					default:
						is_working = !completed;
						break;
					}

					long dummy;
					if (revision.Length > 16 && !long.TryParse (revision, out dummy))
						revision = revision.Substring (0, 8);

					if (is_working) {
						row.AppendFormat (
							@"<td class='{1}'>
								<center>
									<table class='executing'>
										<td>
											<a href='ViewLane.aspx?lane_id={2}&amp;host_id={3}&amp;revision_id={4}' title='{5}'>{0}{6}</a>
										</td>
									</table>
								<center>
							  </td>",
							revision, state_str, lane_id, host_id, revision_id, "", str_date);
					} else {
						row.AppendFormat ("<td class='{1}'><a href='ViewLane.aspx?lane_id={2}&amp;host_id={3}&amp;revision_id={4}' title='{5}'>{0}{6}</a></td>",
							revision, state_str, lane_id, host_id, revision_id, "", str_date);
					}
				} else {
					row.Append ("<td>-</td>");
				}
			}

			if (added > 0 && row.Length > 0) {
				matrix.Append ("<tr>");
				matrix.Append (row.ToString ());
				matrix.Append ("</tr>");
			}

			counter++;
		} while (counter <= limit && added > 0);

		matrix.AppendLine ("</table>");

		return matrix.ToString ();
	}
}

