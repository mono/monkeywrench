/*
 * EditLane.aspx.cs
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class EditLane : System.Web.UI.Page
{
	private DBLane lane;

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	private void RedirectToSelf ()
	{
		Response.Redirect ("EditLane.aspx?lane_id=" + lane.id.ToString (), false);
	}

	protected override void OnInit (EventArgs e)
	{
		base.OnInit (e);
		try {
			TableRow row;
			GetLaneForEditResponse response;

			txtID.Attributes ["readonly"] = "readonly";

			string action = Request ["action"];
			string command_id = Request ["command_id"];

			int id;
			int sequence;
			int timeout;
			
			tblCommands.Visible = true;
			tblFiles.Visible = true;

			int.TryParse (Request ["lane_id"], out id);
			response = Master.WebService.GetLaneForEdit (Master.WebServiceLogin, id, Request ["lane"]);

			lane = response.Lane;

			if (lane == null) {
				Response.Redirect ("EditLanes.aspx", false);
				return;
			}

			lblH2.Text = "Lane: " + lane.lane;
			lblDeletionDirectiveErrors.Visible = false;

			// find possible parent lanes
			lstParentLane.Items.Clear ();
			lstParentLane.Items.Add (new ListItem ("None", "0"));
			foreach (DBLane l in response.Lanes) {
				if (l.id == lane.id)
					continue;

				if (Utils.IsDescendentLaneOf (response.Lanes, lane, l, 0))
					continue; // our descendents can't be our parents too.

				lstParentLane.Items.Add (new ListItem (l.lane, l.id.ToString ()));

				if (!IsPostBack) {
					if (lane.parent_lane_id.HasValue && lane.parent_lane_id.Value == l.id)
						lstParentLane.SelectedIndex = lstParentLane.Items.Count - 1;
				}
			}

			if (!IsPostBack) {
				for (int i = 0; i < cmbSourceControl.Items.Count; i++) {
					cmbSourceControl.Items [i].Selected = lane.source_control == cmbSourceControl.Items [i].Text;
				}
				txtRepository.Text = lane.repository;
				txtCommitFilter.Text = lane.commit_filter;
				txtMinRevision.Text = lane.min_revision;
				txtMaxRevision.Text = lane.max_revision;
				txtLane.Text = lane.lane;
				txtID.Text = lane.id.ToString ();
				// find (direct) child lanes
				foreach (DBLane l in response.Lanes) {
					if (l.parent_lane_id.HasValue && l.parent_lane_id.Value == lane.id) {
						if (!string.IsNullOrEmpty (lblChildLanes.Text))
							lblChildLanes.Text += ", ";
						lblChildLanes.Text += string.Format ("<a href='EditLane.aspx?lane_id={0}'>{1}</a>", l.id, l.lane);
					}
				}
				chkTraverseMerges.Checked = lane.traverse_merge;
			}

			if (!string.IsNullOrEmpty (action)) {
				switch (action) {
				case "createFile":
					Master.WebService.CreateLanefile (Master.WebServiceLogin, lane.id, Request ["filename"]);
					break;
				case "addFile":
					if (int.TryParse (Request ["lanefile_id"], out id))
						Master.WebService.AttachFileToLane (Master.WebServiceLogin, lane.id, id);
					break;
				case "deleteFile":
					if (int.TryParse (Request ["file_id"], out id))
						Master.WebService.DeattachFileFromLane (Master.WebServiceLogin, lane.id, id);
					break;
				case "editCommandFilename":
					if (int.TryParse (command_id, out id))
						Master.WebService.EditCommandFilename (Master.WebServiceLogin, id, Request ["filename"]);
					break;
				case "editCommandSequence":
					if (int.TryParse (command_id, out id)) {
						if (int.TryParse (Request ["sequence"], out sequence))
							Master.WebService.EditCommandSequence (Master.WebServiceLogin, id, sequence);
					}
					break;
				case "editCommandArguments":
					if (int.TryParse (command_id, out id))
						Master.WebService.EditCommandArguments (Master.WebServiceLogin, id, Request ["arguments"]);
					break;
				case "editCommandTimeout":
					if (int.TryParse (command_id, out id)) {
						if (int.TryParse (Request ["timeout"], out timeout))
							Master.WebService.EditCommandTimeout (Master.WebServiceLogin, id, timeout);
					}
					break;
				case "editCommandWorkingDirectory":
					if (int.TryParse (command_id, out id))
						Master.WebService.EditCommandWorkingDirectory (Master.WebServiceLogin, id, Request ["working_directory"]);
					break;
				case "editCommandUploadFiles":
					if (int.TryParse (command_id, out id))
						Master.WebService.EditCommandUploadFiles (Master.WebServiceLogin, id, Request ["upload_files"]);
					break;
				case "deletecommand":
					if (int.TryParse (command_id, out id))
						Master.WebService.DeleteCommand (Master.WebServiceLogin, id);
					break;
				case "switchNonFatal":
					if (int.TryParse (command_id, out id))
						Master.WebService.SwitchCommandNonFatal (Master.WebServiceLogin, id);
					break;
				case "switchAlwaysExecute":
					if (int.TryParse (command_id, out id))
						Master.WebService.SwitchCommandAlwaysExecute (Master.WebServiceLogin, id);
					break;
				case "switchInternal":
					if (int.TryParse (command_id, out id))
						Master.WebService.SwitchCommandInternal (Master.WebServiceLogin, id);
					break;
				case "addCommand":
					if (!int.TryParse (Request ["sequence"], out sequence))
						sequence = -1;
					Master.WebService.AddCommand (Master.WebServiceLogin, lane.id, Request ["command"], false, false, 60, sequence);
					break;
				case "switchHostEnabled":
					if (int.TryParse (Request ["host_id"], out id))
						Master.WebService.SwitchHostEnabledForLane (Master.WebServiceLogin, lane.id, id);
					break;
				case "removeHost":
					if (int.TryParse (Request ["host_id"], out id))
						Master.WebService.RemoveHostForLane (Master.WebServiceLogin, lane.id, id);
					break;
				case "addHost":
					if (int.TryParse (Request ["host_id"], out id))
						Master.WebService.AddHostToLane (Master.WebServiceLogin, lane.id, id);
					break;
				case "addDependency":
					if (int.TryParse (Request ["dependent_lane_id"], out id)) {
						int condition;
						int host_id;
						if (int.TryParse (Request ["condition"], out condition)) {
							if (int.TryParse (Request ["dependent_host_id"], out host_id)) {
								Master.WebService.AddDependencyToLane (Master.WebServiceLogin, lane.id, id, host_id > 0 ? (Nullable<int>) host_id : (Nullable<int>) null, (DBLaneDependencyCondition) condition);
							}
						}
					}
					break;
				case "editDependencyFilename":
					if (int.TryParse (Request ["lanedependency_id"], out id))
						Master.WebService.EditLaneDependencyFilename (Master.WebServiceLogin, id, Request ["filename"]);
					break;
				case "deleteDependency":
					if (int.TryParse (Request ["dependency_id"], out id))
						Master.WebService.DeleteLaneDependency (Master.WebServiceLogin, id);
					break;
				case "editDependencyDownloads":
					if (int.TryParse (Request ["lanedependency_id"], out id))
						Master.WebService.EditLaneDependencyDownloads (Master.WebServiceLogin, id, Request ["downloads"]);
					break;
				case "editEnvironmentVariableValue": {
					int host_id, lane_id;
					if (int.TryParse (Request ["host_id"], out host_id))
						if (int.TryParse (Request ["lane_id"], out lane_id)) {
							if (int.TryParse (Request ["id"], out id)) {
								DBEnvironmentVariable ev = new DBEnvironmentVariable ();
								ev.id = id;
								ev.host_id = host_id == 0 ? (int?)null : host_id;
								ev.lane_id = lane_id == 0 ? (int?)null : lane_id;
								ev.name = Request ["name"];
								ev.value = Request ["value"];
								Master.WebService.EditEnvironmentVariable (Master.WebServiceLogin, ev);
							}
						}
					}
					break;
				case "moveCommandToParentLane": {
						if (int.TryParse (command_id, out id)) {
							if (response.Lane.parent_lane_id != null) {
								DBCommand cmd = response.Commands.Find ((v) => v.id == id);
								if (cmd != null) {
									cmd.lane_id = response.Lane.parent_lane_id.Value;
									Master.WebService.EditCommand (Master.WebServiceLogin, cmd);
								}
							}
						}
					}
					break;
				default:
					break;
				}

				RedirectToSelf ();
				return;
			}

			// Files
			var shown_files = new HashSet<int> ();
			foreach (DBLanefile file in response.Files) {
				if (shown_files.Contains (file.id))
					continue;
				shown_files.Add (file.id);

				string text = file.name;
				if (!string.IsNullOrEmpty (file.mime))
					text += " (" + file.mime + ")";

				bool is_inherited = !response.LaneFiles.Exists ((v) => v.lane_id == lane.id && v.lanefile_id == file.id);

				tblFiles.Rows.Add (Utils.CreateTableRow (
					string.Format ("<a href='EditLaneFile.aspx?lane_id={1}&amp;file_id={0}'>{2}</a>", file.id, lane.id, file.name),
					file.mime,
					(is_inherited ? string.Empty : string.Format ("<a href='EditLane.aspx?lane_id={1}&amp;action=deleteFile&amp;file_id={0}'>Delete</a> ", file.id, lane.id)) + string.Format ("<a href='ViewLaneFileHistory.aspx?id={0}'>View history</a>", file.id),
					string.Join (", ", GetLanesWhereFileIsUsed (file, response).Where ((l) => l.id != lane.id).Select ((l, s) => string.Format ("<a href='EditLane.aspx?lane_id={0}'>{1}</a>", l.id, l.lane)).ToArray ())));

				if (is_inherited)
					tblFiles.Rows [tblFiles.Rows.Count - 1].BackColor = System.Drawing.Color.LightGray;
			}
			tblFiles.Rows.Add (Utils.CreateTableRow (
				"<input type='text' value='filename' id='txtCreateFileName'></input>",
				"text/plain",
				string.Format ("<a href='javascript:createFile ({0})'>Add</a>", lane.id),
				"-"
				));
			StringBuilder existing_files = new StringBuilder ();
			existing_files.AppendLine ("<select id='cmbExistingFiles'>");
			response.ExistingFiles.Sort ((a, b) => string.Compare (a.name, b.name));
			shown_files.Clear ();
			foreach (DBLanefile file in response.ExistingFiles) {
				if (shown_files.Contains (file.id))
					continue;
				shown_files.Add (file.id);

				existing_files.AppendFormat ("<option value='{1}' title='Used in: {2}'>{0}</option>\n", file.name, file.id, string.Join (", ", GetLanesWhereFileIsUsed (file, response).Select ((l, s) => l.lane).ToArray ()));
			}
			existing_files.AppendLine ("</select>");
			tblFiles.Rows.Add (Utils.CreateTableRow (
				existing_files.ToString (),
				"N/A",
				string.Format ("<a href='javascript:addFile ({0})'>Add</a>", lane.id),
				"-"
				));
			tblFiles.Visible = true;

			// commands
			foreach (DBCommand command in response.Commands) {
				string filename = command.command;
				DBLanefile file = Utils.FindFile (response.Files, f => f.name == filename);
				if (file != null)
					filename = string.Format ("<a href='EditLaneFile.aspx?lane_id={1}&amp;file_id={0}'>{2}</a>", file.id, lane.id, file.name);

				string working_directory = "<em>modify</em>";
				if (!string.IsNullOrEmpty(command.working_directory))
				    working_directory = command.working_directory;
				string upload_files = "<em>modify</em>";
				if (!string.IsNullOrEmpty(command.upload_files))
				    upload_files = command.upload_files;

				bool is_inherited = command.lane_id != lane.id;

				tblCommands.Rows.Add (Utils.CreateTableRow (
					string.Format ("<a href='javascript:editCommandSequence ({2}, {0}, true, \"{1}\")'>{1}</a>", command.id, command.sequence, lane.id),
					filename,
					string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;command_id={3}&amp;action=switchAlwaysExecute'>{2}</a>", lane.id, (!command.alwaysexecute).ToString (), command.alwaysexecute ? "yes" : "no", command.id),
					string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;command_id={3}&amp;action=switchNonFatal'>{2}</a>", lane.id, (!command.nonfatal).ToString (), command.nonfatal ? "yes" : "no", command.id),
					string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;command_id={1}&amp;action=switchInternal'>{2}</a>", lane.id, command.id, (command.@internal ? "yes" : "no")),
					string.Format ("<a href='javascript:editCommandFilename ({2}, {0}, true, \"{1}\")'>{1}</a>", command.id, command.filename, lane.id),
					string.Format ("<a href='javascript:editCommandArguments ({2}, {0}, true, \"{1}\")'>{3}</a>", command.id, command.arguments.Replace ("\"", "\\\""), lane.id, command.arguments),
					string.Format ("<a href='javascript:editCommandTimeout ({2}, {0}, true, \"{1}\")'>{1} minutes</a>", command.id, command.timeout, lane.id),
				    string.Format ("<a href='javascript:editCommandWorkingDirectory ({2}, {0}, true, \"{1}\")'>{3}</a>", command.id, command.working_directory, lane.id, working_directory),
					string.Format ("<a href='javascript:editCommandUploadFiles ({2}, {0}, true, \"{1}\")'>{3}</a>", command.id, command.upload_files, lane.id, upload_files),
					is_inherited ? "-" : string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;action=deletecommand&amp;command_id={1}'>Delete</a>", lane.id, command.id),
					is_inherited ? string.Format ("Inherited from <a href='EditLane.aspx?lane_id={1}'>{0}</a>", response.Lanes.Find ((v) => v.id == command.lane_id).lane, command.lane_id) : (lane.parent_lane_id == null ? "-" : string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;command_id={1}&amp;action=moveCommandToParentLane'>Move</a> to parent lane", lane.id, command.id, lane.parent_lane_id.Value))));

				if (is_inherited)
					tblCommands.Rows [tblCommands.Rows.Count - 1].BackColor = System.Drawing.Color.LightGray;
			}
			tblCommands.Rows.Add (Utils.CreateTableRow (
				(response.Commands.Count * 10).ToString (),
				string.Format ("<input type='text' value='command' id='txtCreateCommand_name'></input>"),
				"no",
				"no",
				"no",
				"bash",
				"-ex {0}",
				"60 minutes",
				"-",
				"-",
				string.Format ("<a href='javascript:addCommand ({0}, {1})'>Add</a>", lane.id, response.Commands.Count > 0 ? (response.Commands [response.Commands.Count - 1].sequence + 10) : 0),
				"-"));

			// Show all the hosts
			List<string> current_hosts = new List<string> ();
			string html;

			foreach (DBHostLaneView view in response.HostLaneViews) {
				string ed = view.enabled ? "enabled" : "disabled";
				row = new TableRow ();

				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='EditHost.aspx?host_id={0}'>{1}</a>", view.host_id, view.host), view.enabled ? "enabled" : "disabled"));
				html = string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;host_id={1}&amp;action=removeHost'>Remove</a> ", lane.id, view.host_id);
				html = html + string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;host_id={1}&amp;action=switchHostEnabled'>{2}</a>", lane.id, view.host_id, (view.enabled ? "Disable" : "Enable"));
				row.Cells.Add (Utils.CreateTableCell (html, ed));
				tblHosts.Rows.Add (row);
				current_hosts.Add (view.host);
			}

			if (response.Hosts.Count != current_hosts.Count) {
				row = new TableRow ();
				html = "<select id='lstHosts'>";
				foreach (DBHost host in response.Hosts) {
					if (!current_hosts.Contains (host.host))
						html += "<option value='" + host.id + "'>" + host.host + "</option>";
				}
				html += "</select>";
				row.Cells.Add (Utils.CreateTableCell (html));
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript:addHost({0})'>Add</a>", lane.id)));
				tblHosts.Rows.Add (row);
			}

			// dependencies
			foreach (DBLaneDependency dependency in response.Dependencies) {
				row = new TableRow ();
				for (int i = 0; i < response.Lanes.Count; i++) {
					if (response.Lanes [i].id == dependency.dependent_lane_id) {
						row.Cells.Add (Utils.CreateTableCell (response.Lanes [i].lane));
						break;
					}
				}
				row.Cells.Add (Utils.CreateTableCell (dependency.Condition.ToString ()));
				row.Cells.Add (Utils.CreateTableCell (dependency.dependent_host_id.HasValue ? Utils.FindHost (response.Hosts, dependency.dependent_host_id.Value).host : "Any"));
				switch (dependency.Condition) {
				case DBLaneDependencyCondition.DependentLaneSuccessWithFile:
					row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript: editDependencyFilename ({0}, {1}, \"{2}\")'>{2}</a>", lane.id, dependency.id, string.IsNullOrEmpty (dependency.filename) ? "(edit)" : dependency.filename)));
					break;
				case DBLaneDependencyCondition.DependentLaneSuccess:
				default:
					row.Cells.Add (Utils.CreateTableCell ("-"));
					break;
				}
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript: editDependencyDownloads ({0}, {1}, \"{2}\")'>{3}</a>", lane.id, dependency.id, string.IsNullOrEmpty (dependency.download_files) ? string.Empty : dependency.download_files.Replace ("\"", "\\\""), string.IsNullOrEmpty (dependency.download_files) ? "(edit)" : HttpUtility.HtmlEncode (dependency.download_files))));
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript: deleteDependency ({0}, {1})'>Delete</a>", lane.id, dependency.id)));
				tblDependencies.Rows.Add (row);
			}
			// Create new dependency row
			row = new TableRow ();
			html = "<select id='lstDependentLanes'>";
			foreach (DBLane l in response.Lanes) {
				if (l.id == lane.id)
					continue;
				html += string.Format ("<option value='{0}'>{1}</option>", l.id, l.lane);
			}
			html += "</select>";
			row.Cells.Add (Utils.CreateTableCell (html));
			html = "<select id='lstDependencyConditions'>";
			foreach (object value in Enum.GetValues (typeof (DBLaneDependencyCondition))) {
				if ((int) value == 0)
					continue;
				html += string.Format ("<option value='{0}'>{1}</option>", (int) value, value.ToString ());
			}
			html += "</select>";
			row.Cells.Add (Utils.CreateTableCell (html));
			// host
			html = "<select id='lstDependentHosts'>";
			html += "<option value='0'>Any</option>";
			foreach (DBHost h in response.Hosts) {
				html += string.Format ("<option value='{0}'>{1}</option>", h.id, h.host);
			}
			html += "</select>";
			row.Cells.Add (Utils.CreateTableCell (html));
			row.Cells.Add (Utils.CreateTableCell (string.Empty));
			row.Cells.Add (Utils.CreateTableCell (string.Empty));
			row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript:addDependency ({0})'>Add</a>", lane.id)));
			tblDependencies.Rows.Add (row);

			// deletion directives
			foreach (DBLaneDeletionDirectiveView directive in response.LaneDeletionDirectives)
				AddDeletionDirectiveRow (directive);
			if (response.FileDeletionDirectives != null && response.FileDeletionDirectives.Count > 0) {
				foreach (DBFileDeletionDirective directive in response.FileDeletionDirectives) {
					lstDeletionDirectives2.Items.Add (new ListItem (directive.name, directive.id.ToString ()));
				}
			} else {
				rowDeletionDirectives2.Visible = false;
			}
			foreach (DBDeleteCondition condition in Enum.GetValues (typeof (DBDeleteCondition)))
				lstDeletionDirectiveCondition1.Items.Add (new ListItem (condition.ToString (), ((int) condition).ToString ()));
			foreach (DBMatchMode mode in Enum.GetValues (typeof (DBMatchMode)))
				lstDeletionDirectiveGlobs1.Items.Add (new ListItem (mode.ToString (), ((int) mode).ToString ()));

			editorVariables.Lane = response.Lane;
			editorVariables.Master = Master;
			editorVariables.Variables = response.Variables;

			// notifications
			foreach (DBLaneNotification ln in response.LaneNotifications.FindAll ((v) => v.lane_id == response.Lane.id)) {
				DBNotification notification = response.Notifications.Find ((v) => v.id == ln.notification_id);
				tblNotifications.Rows.AddAt (tblNotifications.Rows.Count - 1, Utils.CreateTableRow (Utils.CreateTableCell (notification.name), Utils.CreateTableCell (Utils.CreateLinkButton ("remove_notification_" + ln.id.ToString (), "Remove", "RemoveNotification", ln.id.ToString (), OnLinkButtonCommand))));
			}
			foreach (DBNotification notification in response.Notifications.FindAll ((v) => !response.LaneNotifications.Exists ((ln) => ln.notification_id == v.id && ln.lane_id == response.Lane.id))) {
				cmbNotifications.Items.Add (new ListItem (notification.name, notification.id.ToString ()));
			}
		} catch (Exception ex) {
			lblMessage.Text = ex.ToString ().Replace ("\n", "<br/>");
		}
	}

	IEnumerable<DBLane> GetLanesWhereFileIsUsed (DBLanefile file, GetLaneForEditResponse response)
	{
		IEnumerable<DBLanefiles> lane_files = response.LaneFiles.Where ((v) => v.lanefile_id == file.id);
		IEnumerable<DBLane> lanes = response.Lanes.Where ((v) => lane_files.FirstOrDefault ((v2) => v2.lane_id == v.id) != null);
		return lanes;
	}

	private void AddDeletionDirectiveRow (DBLaneDeletionDirectiveView directive)
	{
		TableRow row;
		int index = tblDeletionDirective.Rows.Count - 2;
		row = Utils.CreateTableRow (
			directive.name,
			directive.filename,
			((DBMatchMode) directive.match_mode).ToString (),
			((DBDeleteCondition) directive.condition).ToString (),
			directive.x.ToString (),
			Utils.CreateLinkButton ("lnkEnableDirective" + directive.id.ToString (), directive.enabled ? "True" : "False", directive.enabled ? "disableDeletionDirective" : "enableDeletionDirective", directive.id.ToString (), btn_Command),
			Utils.CreateLinkButton ("lnkUnlinkDirective" + directive.id.ToString (), "Unlink", "unlinkDeletionDirective", directive.id.ToString () + ";" + index.ToString (), btn_Command),
			Utils.CreateLinkButton ("lnkDeleteDirective" + directive.id.ToString (), "Delete", "deleteDeletionDirective", directive.file_deletion_directive_id.ToString () + ";" + directive.id + ";" + index.ToString (), btn_Command)
			);
		tblDeletionDirective.Rows.AddAt (index, row);
	}

	private void DumpArguments ()
	{
		Console.WriteLine ("> ARGUMENT DUMP");
		foreach (string obj in Request.Params.AllKeys)
			Console.WriteLine (">> '{0}' = '{1}'", obj, Request [obj]);
		Console.WriteLine ("> ARGUMENT DUMP END");

	}

	public void btn_Command (object sender, CommandEventArgs e)
	{
		//	Console.WriteLine ("EditLane, got command '{0}' with arguments '{1}'", e.CommandName, e.CommandArgument);
		int lane_deletion_directive_id;

		switch (e.CommandName) {
		case "addDeletionDirective":
			switch ((string) e.CommandArgument) {
			case "1": {
					string description = txtDeletionDirective1.Text;
					string filename = txtDeletionDirectiveFilename1.Text;

					lblDeletionDirectiveErrors.Visible = false;
					try {
						if (!string.IsNullOrEmpty (filename) && !string.IsNullOrEmpty (description)) {
							int directive_id = Master.WebService.AddFileDeletionDirective (Master.WebServiceLogin, filename, description,
								(DBMatchMode) int.Parse (lstDeletionDirectiveGlobs1.SelectedValue),
								int.Parse (txtDeletionDirectiveX1.Text),
								(DBDeleteCondition) int.Parse (lstDeletionDirectiveCondition1.SelectedValue));

							lane_deletion_directive_id = Master.WebService.AddLaneDeletionDirective (Master.WebServiceLogin, directive_id, lane.id);

							AddDeletionDirectiveRow (Master.WebService.FindLaneDeletionDirective (Master.WebServiceLogin, directive_id, lane_deletion_directive_id));
						}
					} catch (Exception ex) {
						lblDeletionDirectiveErrors.Text = ex.Message;
						lblDeletionDirectiveErrors.Visible = true;
					}
					break;
				}
			case "2": {
					int directive_id;

					if (int.TryParse (lstDeletionDirectives2.SelectedValue, out directive_id)) {
						lblDeletionDirectiveErrors.Visible = false;
						try {
							lane_deletion_directive_id = Master.WebService.AddLaneDeletionDirective (Master.WebServiceLogin, directive_id, lane.id);
							AddDeletionDirectiveRow (Master.WebService.FindLaneDeletionDirective (Master.WebServiceLogin, directive_id, lane_deletion_directive_id));
						} catch (Exception ex) {
							lblDeletionDirectiveErrors.Text = ex.Message;
							lblDeletionDirectiveErrors.Visible = true;
						}
					}

					break;
				}
			}
			break;
		case "disableDeletionDirective": {
				int id;

				if (int.TryParse ((string) e.CommandArgument, out id)) {
					Master.WebService.EnableDeletionDirective (Master.WebServiceLogin, id, false);
					RedirectToSelf (); // This is needed, otherwise hitting F5 will flap the enabled state
				}
				break;
			}
		case "enableDeletionDirective": {
				int id;

				if (int.TryParse ((string) e.CommandArgument, out id)) {
					Master.WebService.EnableDeletionDirective (Master.WebServiceLogin, id, true);
					RedirectToSelf (); // This is needed, otherwise hitting F5 will flap the enabled state
				}
				break;
			}
		case "deleteDeletionDirective": {
				int file_directive_id, id, index;

				string [] args = ((string) e.CommandArgument).Split (';');

				if (int.TryParse (args [0], out file_directive_id)) {
					if (int.TryParse (args [1], out id)) {
						if (int.TryParse (args [2], out index)) {
							// todo
							try {
								Master.WebService.DeleteDeletionDirective (Master.WebServiceLogin, id, file_directive_id);
								tblDeletionDirective.Rows.RemoveAt (index);
								for (int i = 0; i < lstDeletionDirectives2.Items.Count - 1; i++) {
									if (lstDeletionDirectives2.Items [i].Value == file_directive_id.ToString ()) {
										lstDeletionDirectives2.Items.RemoveAt (i);
										break;
									}
								}
							} catch (Exception ex) {
								lblDeletionDirectiveErrors.Text = ex.Message;
								lblDeletionDirectiveErrors.Visible = true;
							}
						}
					}
				}
				break;
			}
		case "unlinkDeletionDirective": {
				int id, index;

				string [] args = ((string) e.CommandArgument).Split (';');

				if (int.TryParse (args [0], out id)) {
					if (int.TryParse (args [1], out index)) {
						Master.WebService.UnlinkDeletionDirective (Master.WebServiceLogin, id);
						tblDeletionDirective.Rows.RemoveAt (index);
					}
				}
				break;
			}
		default:
			Console.WriteLine ("EditLane: unknown command '{0}' '{1}'", e.CommandName, e.CommandArgument);
			break;
		}
	}

	protected void cmdSave_Click (object sender, EventArgs e)
	{
		string str_lane = txtID.Text;
		int lane_id;
		int? parent_lane_id = null;
		DBLane lane;

		if (!int.TryParse (str_lane, out lane_id))
			return;

		Logger.Log ("lstParentLane: {0}", lstParentLane.SelectedValue);
		if (!string.IsNullOrEmpty (lstParentLane.SelectedValue))
			parent_lane_id = int.Parse (lstParentLane.SelectedValue);

		lane = new DBLane ();
		lane.id = lane_id;
		lane.lane = txtLane.Text;
		lane.max_revision = txtMaxRevision.Text;
		lane.min_revision = txtMinRevision.Text;
		lane.repository = txtRepository.Text;
		lane.commit_filter = txtCommitFilter.Text;
		lane.source_control = cmbSourceControl.Text;
		lane.parent_lane_id = (parent_lane_id.HasValue && parent_lane_id.Value != 0) ? parent_lane_id : null;
		lane.traverse_merge = chkTraverseMerges.Checked;
		Master.WebService.EditLane (Master.WebServiceLogin, lane);
		RedirectToSelf ();
	}

	protected void lnkAddNotification_Click (object sender, EventArgs e)
	{
		WebServiceResponse response;

		try {
			response = null;

			response = Master.WebService.AddLaneNotification (Master.WebServiceLogin, lane.id, int.Parse (cmbNotifications.SelectedItem.Value));
			if (response.Exception != null) {
				lblMessage.Text = response.Exception.Message;
			} else {
				RedirectToSelf ();
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
				response = Master.WebService.RemoveLaneNotification (Master.WebServiceLogin, int.Parse ((string) e.CommandArgument));
				if (response.Exception != null) {
					lblMessage.Text = response.Exception.Message;
				} else {
					RedirectToSelf ();
				}
				break;
			}
		} catch (Exception ex) {
			lblMessage.Text = ex.Message;
		}
	}

	protected void cmdDeleteAllWork_Click (object sender, EventArgs e)
	{
		try {
			Response.Redirect ("Delete.aspx?action=delete-all-work-for-lane&lane_id=" + lane.id.ToString (), false);
		} catch (Exception ex) {
			lblMessage.Text = ex.Message;
		}
	}

	protected void cmdClearAllWork_Click (object sender, EventArgs e)
	{
		try {
			Response.Redirect ("Delete.aspx?action=clear-all-work-for-lane&lane_id=" + lane.id.ToString (), false);
		} catch (Exception ex) {
			lblMessage.Text = ex.Message;
		}
	}

	protected void cmdDeleteAllRevisions_Click (object sender, EventArgs e)
	{
		try {
			Response.Redirect ("Delete.aspx?action=delete-all-revisions-for-lane&lane_id=" + lane.id.ToString (), false);
		} catch (Exception ex) {
			lblMessage.Text = ex.Message;
		}
	}
}
