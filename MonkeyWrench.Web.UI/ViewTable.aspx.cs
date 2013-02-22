/*
 * ViewTable.aspx.cs
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

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class ViewTable : System.Web.UI.Page
{
	GetViewTableDataResponse response;

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected override void OnLoad (EventArgs e)
	{
		DBHost dbhost;
		DBLane dblane;

		base.OnLoad (e);

		try {

			int page = 0;
			int page_size = 0;
			bool horizontal;
			string action;

			if (Authentication.IsInCookieRole (Request, MonkeyWrench.DataClasses.Logic.Roles.Administrator)) {
				action = Request ["action"];
				if (!string.IsNullOrEmpty (action)) {
					switch (action) {
					case "clearrevisions":
						string revisions = Request ["revisions"];
						int lane_id;
						int host_id;
						int revision_id;
						if (!int.TryParse (Request ["lane_id"], out lane_id))
							throw new Exception ("Invalid lane_id");
						if (!int.TryParse (Request ["host_id"], out host_id))
							throw new Exception ("Invalid host_id");
						foreach (string revision in revisions.Split (new char [] {';'}, StringSplitOptions.RemoveEmptyEntries)) {
							if (!int.TryParse (revision.Replace ("revision_id_", ""), out revision_id))
								throw new Exception ("Invalid revision_id: " + revision.ToString () + "(revisions: '" + revisions + "')");
							Master.WebService.ClearRevision (Master.WebServiceLogin, lane_id, host_id, revision_id);
						}
						Response.Redirect (string.Format ("ViewTable.aspx?lane_id={0}&host_id={1}", lane_id, host_id), false);
						return;
					}
				}
			}

			int.TryParse (Request ["page"], out page);
			int.TryParse (Request ["page_size"], out page_size);

			if (page_size <= 0)
				page_size = 20;
			if (page < 0)
				page = 0;

			if (!bool.TryParse (Request ["horizontal"], out horizontal)) {
				// Not specified. See if it's a cookie
				if (Request.Cookies ["horizontal"] != null)
					bool.TryParse (Request.Cookies ["horizontal"].Value, out horizontal);
			}
			Response.Cookies.Add (new HttpCookie ("horizontal", horizontal.ToString ()));

			response = Master.WebService.GetViewTableData (Master.WebServiceLogin,
				Utils.TryParseInt32 (Request ["lane_id"]), Request ["lane"],
				Utils.TryParseInt32 (Request ["host_id"]), Request ["host"],
			page, page_size);

			if (response.Exception != null) {
				if (response.Exception.HttpCode == 403) {
					Master.RequestLogin ();
					return;
				}
				lblMessage.Text = response.Exception.Message;
				return;
			}

			dblane = response.Lane;
			dbhost = response.Host;

			this.header.InnerHtml = GenerateHeader (response, dblane, dbhost , horizontal);
			this.buildtable.InnerHtml = GenerateLaneTable (response, dblane, dbhost, horizontal, page, page_size);
			this.pager.InnerHtml = GeneratePager (response, dblane, dbhost, page, page_size);
		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}

	public string GeneratePager (GetViewTableDataResponse response, DBLane lane, DBHost host, int page, int limit)
	{
		StringBuilder pager = new StringBuilder ();
		int total = response.Count;
		int pages = total / limit;

		if (total % limit != 0)
			pages++;
		Console.WriteLine ("Pages: {0} total: {1}", pages, total);

		if (page > pages - 1)
			page = pages - 1;

		int range = 5;
		pager.AppendFormat ("<p> Page&nbsp;");
		if (pages < (range * 2)) {
			for (int i = 0; i < pages; i++) {
				pager.Append (GeneratePageLink (host.id, lane.id, i + 1, limit));
			}
		} else {
			if (page <= (range + 1)) {
				for (int i = 0; i < (page + range); i++) {
					if (page == i)
						pager.Append (string.Format ("<b>{0}</b>", i + 1));
					else
						pager.Append (GeneratePageLink (host.id, lane.id, i + 1, limit));
				}
				pager.AppendFormat ("...");
				pager.Append (GeneratePageLink (host.id, lane.id, pages - 2, limit));
				pager.Append (GeneratePageLink (host.id, lane.id, pages - 1, limit));
			} else if (page > (pages - range - 4)) {
				pager.Append (GeneratePageLink (host.id, lane.id, 1, limit));
				pager.Append (GeneratePageLink (host.id, lane.id, 2, limit));
				pager.AppendFormat ("...");
				for (int i = page - range; i < pages; i++) {
					if (page == i)
						pager.Append (string.Format ("<b>{0}</b>", i + 1));
					else
						pager.Append (GeneratePageLink (host.id, lane.id, i + 1, limit));
				}
			} else {
				pager.Append (GeneratePageLink (host.id, lane.id, 1, limit));
				pager.Append (GeneratePageLink (host.id, lane.id, 2, limit));
				pager.AppendFormat ("...");
				for (int i = page - range; i < page + range; i++) {
					if (page == i)
						pager.Append (string.Format ("<b>{0}</b>", i + 1));
					else
						pager.Append (GeneratePageLink (host.id, lane.id, i + 1, limit));
				}
				pager.AppendFormat ("...");
				pager.Append (GeneratePageLink (host.id, lane.id, pages - 2, limit));
				pager.Append (GeneratePageLink (host.id, lane.id, pages - 1, limit));
			}
		}
		pager.AppendFormat ("</p>");
		return pager.ToString ();
	}

	private string GeneratePageLink (int hostid, int laneid, int page, int limit)
	{
		return string.Format ("&nbsp;<a href='ViewTable.aspx?host_id={0}&amp;lane_id={1}&amp;page={2}&amp;limit={3}'>{4}</a> ", hostid, laneid, page - 1, limit, page);
	}

	public string GenerateHeader (GetViewTableDataResponse response, DBLane lane, DBHost host, bool horizontal)
	{
		string result;
		string format;
		string disabled_msg = string.Empty;

		if (!response.Enabled)
			disabled_msg = " (Disabled)";

		if (Authentication.IsInRole (response, MonkeyWrench.DataClasses.Logic.Roles.Administrator)) {
			format = @"<h2>Build Matrix for <a href='EditLane.aspx?lane_id={0}'>'{2}'</a> on <a href='EditHost.aspx?host_id={5}'>'{4}'</a>{6}</h2><br/>";
		} else {
			format = @"<h2>Build Matrix for '{2}' on '{4}'{6}</h2><br/>";
		}

		format += @"<a href='ViewTable.aspx?lane_id={0}&amp;host_id={1}&amp;horizontal={3}'>Reverse x/y axis</a><br/>";
		if (Authentication.IsInRole (response, MonkeyWrench.DataClasses.Logic.Roles.Administrator))
			format += string.Format (@"<a href='javascript:clearRevisions ({0}, {1})'>Clear selected revisions</a><br/>", lane.id, host.id);

		format += "<br/>";

		result = string.Format (format, lane.id, host.id, lane.lane, horizontal ? "false" : "true", host.host, host.id, disabled_msg);

		return result;
	}

	class TableNode {
		public string text;
		public string @class;
		//public string style;
		public bool is_header;

		public TableNode (string text, string @class, bool is_header)
		{
			this.text = text;
			this.@class = @class;
			this.is_header = is_header;
		}

		public TableNode (string text, string @class)
			: this (text, @class, false)
		{
		}

		public TableNode (string text, bool is_header)
			: this (text, null, is_header)
		{
		}

		public TableNode (string text)
			: this (text, null, false)
		{
		}
	}

	string DarkenColor (string color, int magic_number)
	{
		magic_number--;
		if (magic_number >= 3) {
			return color + "3";
		} else if (magic_number > 0) {
			return color + magic_number.ToString ();
		} else {
			return color;
		}
	}

	public string GenerateLaneTable (GetViewTableDataResponse response, DBLane lane, DBHost host, bool horizontal, int page, int limit)
	{
		StringBuilder matrix = new StringBuilder ();
		StringBuilder tooltip = new StringBuilder ();
		bool new_revision = true;
		int revision_id = 0;
		List<DBRevisionWorkView> views = response.RevisionWorkViews;
		List<List<TableNode>> table = new List<List<TableNode>> ();
		List<TableNode> row = new List<TableNode> ();
		List<TableNode> header = new List<TableNode> ();

		try {
			for (int i = 0; i < views.Count; i++) {
				while (header.Count <= views [i].sequence) {
					header.Add (null);
				}
				if (header [views [i].sequence] != null)
					continue;

				header [views [i].sequence] = new TableNode (string.Format ("<a href='ViewWorkTable.aspx?lane_id={0}&amp;host_id={1}&amp;command_id={2}'>{3}</a>", lane.id, host.id, views [i].command_id, views [i].command));
			}
			header.RemoveAll (delegate (TableNode match) { return match == null; });
			header.Insert (0, new TableNode ("Revision", true));
			header.Insert (1, new TableNode ("Diff", true));
			header.Insert (2, new TableNode ("Author", true));
			//if (Authentication.IsInRole (response, MonkeyWrench.DataClasses.Logic.Roles.Administrator)) {
			//    header.Insert (3, new TableNode ("Select", true));
			//}
			header.Add (new TableNode ("Host", true));
			header.Add (new TableNode ("Duration", true));
			table.Add (header);

			bool failed = false;
			double duration = 0;

			for (int i = 0; i < views.Count; i++) {
				DBRevisionWorkView view = views [i];
				DBState revisionwork_state = (DBState) view.revisionwork_state;
				DBState state = (DBState) view.state;

				new_revision = revision_id != view.revision_id;
				revision_id = view.revision_id;

				if (new_revision) {
					if (i > 0) {
						table.Add (row);
						row [row.Count - 1] = new TableNode (TimeSpan.FromSeconds (duration).ToString (), row [0].@class);
					}

					string revision = view.revision;
					long dummy;
					if (revision.Length > 16 && !long.TryParse (revision, out dummy))
						revision = revision.Substring (0, 8);

					string clazz = revisionwork_state.ToString ().ToLower ();
					clazz = clazz + " " + DarkenColor (clazz, table.Count);
					row = new List<TableNode> ();

					tooltip.Length = 0;
					tooltip.AppendFormat ("Author: {0}.", view.author);
					if (view.starttime.Date.Year > 2000)
						tooltip.AppendFormat (" Build start date: {0}.", view.starttime.ToUniversalTime ().ToString ("yyyy/MM/dd HH:mm:ss UTC"));

					row.Add (new TableNode (string.Format ("<a href='ViewLane.aspx?lane_id={0}&amp;host_id={1}&amp;revision_id={2}' title='{4}'>{3}</a>", lane.id, host.id, view.revision_id, revision, tooltip.ToString ()), clazz));
					row.Add (new TableNode (string.Format ("<a href='GetRevisionLog.aspx?id={0}'>diff</a>", view.revision_id)));
					row.Add (new TableNode (view.author));
					
					//if (Authentication.IsInRole (response, MonkeyWrench.DataClasses.Logic.Roles.Administrator))
					//    row.Add (new TableNode (string.Format ("<input type=checkbox id='id_revision_chk_{1}' name='revision_id_{0}' />", view.revision_id, i)));
					while (row.Count < header.Count - 2)
						row.Add (new TableNode ("-"));
					row.Add (new TableNode (view.workhost ?? ""));
					row.Add (new TableNode (""));
					failed = false;
					duration = 0;
				}

				if (view.endtime > view.starttime)
					duration += (view.endtime - view.starttime).TotalSeconds;

				if (state == DBState.Failed && !view.nonfatal)
					failed = true;

				// result
				string result;
				bool completed = true;
				switch (state) {
				case DBState.NotDone:
					completed = false;
					result = failed ? "skipped" : "queued"; break;
				case DBState.Executing:
					completed = false;
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
					completed = false;
					result = "paused"; break;
				case DBState.Ignore:
					completed = false;
					result = "ignore"; break;
				default:
					completed = true;
					result = "unknown"; break;
				}

				for (int j = 2; j < header.Count; j++) {
					if (header [j].text.Contains (view.command)) {
						if (completed) {
							row [j] = new TableNode (string.Format ("<a href='{0}'>{1}</a>", Utilities.CreateWebServiceDownloadUrl (Request, view.id, view.command + ".log", true), result));
						} else {
							row [j] = new TableNode (result);
						}
						row [j].@class = result + " " + DarkenColor (result, table.Count);
						break;
					}
				}
			}

			table.Add (row);
			row [row.Count - 1] = new TableNode (TimeSpan.FromSeconds (duration).ToString (), row [0].@class);

			matrix.AppendLine ("<table class='buildstatus'>");
			if (horizontal) {
				for (int i = 0; i < header.Count; i++) {
					matrix.Append ("<tr>");
					for (int j = 0; j < table.Count; j++) {
						TableNode node = table [j] [i];
						string td = node.is_header ? "th" : "td";
						matrix.Append ('<');
						matrix.Append (td);
						if (node.@class != null) {
							matrix.Append (" class='");
							matrix.Append (node.@class);
							matrix.Append ("'");
						}
						/*
						if (node.style != null) {
							matrix.Append (" style='");
							matrix.Append (node.style);
							matrix.Append ("'");
						}*/
						matrix.Append (">");
						matrix.Append (node.text);
						matrix.Append ("</");
						matrix.Append (td);
						matrix.Append (">");
					}
					matrix.AppendLine ("</tr>");
				}
			} else {
				for (int i = 0; i < table.Count; i++) {
					matrix.Append ("<tr>");
					for (int j = 0; j < row.Count; j++) {
						TableNode node = table [i] [j];
						string td = node.is_header ? "th" : "td";
						matrix.Append ('<');
						matrix.Append (td);
						if (node.@class != null) {
							matrix.Append (" class='");
							matrix.Append (node.@class);
							matrix.Append ("'");
						}
						/*
						if (node.style != null) {
							matrix.Append (" style='");
							matrix.Append (node.style);
							matrix.Append ("'");
						}*/
						matrix.Append (">");
						matrix.Append (node.text);
						matrix.Append ("</");
						matrix.Append (td);
						matrix.Append (">");
					}
					matrix.AppendLine ("</tr>");
				}
			}
			matrix.AppendLine ("</table>");

		} catch (Exception ex) {
			matrix.Append (ex.ToString ().Replace ("\n", "<br/>"));
		}
		return matrix.ToString ();
	}
}
