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

	private void SetResponse (WebServiceResponse response)
	{
		this.response = response;
		LoadView ();
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		LoadView ();
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

		GetLanesResponse response = WebService.GetLanes (WebServiceLogin);
		// we need to create a tree of the lanes
		LaneTreeNode root = LaneTreeNode.BuildTree (response.Lanes, null);

		SetResponse (response);
		
		// layout the tree
		TreeNode tn = new TreeNode ();
		tn.Text = "All";
		tn.NavigateUrl = "index.aspx?show_all=true";
		treeMain.Nodes.Clear ();
		treeMain.Nodes.Add (tn);
		root.WriteTree (tn.ChildNodes);
		treeMain.NodeIndent = 8;
	}
}
