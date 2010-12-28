/*
 * Identities.aspx.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2010 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class Identities : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		GetIdentitiesResponse response;
		TableRow row;

		try {

			foreach (TextBox tb in new TextBox [] { txtEmailEmail, txtEmailName, txtEmailPassword }) {
				tb.Attributes.Add ("onfocus", "javascript: document.getElementById ('lblEmailHelp').innerHTML = '" + tb.ToolTip + "';");
			}

			foreach (TextBox tb in new TextBox [] { txtIrcChannels, txtIrcName, txtIrcNicks, txtIrcServers}) {
				tb.Attributes.Add ("onfocus", "javascript: document.getElementById ('lblIrcHelp').innerHTML = '" + tb.ToolTip + "';");
			}
			
			response = Master.WebService.GetIdentities (Master.WebServiceLogin);

			if (response.Exception != null) {
				lblMessage.Text = response.Exception.Message;
			} else {
				if (response.IrcIdentities != null) {
					foreach (DBIrcIdentity irc in response.IrcIdentities) {
						row = new TableRow ();
						row.Cells.Add (Utils.CreateTableCell (irc.name));
						row.Cells.Add (Utils.CreateTableCell (irc.servers));
						row.Cells.Add (Utils.CreateTableCell (irc.channels));
						row.Cells.Add (Utils.CreateTableCell (irc.nicks));
						row.Cells.Add (Utils.CreateTableCell (Utils.CreateLinkButton ("remove_irc_" + irc.id.ToString (), "Remove", "RemoveIrcIdentity", irc.id.ToString (), OnLinkButtonCommand)));
						tblIrcIdentities.Rows.AddAt (tblIrcIdentities.Rows.Count - 1, row);
					}
				}
				if (response.EmailIdentities != null) {
					foreach (DBEmailIdentity email in response.EmailIdentities) {
						row = new TableRow ();
						row.Cells.Add (Utils.CreateTableCell (email.name));
						row.Cells.Add (Utils.CreateTableCell (email.email));
						row.Cells.Add (Utils.CreateTableCell (email.password));
						row.Cells.Add (Utils.CreateTableCell (Utils.CreateLinkButton ("remove_email_" + email.id.ToString (), "Remove", "RemoveEmailIdentity", email.id.ToString (), OnLinkButtonCommand)));
						tblEmailIdentities.Rows.AddAt (tblEmailIdentities.Rows.Count - 1, row);
					}
				}
			}
		} catch (Exception ex) {
			lblMessage.Text = ex.Message;
		}
	}

	protected void OnLinkButtonCommand (object sender, CommandEventArgs e)
	{
		WebServiceResponse response = null;

		try {
			switch (e.CommandName) {
			case "RemoveIrcIdentity":
				response = Master.WebService.RemoveIdentity (Master.WebServiceLogin, int.Parse ((string) e.CommandArgument), null);
				break;
			case "RemoveEmailIdentity":
				response = Master.WebService.RemoveIdentity (Master.WebServiceLogin, null, int.Parse ((string) e.CommandArgument));
				break;
			}
			if (response != null) {
				if (response.Exception != null) {
					lblMessage.Text = response.Exception.Message;
				} else {
					Response.Redirect ("Identities.aspx", false);
				}
			}
		} catch (Exception ex) {
			lblMessage.Text = ex.Message;
		}
	}

	protected void lnkIrcAdd_Click (object sender, EventArgs e)
	{
		WebServiceResponse response;

		try {
			DBIrcIdentity irc_identity = new DBIrcIdentity ();
			irc_identity.name = txtIrcName.Text;
			irc_identity.nicks = txtIrcNicks.Text;
			irc_identity.servers = txtIrcServers.Text;
			irc_identity.channels = txtIrcChannels.Text;

			if (string.IsNullOrEmpty (irc_identity.name))
				throw new Exception ("You need to specify the name of the irc identity");
			if (string.IsNullOrEmpty (irc_identity.nicks))
				throw new Exception ("You need to specify the nick names to use for the irc identity");
			if (string.IsNullOrEmpty (irc_identity.servers))
				throw new Exception ("You need to specify the servers to use for the irc identity");
			if (string.IsNullOrEmpty (irc_identity.channels))
				throw new Exception ("You need to specify the channels to join for the irc identity");

			response = Master.WebService.EditIdentity (Master.WebServiceLogin, irc_identity, null);

			if (response.Exception != null) {
				lblMessage.Text = response.Exception.Message;
			} else {
				Response.Redirect ("Identities.aspx", false);
			}
		} catch (Exception ex) {
			lblMessage.Text = ex.Message;
		}
	}

	protected void lnkEmailAdd_Click (object sender, EventArgs e)
	{
		WebServiceResponse response;

		try {
			DBEmailIdentity email_identity = new DBEmailIdentity ();
			email_identity.name = txtEmailName.Text;
			email_identity.email = txtEmailEmail.Text;
			email_identity.password = txtEmailPassword.Text;


			if (string.IsNullOrEmpty (email_identity.name))
				throw new Exception ("You need to specify the name of the email identity");
			if (string.IsNullOrEmpty (email_identity.email))
				throw new Exception ("You need to specify the email for the email identity");
			if (string.IsNullOrEmpty (email_identity.password))
				throw new Exception ("You need to specify the password for the email identity");

			response = Master.WebService.EditIdentity (Master.WebServiceLogin, null, email_identity);

			if (response.Exception != null) {
				lblMessage.Text = response.Exception.Message;
			} else {
				Response.Redirect ("Identities.aspx", false);
			}
		} catch (Exception ex) {
			lblMessage.Text = ex.Message;
		}
	}
}
