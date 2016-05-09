/*
 * ViewAuditEntries.aspx.cs
 *
 * Authors:
 *   Matt Sylvia (matthew.sylvia@xamarin.com)
 *   
 * Copyright 2016 Xamarin, Inc. (http://www.xamarin.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class ViewAuditEntries : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		GetAuditResponse response;
		int limit;
		int page;

		if (!int.TryParse (Request.QueryString ["limit"], out limit))
			limit = 25;
		if (!int.TryParse (Request.QueryString ["page"], out page))
			page = 0;

		response = Utils.LocalWebService.GetAuditHistory (Master.WebServiceLogin, limit, limit * page);

		auditList.InnerHtml = "<h2>HELLO WORLD</h2>";

		StringBuilder table = new StringBuilder ();
		table.AppendLine ("<table class='buildstatus'>");
		table.AppendLine ("<tr><td>Timstamp</td><td>User</td><td>IP</td><td>Action</td></tr>");

		for (int i = 0; i < response.AuditEntries.Count; i++) {
			
			DBAudit audit = response.AuditEntries [i];

			table.Append ("<tr>");
			table.AppendFormat ("<td>{0}</td>", audit.stamp.ToString ("yyyy/MM/dd HH:mm:ss UTC"));
			table.AppendFormat ("<td style='text-align: left;'>{0}</td>", audit.person_login);
			table.AppendFormat ("<td style='text-align: left;'>{0}</td>", !string.IsNullOrEmpty(audit.ip) ? audit.ip : "127.0.0.1");
			table.AppendFormat ("<td style='text-align: left;'>{0}</td>", audit.action);
			table.AppendLine ("</tr>");
		}

		table.AppendLine ("</table>");

		// Add the pager at the bottom
		table.AppendLine(GeneratePager (response.Count, page, limit));
		auditList.InnerHtml = table.ToString ();
	}

	public string GeneratePager (int total, int page, int limit)
	{
		StringBuilder pager = new StringBuilder ();
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
				pager.Append (GeneratePageLink (i + 1, limit));
			}
		} else {
			if (page <= (range + 1)) {
				for (int i = 0; i < (page + range); i++) {
					if (page == i)
						pager.Append (string.Format ("<b style=\"padding: 5px;\">{0}</b>", i + 1));
					else
						pager.Append (GeneratePageLink (i + 1, limit));
				}
				pager.AppendFormat ("...");
				pager.Append (GeneratePageLink (pages - 2, limit));
				pager.Append (GeneratePageLink (pages - 1, limit));
			} else if (page > (pages - range - 4)) {
				pager.Append (GeneratePageLink (1, limit));
				pager.Append (GeneratePageLink (2, limit));
				pager.AppendFormat ("...");
				for (int i = page - range; i < pages; i++) {
					if (page == i)
						pager.Append (string.Format ("<b style=\"padding: 5px;\">{0}</b>", i + 1));
					else
						pager.Append (GeneratePageLink (i + 1, limit));
				}
			} else {
				pager.Append (GeneratePageLink (1, limit));
				pager.Append (GeneratePageLink (2, limit));
				pager.AppendFormat ("...");
				for (int i = page - range; i < page + range; i++) {
					if (page == i)
						pager.Append (string.Format ("<b style=\"padding: 5px;\">{0}</b>", i + 1));
					else
						pager.Append (GeneratePageLink (i + 1, limit));
				}
				pager.AppendFormat ("...");
				pager.Append (GeneratePageLink (pages - 2, limit));
				pager.Append (GeneratePageLink (pages - 1, limit));
			}
		}
		pager.AppendFormat ("</p>");
		return pager.ToString ();
	}

	private string GeneratePageLink (int page, int limit)
	{
		return string.Format ("&nbsp;<a href='ViewAuditEntries.aspx?page={0}&amp;limit={1}'>{2}</a> ", (page - 1), limit, page);
	}
}


