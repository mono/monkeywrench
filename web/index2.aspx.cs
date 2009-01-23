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

public partial class index2 : Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected override void OnLoad (EventArgs e)
	{
		base.OnLoad (e);

		try {
			this.buildtable.InnerHtml = GenerateOverview (Master.DB);
		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}
	public string GenerateOverview (DB db)
	{
		StringBuilder matrix = new StringBuilder ();

		List<DBLane> lanes = db.GetAllLanes ();
		List<DBHost> hosts = db.GetHosts ();
		List<DBHostLane> hostlanes = db.GetAllHostLanes ();

        bool hosts_header = false;

		// Generate the output
		matrix.AppendLine ("<table class='buildstatus'>");
		matrix.AppendLine ("<tr>");
		matrix.AppendLine ("\t<th colspan='2' rowspan='2'> </th>");
		if (Master.Login != null) {
			matrix.AppendFormat ("\t<th colspan='{0}'><a href='EditLanes.aspx'>Lanes</a></th>\n", lanes.Count);
		} else {
			matrix.AppendFormat ("\t<th colspan='{0}'>Lanes</th>\n", lanes.Count);
		}
        matrix.AppendLine("</tr>");
        matrix.AppendLine("\t<tr>");
		for (int i = 0; i < lanes.Count; i++) {
			if (Master.Login != null) {
				matrix.AppendFormat ("\t<th><a href='EditLane.aspx?lane={0}'>{0}</a></th>\n", lanes [i].lane);
			} else {
				matrix.AppendFormat ("\t<th>{0}</th>\n", lanes [i].lane);
			}
		}
		matrix.AppendLine ("</tr>");

		for (int h = 0; h < hosts.Count; h++) {
			matrix.AppendLine ("<tr>");
            if (!hosts_header)
            {
				if (Master.Login != null) {
					matrix.AppendFormat ("\t<th rowspan='{0}'><a href='EditHosts.aspx'>Hosts</a></th>", hosts.Count + 1);
				} else {
					matrix.AppendFormat ("\t<th rowspan='{0}'>Hosts</th>", hosts.Count + 1);
				}
                hosts_header = true;
            }
			if (Master.Login != null) {
				matrix.AppendFormat ("\t<th><a href='EditHost.aspx?host={0}'>{0}</a></th>", hosts [h].host);
			} else {
				matrix.AppendFormat ("\t<th>{0}</th>", hosts [h].host);
			}
			for (int i = 0; i < lanes.Count; i++) {
				DBLane lane = lanes [i];
				DBHostLane hostlane = null;

				for (int hl = 0; hl < hostlanes.Count; hl++) {
					if (hostlanes [hl].host_id == hosts [h].id && hostlanes [hl].lane_id == lane.id) {
						hostlane = hostlanes [hl];
						break;
					}
				}

				int last_rev = db.GetLastRevision (lane.lane);
				if (last_rev == 0 || hostlane == null) {
					matrix.Append ("<td>None</td>");
				} else {
					matrix.AppendFormat("<td class='{3}'><a href='ViewTable.aspx?lane={0}&amp;host={2}'>{1}</a></td>", lane.lane, last_rev, hosts[h].host, hostlane.enabled ? "enabled" : "disabled");
				}
			}
            matrix.AppendLine("</tr>");
		}

        matrix.AppendLine("</table >");

		return matrix.ToString ();
	}
}
