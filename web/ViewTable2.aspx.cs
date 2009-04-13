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

public partial class ViewTable2 : System.Web.UI.Page
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
			int page = 0;
			int limit = 0;
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

			int.TryParse (Request ["page"], out page);
			int.TryParse (Request ["limit"], out limit);

			if (limit <= 0)
				limit = 20;
			if (page < 0)
				page = 0;

			this.header.InnerHtml = GenerateHeader (db, dblane, dbhost, horizontal);
			this.buildtable.InnerHtml = GenerateLaneTable (db, dblane, dbhost, horizontal, page, limit);
			this.pager.InnerHtml = GeneratePager (db, dblane, dbhost, page, limit);
		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}

	public string GeneratePager (DB db, DBLane lane, DBHost host, int page, int limit)
	{
		StringBuilder pager = new StringBuilder ();
		int total = DBRevisionWork.GetCount (db, lane, host);
		int pages = total / limit;

		if (total % limit != 0)
			pages++;
		Console.WriteLine ("Pages: {0} total: {1}", pages, total);

		if (page > pages - 1)
			page = pages - 1;

		int range = 3;
		pager.AppendFormat("<p> Page&nbsp;");
		if (pages < (range + limit)) {
			for (int i = 0; i < pages; i++) {
				pager.Append(GeneratePageLink(host.id,lane.id.i+1,limit));
			} 
		} else {
			if (page < (range + 1)) {
				for (int i = 0; i < (page + range); i++) {
					pager.Append(GeneratePageLink(host.id,lane.id.i+1,limit));
				}
				pager.AppendFormat ("...");
				pager.Append(GeneratePageLink(host.id,lane.id,pages-2,limit));
				pager.Append(GeneratePageLink(host.id,lane.id,pages-1,limit));
			} else if (page > (pages - range)) {
				pager.Append(GeneratePageLink(host.id,lane.id,1,limit));
				pager.Append(GeneratePageLink(host.id,lane.id,2,limit));
				pager.AppendFormat ("...");
				for (int i = page-range; i < pages; i++) {
					pager.Append(GeneratePageLink(host.id,lane.id,i+1,limit));
				}
			} else {
				pager.Append(GeneratePageLink(host.id,lane.id,1,limit));
				pager.Append(GeneratePageLink(host.id,lane.id,2,limit));
				pager.AppendFormat ("...");
				for (int i = page - range; i < page + range; i++) {
					pager.Append(GeneratePageLink(host.id,lane.id,i+1,limit));
				}
				pager.AppendFormat ("...");
				pager.Append(GeneratePageLink(host.id,lane.id,pages-2,limit));
				pager.Append(GeneratePageLink(host.id,lane.id,pages-1,limit));
			}
		}
		pager.AppendFormat("</p>");
		return pager.ToString ();
	}

	private string GeneratePageLink(int hostid, int laneid, int page, int limit)
	{
		return string.Format ("&nbsp;<a href='ViewTable2.aspx?host_id={0}&amp;lane_id={1}&amp;page={2}&amp;limit={3}'>{4}</a> ", hostid, laneid, page - 1 , limit, page);
	}

	public string GenerateHeader (DB db, DBLane lane, DBHost host, bool horizontal)
	{
		string result;
		string format;

		if (Login != null) {
			format = @"<h2>Build Matrix for <a href='EditLane.aspx?lane_id={0}'>'{2}'</a> on <a href='EditHost.aspx?host_id={5}'>'{4}'</a></h2><br/>";
		} else {
			format = @"<h2>Build Matrix for '{2}' on '{4}'</h2><br/>";
		}

		format += @"<a href='ViewTable2.aspx?lane_id={0}&amp;host_id={1}&amp;horizontal={3}'>Reverse x/y axis</a><br/>";
		if (Login != null)
			format += @"<a href='javascript:clearRevisions ()'>Clear selected revisions</a><br/>";

		format += "<br/>";

		result = string.Format (format, lane.id, host.id, lane.lane, horizontal ? "false" : "true", host.host, host.id);

		return result;
	}

	public string GenerateLaneTable (DB db, DBLane lane, DBHost host, bool horizontal, int page, int limit)
	{
		StringBuilder matrix = new StringBuilder ();
		DateTime beginning = new DateTime (2001, 1, 1, 0, 0, 0);
		bool new_revision = true;
		int revision_id = 0;
		int result_index;
		List<DBRevisionWorkView> views;
		List<List<string>> table = new List<List<string>> ();
		List<string> row = new List<string> ();
		List<string> header = new List<string> ();
		List<string> header_classes = new List<string> ();

		try {
			views = DBRevisionWorkView.Query (db, lane, host, limit, page);

			for (int i = 0; i < views.Count; i++) {
				while (header.Count <= views [i].sequence) {
					header.Add (null);
				}
				if (header [views [i].sequence] != null)
					continue;

				header [views [i].sequence] = string.Format ("<a href='ViewWorkTable.aspx?lane_id={0}&amp;host_id={1}&amp;command_id={2}'>{3}</a>", lane.id, host.id, views [i].command_id, views [i].command);
			}
			header.RemoveAll (delegate (string match) { return match == null; });
			header.Insert (0, "Revision");
			header.Insert (1, "Author");
			if (Login != null)
				header.Insert (2, "Select");
			result_index = Login != null ? 3 : 2;
			table.Add (header);

			bool failed = false;
			for (int i = 0; i < views.Count; i++) {
				DBRevisionWorkView view = views [i];

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
					header_classes.Add (view.RevisionWorkState.ToString ().ToLower ());
					failed = false;
				}

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
					result = view.nonfatal ? "issues" : "failure"; break;
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
					if (header [j].Contains (view.command)) {
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
						if (i == 0 && j > 0) {
							matrix.AppendFormat ("<{0} class='{1}'>", td, header_classes [j - 1]);
						} else if (i >= result_index && j > 1) {
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
					matrix.Append ("<tr>\n");
					for (int j = 0; j < row.Count; j++) {
						string td = j == 0 ? "th" : "td";
						if (j == 0 & i > 0) {
							matrix.AppendFormat ("\t<{0} class='{1}'>", td, header_classes [i - 1]);
						} else if (j >= result_index && row [j] != "-" && i > 0) {
							matrix.AppendFormat ("\t<{0} class='{1}'>", td, row [j]);
						} else {
							matrix.AppendFormat ("\t<{0}>", td);
						}
						matrix.AppendFormat (row [j]);
						matrix.AppendFormat ("</{0}>\n", td);
					}
					matrix.AppendLine ("</tr>");
				}
			}
			matrix.AppendLine ("</table>");

		} catch (Exception ex) {
			matrix.Append (ex.ToString ().Replace ("\n", "</br>"));
		}
		return matrix.ToString ();
	}
}
