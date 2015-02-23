/*
 * GetRevisionLog.aspx.cs
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
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class GetRevisionLog : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		int? revision_id = Utils.TryParseInt32 (Request ["id"]);
		bool raw = Request ["raw"] == "true";
		string d, l;

		if (revision_id != null) {
			l = WebServices.DownloadString (WebServices.CreateWebServiceDownloadRevisionUrl (revision_id.Value, false, Master.WebServiceLogin));
			if (raw) {
				log.InnerHtml = "<pre>" + HttpUtility.HtmlEncode (l) + "</pre>";
			} else {
				log.InnerHtml = "<div style='background-color: #ccccff; padding: 5px; margin-top: 5px; margin-bottom: 5px;'><pre style='border-width: 0px;'>" + HttpUtility.HtmlEncode (l) + "</pre></div>";
			}
			d = WebServices.DownloadString (WebServices.CreateWebServiceDownloadRevisionUrl (revision_id.Value, true, Master.WebServiceLogin));
			if (raw) {
				diff.InnerHtml = "<pre>" + HttpUtility.HtmlEncode (d) + "</pre>";
			} else {
				diff.InnerHtml = ParseDiff (d);
			}
		}
	}

	private string ParseDiff (string diff)
	{
		if (diff.StartsWith ("diff --git")) {
			return ParseGitDiff (diff);
		} else {
			return "<pre>" + HttpUtility.HtmlEncode (diff) + "</pre>";
		}
	}

	private string ParseGitDiff (string diff)
	{
		StringBuilder result = new StringBuilder (diff.Length);
		string line;
		int old_ln = 0, new_ln = 0;
		int old_d = 0, new_d = 0;
		bool started = false;
		result.Append ("<div>");
		using (StringReader reader = new StringReader (diff)) {
			while ((line = reader.ReadLine ()) != null) {
				if (line.StartsWith ("diff --git")) {
					// New file
					if (started)
						result.AppendLine ("</table>");
					started = true;
					result.AppendLine ("<table class='diff_view_table'>");
					result.AppendFormat ("<tr><td class='diff_view_header_td' colspan='3'>{0}</td></tr>\n", line.Substring (10).Trim ().Split (' ') [0]);
				} else if (line.StartsWith ("index")) {
					// Not sure what this is
					result.AppendFormat ("<tr><td class='diff_view_line_number'><pre class='diff_view_pre'>&nbsp;</pre></td><td class='diff_view_line_number'><pre class='diff_view_pre'>&nbsp;</pre></td><td class='diff_view_index_td'><pre class='diff_view_pre'>{0}</pre></td></tr>\n", line);
				} else if (line.StartsWith ("---") || line.StartsWith ("+++")) {
					// Ignore this for now
					// style = "background-color: white";
					// result.AppendFormat ("<tr style='{1}'><td colspan='3'>{0}</td></tr>", line, style);
				} else if (line.StartsWith ("@@")) {
					// line numbers
					string [] nl = line.Replace ("@@", "").Trim ().Split (' ');
					var oldc = nl [0].IndexOf (',');
					var newc = nl [1].IndexOf (',');
					old_ln = int.Parse (nl [0].Substring (1, oldc > 0 ? oldc - 1 : nl [0].Length - 1));
					new_ln = int.Parse (nl [1].Substring (1, newc > 0 ? newc - 1 : nl [1].Length - 1));
					result.AppendFormat ("<tr><td class='diff_view_line_number'><pre class='diff_view_pre'>&nbsp;</pre></td><td class='diff_view_line_number'><pre class='diff_view_pre'>&nbsp;</pre></td><td class='diff_view_at_td'><pre class='diff_view_pre'>{0}</pre></td></tr>\n", line);
				} else {
					string cl;
					if (line.StartsWith ("-")) {
						cl = "diff_view_removed_line";
						old_d = 1;
						new_d = 0;
					} else if (line.StartsWith ("+")) {
						cl = "diff_view_added_line";
						old_d = 0;
						new_d = 1;
					} else {
						cl = "diff_view_normal_line";
						old_d = 1;
						new_d = 1;
					}
					result.AppendFormat ("<tr><td class='diff_view_line_number'><pre class='diff_view_pre'>{2}</pre></td><td class='diff_view_line_number'><pre class='diff_view_pre'>{3}</pre></td><td class='{1}'><pre class='diff_view_pre'>{0}</pre></td></tr>\n",
						HttpUtility.HtmlEncode (line), cl, old_d == 0 ? string.Empty : old_ln.ToString (), new_d == 0 ? string.Empty : new_ln.ToString ());
					old_ln += old_d;
					new_ln += new_d;
				}
			}
		}
		result.AppendLine ("</table>");
		result.AppendLine ("</div>");
		return result.ToString ();
	}
}
