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
using System.Text.RegularExpressions;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class Notifications : System.Web.UI.Page
{
	private static readonly Regex IDENT_RE = new Regex(@"^([a-zA-Z]+),([0-9]+)$");

	private struct NotificationEntry
	{
		public int id {get; set;}
		public string name {get; set;}
		public string identName {get; set;}
		public string identType {get; set;}
		public string mode {get; set;}
		public string type {get; set;}
	}

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected override void OnInit (EventArgs e)
	{
		base.OnInit (e);

		foreach (WebControl tb in new WebControl [] { txtName, cmbIdentity, cmbMode, cmbNotificationType }) {
			tb.Attributes.Add ("onfocus", "javascript: document.getElementById ('lblHelp').innerHTML = '" + tb.ToolTip.Replace ("'", "\\'").Replace ("\n", "<br/>").Replace ("\r", "<br />") + "';");
		}

		var notifications = new List<NotificationEntry> ();

		using (var db = new DB ())
		using (var cmd = db.CreateCommand (@"
			SELECT notification.id, notification.name,
				COALESCE(irc.name, email.name, gh.name) AS identname,
				COALESCE(
					CASE WHEN irc.id IS NULL THEN NULL ELSE 'IRC' END,
					CASE WHEN email.id IS NULL THEN NULL ELSE 'Email' END,
					CASE WHEN gh.id IS NULL THEN NULL ELSE 'GitHub' END
				) AS identtype,
				notification.mode,
				notification.type
			FROM notification
			LEFT OUTER JOIN ircidentity AS irc ON irc.id = notification.ircidentity_id
			LEFT OUTER JOIN emailidentity AS email ON email.id = notification.emailidentity_id
			LEFT OUTER JOIN githubidentity AS gh ON gh.id = notification.githubidentity_id
			ORDER BY notification.name;

			SELECT 'IRC: ' || name AS name, 'IRC,' || id AS id
			FROM ircidentity
			UNION ALL
			SELECT 'Email: ' || name AS name, 'Email,' || id AS id
			FROM emailidentity
			UNION ALL
			SELECT 'GitHub: ' || name AS name, 'GitHub,' || id AS id
			FROM githubidentity

			ORDER BY name;
		"))
		using (var reader = cmd.ExecuteReader ()) {
			while (reader.Read ()) {
				var entry = new NotificationEntry ();
				entry.id = reader.GetInt32 (0);
				entry.name = reader.GetString (1);
				entry.identName = reader.GetString (2);
				entry.identType = reader.GetString (3);
				entry.mode = ((DBNotificationMode)reader.GetInt32 (4)).ToString ();
				entry.type = ((DBNotificationType)reader.GetInt32 (5)).ToString ();

				notifications.Add (entry);
			}

			reader.NextResult ();

			while (reader.Read ()) {
				cmbIdentity.Items.Add (new ListItem (reader.GetString (0), reader.GetString (1)));
			}
		}

		notificationsRepeater.DataSource = notifications;
		notificationsRepeater.DataBind ();
	}

	protected void lnkAdd_Click (object sender, EventArgs e)
	{
		DBNotification notification = new DBNotification ();
		WebServiceResponse response;

		try {
			notification.mode = int.Parse (cmbMode.SelectedValue);
			notification.name = txtName.Text;
			notification.type = int.Parse (cmbNotificationType.SelectedValue);
		} catch (FormatException) {
			lblMessage.Text = "Invalid number";
			return;
		} catch (OverflowException) {
			lblMessage.Text = "Invalid number";
			return;
		}

		try {
			var match = IDENT_RE.Match (cmbIdentity.SelectedItem.Value);
			if (!match.Success)
				throw new ValidationException ("Invalid identity");

			var type = match.Groups[1].Value;
			var id = int.Parse(match.Groups[2].Value);

			if (type == "IRC")
				notification.ircidentity_id = id;
			else if (type == "Email")
				notification.emailidentity_id = id;
			else if (type == "GitHub")
				notification.githubidentity_id = id;
			else
				throw new ValidationException ("Invalid identity");

			if (string.IsNullOrEmpty (notification.name))
				throw new ValidationException ("You need to specify the name of the notification");

			response = Master.WebService.EditNotification (Master.WebServiceLogin, notification);
		} catch (ValidationException ex) {
			lblMessage.Text = ex.Message;
			return;
		}

		if (response.Exception != null) {
			lblMessage.Text = response.Exception.Message;
		} else {
			Response.Redirect ("Notifications.aspx", false);
		}
	}

	protected void notificationRemove_remove (object sender, CommandEventArgs e) {
		using (var db = new DB ())
		using (var cmd = db.CreateCommand (@"
			DELETE FROM notification WHERE id = @id;
		")) {
			DB.CreateParameter (cmd, "id", int.Parse (e.CommandArgument as string));

			cmd.ExecuteNonQuery ();
		}

		Response.Redirect ("Notifications.aspx", false);
	}
}
