/*
 * EditHost.aspx.cs
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

public partial class EditHost : System.Web.UI.Page
{
	GetHostForEditResponse response;

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected override void OnInit (EventArgs e)
	{
		base.OnInit (e);

		TableRow row;
		string html;
		string page;
		List<string> current_lanes = new List<string> ();
		List<DBLane> all_lanes;

		try {
			if (!Utils.IsInRole (MonkeyWrench.DataClasses.Logic.Roles.Administrator)) {
				Response.Redirect ("index.aspx", false);
				return;
			}

			string disable = Request ["disablelane"];
			string enable = Request ["enablelane"];
			string remove = Request ["removelane"];
			string add = Request ["addlane"];
			string action = Request ["action"];
			int id;
			bool redirect = false;

			response = Master.WebService.GetHostForEdit (Master.WebServiceLogin, Utils.TryParseInt32 (Request ["host_id"]), Request ["host"]);

			if (response == null || response.Host == null) {
				Response.Redirect ("EditHosts.aspx", false);
				return;
			}

			if (!IsPostBack) {
				switch (action) {
				case "editEnvironmentVariableValue":
					if (int.TryParse (Request ["id"], out id)) {
						foreach (DBEnvironmentVariable ev in response.Variables) {
							if (ev.id == id) {
								ev.value = Request ["value"];
								Master.WebService.EditEnvironmentVariable (Master.WebServiceLogin, ev);
								break;
							}
						}
					}
					redirect = true;
					break;
				}

				if (!string.IsNullOrEmpty (disable) && int.TryParse (disable, out id)) {
					Master.WebService.SwitchHostEnabledForLane (Master.WebServiceLogin, id, response.Host.id);
					redirect = true;
				}

				if (!string.IsNullOrEmpty (enable) && int.TryParse (enable, out id)) {
					Master.WebService.SwitchHostEnabledForLane (Master.WebServiceLogin, id, response.Host.id);
					redirect = true;
				}

				if (!string.IsNullOrEmpty (remove) && int.TryParse (remove, out id)) {
					Master.WebService.RemoveHostForLane (Master.WebServiceLogin, id, response.Host.id);
					redirect = true;
				}

				if (!string.IsNullOrEmpty (add) && int.TryParse (add, out id)) {
					Master.WebService.AddHostToLane (Master.WebServiceLogin, id, response.Host.id);
					redirect = true;
				}
				if (redirect) {
					Response.Redirect ("EditHost.aspx?host_id=" + response.Host.id.ToString (), false);
					return;
				}

				txtID.Text = response.Host.id.ToString ();
				txtArchitecture.Text = response.Host.architecture;
				txtDescription.Text = response.Host.description;
				txtHost.Text = response.Host.host;
				chkEnabled.Checked = response.Host.enabled;
				cmbQueueManagement.SelectedIndex = response.Host.queuemanagement;
				if (response.Person == null) {
					string valid_chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMOPQRSUVWXYZ123456789\\!\"·$%/()=?^*+,.;:-_+";
					System.Text.StringBuilder builder = new System.Text.StringBuilder ();
					Random random = new Random ();
					for (int i = 0; i < 64; i++)
						builder.Append ((char) valid_chars [random.Next (valid_chars.Length)]);
					txtPassword.Text = builder.ToString ();

					TableRow warning = Utils.CreateTableRow ("A password for the host has been generated, please hit Save to save it.");
					warning.Cells [0].ColumnSpan = 2;
					warning.Cells [0].ForeColor = System.Drawing.Color.Tomato;
					warning.Cells [0].HorizontalAlign = HorizontalAlign.Center;
					tblData.Rows.AddAt (tblData.Rows.Count - 1, warning);
				} else {
					txtPassword.Text = response.Person.password;
				}

			}

			editorVariables.Host = response.Host;
			editorVariables.Variables = response.Variables;
			editorVariables.Master = Master;

			foreach (DBHostLaneView view in response.HostLaneViews) {
				string ed = view.enabled ? "enabled" : "disabled";
				row = new TableRow ();
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='EditLane.aspx?lane_id={0}'>{1}</a>", view.lane_id, view.lane), view.enabled ? "enabled" : "disabled"));
				page = "EditHost.aspx?host_id=" + response.Host.id.ToString ();
				html = "<a href='" + page + "&amp;removelane=" + view.lane_id.ToString () + "'>Remove</a> ";
				row.Cells.Add (Utils.CreateTableCell (html, ed));

				html = "<a href='" + page + "&amp;" + (view.enabled ? "disable" : "enable") + "lane=" + view.lane_id.ToString () + "'>" + (view.enabled ? "Disable" : "Enable") + "</a>";
				row.Cells.Add (Utils.CreateTableCell (html, ed));
				tblLanes.Rows.Add (row);
				current_lanes.Add (view.lane);
			}

			all_lanes = response.Lanes;
			if (all_lanes.Count != current_lanes.Count) {
				row = new TableRow ();
				html = "<select id='addhostlane'>";
				foreach (DBLane lane in all_lanes) {
					if (!current_lanes.Contains (lane.lane))
						html += "<option value='" + lane.id + "'>" + lane.lane + "</option>";
				}
				html += "</select>";
				row.Cells.Add (Utils.CreateTableCell (html));
				row.Cells.Add (Utils.CreateTableCell ("<a href='javascript:addLane()'>Add</a>"));
				row.Cells.Add (Utils.CreateTableCell ("-"));
				tblLanes.Rows.Add (row);
			}

			foreach (DBHost host in response.Hosts) {
				if (host.id == response.Host.id)
					continue; // don't add self
				if (response.MasterHosts.Find (d => host.id == d.id) != null)
					continue; // don't add master hosts already added
				if (response.SlaveHosts.Find (d => host.id == d.id) != null)
					continue; // don't add any slave hosts, circular references is never a good thing
				cmbMasterHosts.Items.Add (new ListItem (host.host, host.id.ToString ()));
			}
			foreach (DBHost host in response.MasterHosts) {
				tblMasters.Rows.AddAt (1, Utils.CreateTableRow (
					string.Format ("<a href='EditHost.aspx?host_id={0}'>{1}</a>", host.id, host.host),
					Utils.CreateLinkButton ("removeMasterHostLinkButton_" + host.id.ToString (), "Remove", "RemoveMasterHost", host.id.ToString (), OnLinkButtonCommand)));
			}
			foreach (DBHost host in response.SlaveHosts) {
				tblSlaves.Rows.AddAt (1, Utils.CreateTableRow (
					string.Format ("<a href='EditHost.aspx?host_id={0}'>{1}</a>", host.id, host.host),
					Utils.CreateLinkButton ("removeSaveHostLinkButton_" + host.id.ToString (), "Remove", "RemoveSlaveHost", host.id.ToString (), OnLinkButtonCommand)));
			}

			lblConfiguration.Text = string.Format (@"
<MonkeyWrench Version='2'>
	<Configuration>
		<WebServiceUrl>http://{0}/WebServices/</WebServiceUrl>
		<WebServicePassword>{1}</WebServicePassword>
		<Host>{2}</Host>
		<DataDirectory>[FULL PATH TO HOME DIRECTORY, NOT ~]/moonbuilder/data</DataDirectory>
		<LockingAlgorithm>fileexistence</LockingAlgorithm>
	</Configuration>
</MonkeyWrench>
", Request.Url.Host + (Request.Url.Port != 0 && Request.Url.Port != 80 ? ":" + Request.Url.Port.ToString () : ""), txtPassword.Text, txtHost.Text);
			lblConfiguration.Text = "<pre>" + HttpUtility.HtmlEncode (lblConfiguration.Text) + "</pre>";
		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}

	protected void OnLinkButtonCommand (object sender, CommandEventArgs e)
	{
		switch (e.CommandName) {
		case "RemoveMasterHost":
			Master.WebService.RemoveMasterHost (Master.WebServiceLogin, response.Host.id, int.Parse ((string) e.CommandArgument));
			break;
		case "AddMasterHost":
			Master.WebService.AddMasterHost (Master.WebServiceLogin, response.Host.id, int.Parse (cmbMasterHosts.SelectedValue));
			break;
		case "RemoveSlaveHost":
			Master.WebService.RemoveMasterHost (Master.WebServiceLogin, int.Parse ((string) e.CommandArgument), response.Host.id);
			break;
		}
		Response.Redirect ("EditHost.aspx?host_id=" + response.Host.id.ToString (), false);
	}

	protected void cmdSave_Click (object sender, EventArgs e)
	{
		if (!Utils.IsInRole (MonkeyWrench.DataClasses.Logic.Roles.Administrator)) {
			Response.Redirect ("index.aspx", false);
			return;
		}

		DBHost host = response.Host;
		host.host = txtHost.Text;
		host.architecture = txtArchitecture.Text;
		host.description = txtDescription.Text;
		host.queuemanagement = cmbQueueManagement.SelectedIndex;
		host.enabled = chkEnabled.Checked;
		Master.WebService.EditHostWithPassword (Master.WebServiceLogin, host, txtPassword.Text);
	}

}
