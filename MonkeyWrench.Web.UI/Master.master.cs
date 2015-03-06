/*
 * Master.master.cs
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
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class Master : System.Web.UI.MasterPage
{
	private WebServiceLogin web_service_login;
	private WebServiceResponse response;
	private GetLeftTreeDataResponse tree_response;

	public WebServiceLogin WebServiceLogin
	{
		get
		{
			if (web_service_login == null)
				web_service_login = Utilities.CreateWebServiceLogin (Context.Request);

			return web_service_login;
		}
	}

	public void ClearLogin ()
	{
		web_service_login = null;
	}

	/// <summary>
	/// Redirects to the login page. Does not end the current request.
	/// </summary>
	public void RequestLogin ()
	{
		Response.Redirect ("Login.aspx?referrer=" + HttpUtility.UrlEncode (Request.Url.ToString ()));
	}

	private void SetResponse (WebServiceResponse response)
	{
		this.response = response;
		LoadView ();
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		LoadView ();
		if (!string.IsNullOrEmpty (Configuration.SiteSkin)) {
			idFavicon.Href = "res/" + Configuration.SiteSkin + "/favicon.ico";
			imgLogo.Src = "res/" + Configuration.SiteSkin + "/logo.png";
			cssSkin.Href = "res/" + Configuration.SiteSkin + "/" + Configuration.SiteSkin + ".css";
		}
	}

	private void LoadView ()
	{
		if (!Authentication.IsLoggedIn (response)) {
			cellLogin.Text = "<li><a href='User.aspx'>Create account</a></li><li><a href='Login.aspx'>Login</a></li>";
			adminmenu.Visible = false;
		} else {
			cellLogin.Text = string.Format ("<li><a href='User.aspx?username={0}'>My Account ({0})</a></li>", Utilities.GetCookie (Request, "user"));
			cellLogout.Text = "<li><a href='Login.aspx?action=logout'>Log out</a></li>";
			adminmenu.Visible = true;
		}

		if (tree_response != null)
			return;

		try {
			tree_response = Utils.LocalWebService.GetLeftTreeData (WebServiceLogin);
		} catch(MonkeyWrench.WebServices.UnauthorizedException) {
			// LoadView is called on the login page, but if anonymous access is disabled,
			// the user will be 'unauthorized' to view the sidebar, and thus the entire
			// login page.
			// So catch the exception and ignore it.
		}

		if (tree_response != null) {
			CreateTree ();
			CreateHostStatus ();
		}
	}

	private void CreateHostStatus ()
	{
		try {
			while (tableHostStatus.Rows.Count > 1)
				tableHostStatus.Rows.RemoveAt (tableHostStatus.Rows.Count - 1);

			var idles = new List<string> ();
			var working = new List<string> ();
			TableRow row;

			for (int i = 0; i < tree_response.HostStatus.Count; i++) {
				var status = tree_response.HostStatus [i];
				var idle = string.IsNullOrEmpty (status.lane);
				
				string tooltip = string.Empty;
				var color = EditHosts.GetReportDateColor (true, status.report_date);
				if (!idle)
					tooltip = string.Format ("Executing {0}\n", status.lane);
				tooltip += string.Format ("Last check-in date: {0}", index.TimeDiffToString (status.report_date, DateTime.Now));
				
				var str = string.Format ("<span style='color: {3}; cursor: pointer;' onclick=\"javascript: window.location = 'ViewHostHistory.aspx?host_id={0}'\" title='{2}'>{1}</span>", status.id, status.host, HttpUtility.HtmlEncode (tooltip), color);
				if (idle) {
					idles.Add (str);
				} else {
					working.Add (str);
				}
			}

			if (!string.IsNullOrEmpty (tree_response.UploadStatus))
				cellUploadStatus.Text = "<span class='uploadstatus'>" + tree_response.UploadStatus + "</span>";

			if (working.Count > 0) {
				row = Utils.CreateTableRow ("Working");
				row.CssClass = "hoststatus_header";
				tableHostStatus.Rows.Add (row);
				row = Utils.CreateTableRow (string.Join ("<br/> ", working.ToArray ()));
				tableHostStatus.Rows.Add (row);
			}

			if (idles.Count > 0) {
				row = Utils.CreateTableRow ("Idle");
				row.CssClass = "hoststatus_header";
				tableHostStatus.Rows.Add (row);
				row = Utils.CreateTableRow (string.Join ("<br/> ", idles.ToArray ()));
				tableHostStatus.Rows.Add (row);
			}

		} catch {
			tableHostStatus.Visible = false;
		}
	}

	private void CreateTree ()
	{
		if (this.response != null)
			return;

		// we need to create a tree of the lanes
		LaneTreeNode root;
		Panel div;

		// Remove disabled lanes.
		var lanes = new List<DBLane> (tree_response.Lanes);
		for (int i = lanes.Count -1; i >= 0; i--) {
			if (lanes [i].enabled)
				continue;
			lanes.RemoveAt (i);
		}
		root = LaneTreeNode.BuildTree (lanes, null);

		SetResponse (tree_response);

		// layout the tree
		div = new Panel ();
		div.ID = "tree_root_id";

		tableMainTree.Rows.Add (Utils.CreateTableRow (CreateTreeViewRow ("index.aspx?show_all=true", "All", 0, root.Depth, true, div, true)));

		tableMainTree.Rows.Add (Utils.CreateTableRow (div));
		WriteTree (root, tableMainTree, 1, root.Depth, div);

		// layout the tags
		div = new Panel ();
		div.ID = "tags_root_id";

		tableMainTree.Rows.Add (Utils.CreateTableRow (CreateTreeViewRow (null, "Tags", 0, 1, true, div, true)));
		tableMainTree.Rows.Add (Utils.CreateTableRow (div));
		WriteTags (tree_response.Tags, tableMainTree, 1, div);
	}

	public void WriteTags (List<string> tags, Table tableMain, int level, Panel containing_div)
	{
		Panel div = new Panel ();
		div.ID = "tag_node_" + (++counter).ToString ();

		foreach (var tag in tags) {
			containing_div.Controls.Add (CreateTreeViewRow (string.Format ("index.aspx?tags={0}", HttpUtility.UrlEncode (tag)), tag, 1, 1, false, div, false));
		}
	}

	public void WriteTree (LaneTreeNode node, Table tableMain, int level, int max_level, Panel containing_div)
	{
		Panel div = new Panel ();
		div.ID = "tree_node_" + (++counter).ToString ();

		foreach (LaneTreeNode n in node.Children) {
			bool hiding = true;
			if (!string.IsNullOrEmpty (Request ["lane"])) {
				if (n.Lane.lane == Request ["lane"] || n.Find ((v) => v.Lane.lane == Request ["lane"]) != null) {
					hiding = false;
				}
			}

			containing_div.Controls.Add (CreateTreeViewRow (string.Format ("index.aspx?lane={0}", HttpUtility.UrlEncode (n.Lane.lane)), n.Lane.lane, level, max_level, n.Children.Count > 0, div, hiding));
			
			if (n.Children.Count > 0) {
				containing_div.Controls.Add (div);
				WriteTree (n, tableMain, level + 1, max_level, div);
				div = new Panel ();
				div.ID = "tree_node_" + (++counter).ToString ();
			}
		}
	}

	static int counter = 0;
	public Table CreateTreeViewRow (string target, string name, int level, int max_level, bool has_children, Panel div_to_switch, bool enable_default_hiding)
	{
		TableRow row = new TableRow ();
		TableCell cell;
		Table tbl = new Table ();

		for (int i = 0; i < level; i++) {
			cell = new TableCell ();
			cell.Text = "<div style='width:8px;height:1px;'/>";
			row.Cells.Add (cell);
		}
		cell = new TableCell ();
		if (!has_children) {
			cell.Text = "<div style='width:20px;height:20px;'/>";
		} else {
			counter++;
			cell.Text = string.Format (@"
<img id='minus_img_{1}' src='res/minus.gif' alt='Collapse {0}' height='20px' width='20px' style='display:{3};' onclick='switchVisibility (""plus_img_{1}"", ""minus_img_{1}"", ""{2}"");' />
<img id='plus_img_{1}'  src='res/plus.gif'  alt='Expand   {0}' height='20px' width='20px' style='display:{4};'  onclick='switchVisibility (""plus_img_{1}"", ""minus_img_{1}"", ""{2}"");' />
", name, counter, div_to_switch.ClientID, (enable_default_hiding && level > 0) ? "none" : "block", (enable_default_hiding && level > 0) ? "block" : "none");
			if (enable_default_hiding && level > 0) {
				div_to_switch.Attributes ["style"] = "display: none;";
			}
		}
		row.Cells.Add (cell);
		if (!string.IsNullOrEmpty (target)) {
			row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='{0}'>{1}</a>", target, name)));
		} else {
			row.Cells.Add (Utils.CreateTableCell (name));
		}

		tbl.Rows.Add (row);

		return tbl;
	}
}

