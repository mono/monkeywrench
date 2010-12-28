/*
 * Notifications.aspx.cs
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

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class Notifications : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected override void OnInit (EventArgs e)
	{
		base.OnInit (e);
	
		TableRow row;
		GetNotificationsResponse response;

		try {

			foreach (WebControl tb in new WebControl [] { txtName, cmbIdentity, cmbMode, cmbNotificationType }) {
				tb.Attributes.Add ("onfocus", "javascript: document.getElementById ('lblHelp').innerHTML = '" + tb.ToolTip.Replace ("'", "\\'").Replace ("\n", "<br/>").Replace ("\r", "<br />") + "';");
			}

			response = Master.WebService.GetNotifications (Master.WebServiceLogin);

			if (response.Exception != null) {
				lblMessage.Text = response.Exception.Message;
			} else {
				if (response.IrcIdentities != null) {
					foreach (DBIrcIdentity irc in response.IrcIdentities) {
						cmbIdentity.Items.Add (new ListItem ("IRC: " + irc.name, irc.id.ToString ()));
					}
				}
				if (response.EmailIdentities != null) {
					foreach (DBEmailIdentity email in response.EmailIdentities) {
						cmbIdentity.Items.Add (new ListItem ("Email: " + email.name, email.id.ToString ()));
					}
				}
				if (response.Notifications != null) {
					foreach (DBNotification notification in response.Notifications) {
						row = new TableRow ();
						row.Cells.Add (Utils.CreateTableCell (notification.name));
						if (notification.ircidentity_id.HasValue) {
							row.Cells.Add (Utils.CreateTableCell (response.IrcIdentities.Find ((v) => v.id == notification.ircidentity_id.Value).name));
							row.Cells.Add (Utils.CreateTableCell ("IRC"));
						} else if (notification.emailidentity_id.HasValue) {
							row.Cells.Add (Utils.CreateTableCell (response.EmailIdentities.Find ((v) => v.id == notification.emailidentity_id.Value).name));
							row.Cells.Add (Utils.CreateTableCell ("Email"));
						} else {
							row.Cells.Add (Utils.CreateTableCell ("?"));
							row.Cells.Add (Utils.CreateTableCell ("?"));
						}
						row.Cells.Add (Utils.CreateTableCell (cmbMode.Items [notification.mode].Text));
						row.Cells.Add (Utils.CreateTableCell (cmbNotificationType.Items [notification.type].Text));
						row.Cells.Add (Utils.CreateTableCell (Utils.CreateLinkButton ("remove_notification_" + notification.id.ToString (), "Remove", "RemoveNotification", notification.id.ToString (), OnLinkButtonCommand)));
						tblNotifications.Rows.AddAt (tblNotifications.Rows.Count - 1, row);
					}
				}
			}
		} catch (Exception ex) {
			lblMessage.Text = ex.Message;
		}
	}

	protected void OnLinkButtonCommand (object sender, CommandEventArgs e)
	{
		WebServiceResponse response;

		try {
			switch (e.CommandName) {
			case "RemoveNotification":
				response = Master.WebService.RemoveNotification (Master.WebServiceLogin, int.Parse ((string) e.CommandArgument));
				if (response.Exception != null) {
					lblMessage.Text = response.Exception.Message;
				} else {
					Response.Redirect ("Notifications.aspx", false);
				}
				break;
			}
		} catch (Exception ex) {
			lblMessage.Text = ex.Message;
		}
	}

	protected void lnkAdd_Click (object sender, EventArgs e)
	{
		WebServiceResponse response;

		try {
			DBNotification notification = new DBNotification ();
			notification.mode = int.Parse (cmbMode.SelectedValue);
			notification.name = txtName.Text;
			notification.type = int.Parse (cmbNotificationType.SelectedValue);
			if (cmbIdentity.SelectedItem.Text.StartsWith ("IRC: ")) {
				notification.ircidentity_id = int.Parse (cmbIdentity.SelectedItem.Value);
			} else if (cmbIdentity.SelectedItem.Text.StartsWith ("Email: ")) {
				notification.emailidentity_id = int.Parse (cmbIdentity.SelectedItem.Value);
			}

			if (string.IsNullOrEmpty (notification.name))
				throw new Exception ("You need to specify the name of the notification");

			response = Master.WebService.EditNotification (Master.WebServiceLogin, notification);

			if (response.Exception != null) {
				lblMessage.Text = response.Exception.Message;
			} else {
				Response.Redirect ("Notifications.aspx", false);
			}
		} catch (Exception ex) {
			lblMessage.Text = ex.Message;
		}
	}
}
