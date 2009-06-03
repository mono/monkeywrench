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

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class Master : System.Web.UI.MasterPage
{
	private static WebServiceLogin web_service_login;

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
				web_service_login = Utils.CreateWebServiceLogin (Context.Request);

			return web_service_login;
		}
	}

	public void ClearLogin ()
	{
		web_service_login = null;
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		TableRow row = new TableRow ();
		TableCell title = new TableCell ();
		TableCell user = new TableCell ();

		title.Text = "<a href='index.aspx'>MonkeyWrench</a>";
		if (!Utils.IsInRole (MonkeyWrench.DataClasses.Logic.Roles.Administrator)) {
			user.Text = "<a href='Login.aspx'>Login</a>";
		} else {
			user.Text = string.Format ("<a href='User.aspx?id={0}'>{0}</a> <a href='Login.aspx?action=logout'>Log out</a>", Context.User.Identity.Name);
		}
		user.CssClass = "headerlogin";
		row.Cells.Add (title);
		row.Cells.Add (user);

		tableHeader.Rows.Add (row);

		if (!IsPostBack)
			CreateTree ();

		if (Utils.IsInRole (MonkeyWrench.DataClasses.Logic.Roles.Administrator)) {
			tableFooter.Rows.Add (Utils.CreateTableRow ("<a href='EditHosts.aspx'>Edit Hosts</a>"));
			tableFooter.Rows.Add (Utils.CreateTableRow ("<a href='EditLanes.aspx'>Edit Lanes</a>"));
		}
		tableFooter.Rows.Add (Utils.CreateTableRow ("<a href='doc/index.html'>Documentation</a>"));
	}

	private void CreateTree ()
	{
		GetLanesResponse response = WebService.GetLanes (WebServiceLogin);
		// we need to create a tree of the lanes
		LaneTreeNode root = LaneTreeNode.BuildTree (response.Lanes, null);

		// layout the tree
		TreeNode tn = new TreeNode ();
		tn.Text = "All";
		tn.NavigateUrl = "index.aspx";
		treeMain.Nodes.Add (tn);
		root.WriteTree (tn.ChildNodes);
		treeMain.NodeIndent = 8;
	}
}
