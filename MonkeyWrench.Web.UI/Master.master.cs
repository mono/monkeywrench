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

	public WebServices WebService
	{
		get
		{
			return Utils.WebService;
		}
	}

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
			cellLogin.Text = "<a href='User.aspx'>Create account</a> <a href='Login.aspx'>Login</a>";
			rowAdmin.Visible = false;
		} else {
			cellLogin.Text = string.Format ("<a href='User.aspx?username={0}'>My Account ({0})</a> <a href='Login.aspx?action=logout'>Log out</a>", Utilities.GetCookie (Request, "user"));
			rowAdmin.Visible = true;
		}

		CreateTree ();
	}

	private void CreateTree ()
	{
		if (this.response != null)
			return;

		GetLanesResponse response;
		// we need to create a tree of the lanes
		LaneTreeNode root;
		Panel div;

		try {
			response = WebService.GetLanes (WebServiceLogin);
			root = LaneTreeNode.BuildTree (response.Lanes, null);

			SetResponse (response);

			div = new Panel ();
			div.ID = "tree_root_id";

			// layout the tree
			tableMainTree.Rows.Add (Utils.CreateTableRow (CreateTreeViewRow ("index.aspx?show_all=true", "All", 0, root.Depth, true, div, true)));

			tableMainTree.Rows.Add (Utils.CreateTableRow (div));
			WriteTree (root, tableMainTree, 1, root.Depth, div);
		} catch {
			tableMainTree.Rows.Add (Utils.CreateTableRow ("[Exception occurred]"));
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
		row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='{0}'>{1}</a>", target, name)));

		tbl.Rows.Add (row);

		return tbl;
	}
}
