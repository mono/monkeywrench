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

		TableRow row;
		GetLaneForEditResponse response;

		txtID.Attributes ["readonly"] = "readonly";

		string action = Request ["action"];
		string command_id = Request ["command_id"];

		int id;
		int sequence;
		int timeout;
		int? deadlock_timeout;
		
		tblCommands.Visible = true;
		tblFiles.Visible = true;

		int.TryParse (Request ["lane_id"], out id);
		response = Utils.LocalWebService.GetLaneForEdit (Master.WebServiceLogin, id, Request ["lane"]);

		lane = response.Lane;

		if (lane == null) {
			Response.Redirect ("EditLanes.aspx", false);
			return;
		}

		lblH2.Text = "Lane: " + lane.lane;
//			lblDeletionDirectiveErrors.Visible = false;

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

		lstPriority.SelectedIndex = Int32.Parse (lstPriority.Items.FindByValue (response.Lane.priority.ToString ()).Value);

		if (!IsPostBack) {
			for (int i = 0; i < cmbSourceControl.Items.Count; i++) {
				cmbSourceControl.Items [i].Selected = lane.source_control == cmbSourceControl.Items [i].Text;
			}
			txtRepository.Text = lane.repository;
			txtCommitFilter.Text = lane.commit_filter;
			txtMinRevision.Text = lane.min_revision;
			txtMaxRevision.Text = lane.max_revision;
			if (response.Tags != null)
				txtTags.Text = string.Join (",", response.Tags.ConvertAll<string> ((DBLaneTag tag) => tag.tag).ToArray ());
			if (response.Lane.additional_roles != null)
				txtRoles.Text = response.Lane.additional_roles;
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
			chkEnabled.Checked = lane.enabled;
			chkProtected.Checked = lane.is_protected;
		}

		if (!string.IsNullOrEmpty (action)) {
			switch (action) {
			case "createFile":
				Utils.LocalWebService.CreateLanefile (Master.WebServiceLogin, lane.id, Request ["filename"]);
				break;
			case "addFile":
				if (int.TryParse (Request ["lanefile_id"], out id))
					Utils.LocalWebService.AttachFileToLane (Master.WebServiceLogin, lane.id, id);
				break;
			case "deleteFile":
				if (int.TryParse (Request ["file_id"], out id))
					Utils.LocalWebService.DeattachFileFromLane (Master.WebServiceLogin, lane.id, id);
				break;
			case "editCommandFilename":
				if (int.TryParse (command_id, out id))
					Utils.LocalWebService.EditCommandFilenameInLane (Master.WebServiceLogin, lane.id, id, Request ["filename"]);
				break;
			case "editCommandSequence":
				if (int.TryParse (command_id, out id)) {
					if (int.TryParse (Request ["sequence"], out sequence))
						Utils.LocalWebService.EditCommandSequenceInLane (Master.WebServiceLogin, lane.id, id, sequence);
				}
				break;
			case "editCommandArguments":
				if (int.TryParse (command_id, out id))
					Utils.LocalWebService.EditCommandArgumentsInLane (Master.WebServiceLogin, lane.id, id, Request ["arguments"]);
				break;
			case "editCommandTimeout":
				if (int.TryParse (command_id, out id)) {
					if (int.TryParse (Request ["timeout"], out timeout))
						Utils.LocalWebService.EditCommandTimeoutInLane (Master.WebServiceLogin, lane.id, id, timeout);
				}
				break;
			case "editCommandDeadlockTimeout":
				if (int.TryParse (command_id, out id) && Request.Params.AllKeys.Contains ("deadlock_timeout")) {
					if (string.IsNullOrEmpty (Request ["deadlock_timeout"])) {
						deadlock_timeout = null;
					} else if (int.TryParse (Request ["deadlock_timeout"], out timeout)) {
						deadlock_timeout = timeout;
					} else {
						break;
					}
						
					Utils.LocalWebService.EditCommandDeadlockTimeoutInLane (Master.WebServiceLogin, lane.id, id, deadlock_timeout);
				}
				break;
			case "editCommandWorkingDirectory":
				if (int.TryParse (command_id, out id))
					Utils.LocalWebService.EditCommandWorkingDirectoryInLane (Master.WebServiceLogin, lane.id, id, Request ["working_directory"]);
				break;
			case "editCommandUploadFiles":
				if (int.TryParse (command_id, out id))
					Utils.LocalWebService.EditCommandUploadFilesInLane (Master.WebServiceLogin, lane.id, id, Request ["upload_files"]);
				break;
			case "deletecommand":
				if (int.TryParse (command_id, out id))
					Utils.LocalWebService.DeleteCommandInLane (Master.WebServiceLogin, lane.id, id);
				break;
			case "switchNonFatal":
				if (int.TryParse (command_id, out id))
					Utils.LocalWebService.SwitchCommandNonFatalInLane (Master.WebServiceLogin, lane.id, id);
				break;
			case "switchAlwaysExecute":
				if (int.TryParse (command_id, out id))
					Utils.LocalWebService.SwitchCommandAlwaysExecuteInLane (Master.WebServiceLogin, lane.id, id);
				break;
			case "switchInternal":
				if (int.TryParse (command_id, out id))
					Utils.LocalWebService.SwitchCommandInternalInLane (Master.WebServiceLogin, lane.id, id);
				break;
			case "switchTimestamp":
				if (int.TryParse (command_id, out id))
					Utils.LocalWebService.SwitchCommandTimestampInLane (Master.WebServiceLogin, lane.id, id);
				break;
			case "addCommand":
				if (!int.TryParse (Request ["sequence"], out sequence))
					sequence = -1;
				Utils.LocalWebService.AddCommand (Master.WebServiceLogin, lane.id, Request ["command"], false, false, 60, sequence);
				break;
			case "switchHostEnabled":
				if (int.TryParse (Request ["host_id"], out id))
					Utils.LocalWebService.SwitchHostEnabledForLane (Master.WebServiceLogin, lane.id, id);
				break;
			case "switchHostHidden":
				if (int.TryParse (Request ["host_id"], out id))
					Utils.LocalWebService.SwitchHostHiddenForLane (Master.WebServiceLogin, lane.id, id);
				break;
			case "removeHost":
				if (int.TryParse (Request ["host_id"], out id))
					Utils.LocalWebService.RemoveHostForLane (Master.WebServiceLogin, lane.id, id);
				break;
			case "addHost":
				if (int.TryParse (Request ["host_id"], out id))
					Utils.LocalWebService.AddHostToLane (Master.WebServiceLogin, lane.id, id);
				break;
			case "addDependency":
				if (int.TryParse (Request ["dependent_lane_id"], out id)) {
					int condition;
					int host_id;
					if (int.TryParse (Request ["condition"], out condition)) {
						if (int.TryParse (Request ["dependent_host_id"], out host_id)) {

							var hostid = host_id > 0 ? (Nullable<int>) host_id : (Nullable<int>) null;
							Utils.LocalWebService.AddDependencyToLane (Master.WebServiceLogin, lane.id, id, hostid, (DBLaneDependencyCondition) condition);
						}
					}
				}
				break;
			case "editDependencyFilename":
				if (int.TryParse (Request ["lanedependency_id"], out id))
					Utils.LocalWebService.EditLaneDependencyFilenameInLane (Master.WebServiceLogin, lane.id, id, Request ["filename"]);
				break;
			case "deleteDependency":
				if (int.TryParse (Request ["dependency_id"], out id))
					Utils.LocalWebService.DeleteLaneDependencyInLane (Master.WebServiceLogin, lane.id, id);
				break;
			case "editDependencyDownloads":
				if (int.TryParse (Request ["lanedependency_id"], out id))
					Utils.LocalWebService.EditLaneDependencyDownloadsInLane (Master.WebServiceLogin, lane.id, id, Request ["downloads"]);
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
							Utils.LocalWebService.EditEnvironmentVariable (Master.WebServiceLogin, ev);
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
								Utils.LocalWebService.EditCommandInLane (Master.WebServiceLogin, cmd, lane.id);
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

			var file_used_list = GetLanesWhereFileIsUsed (file, response).Where ((l) => l.id != lane.id).Select ((l, s) => string.Format ("<a href='EditLane.aspx?lane_id={0}'>{1}</a>", l.id, l.lane)).ToArray ();
			var file_list = string.Format (
@"
<div id=""hideFileList{2}"" style=""display: none;""  onclick=""document.getElementById('fileList{2}').style.display = 'none';  document.getElementById('hideFileList{2}').style.display = 'none';  document.getElementById('showFileList{2}').style.display = 'block';""  >[Hide]</div>
<div id=""showFileList{2}"" style=""display: block;"" onclick=""document.getElementById('fileList{2}').style.display = 'block'; document.getElementById('hideFileList{2}').style.display = 'block'; document.getElementById('showFileList{2}').style.display = 'none' ; "" >Used by {0} other lanes [Show]</div>
<div id=""fileList{2}"" style=""display: none;"">{1}</div>
", file_used_list.Length, string.Join (", ", file_used_list), file.id);

			tblFiles.Rows.Add (Utils.CreateTableRow (
				string.Format ("<a href='EditLaneFile.aspx?lane_id={1}&amp;file_id={0}'>{2}</a>", file.id, lane.id, file.name),
				file.mime,
				(is_inherited ? string.Empty : string.Format ("<a href='EditLane.aspx?lane_id={1}&amp;action=deleteFile&amp;file_id={0}'>Delete</a> ", file.id, lane.id)) + string.Format ("<a href='ViewLaneFileHistory.aspx?id={0}'>View history</a>", file.id),
				file_list.ToString ()));

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

		// build dictionary of lanes
		var laneDictionary = new Dictionary<int, DBLane> ();
		foreach (var l in response.Lanes)
			laneDictionary.Add (l.id, l);

		// build table of lanes where each file is used
		var lanesUsedByFile = new Dictionary<int, HashSet<string>> ();
		foreach (var lf in response.LaneFiles) {
			HashSet<string> f;
			if (!lanesUsedByFile.TryGetValue (lf.lanefile_id, out f)) {
				f = new HashSet<string> ();
				lanesUsedByFile [lf.lanefile_id] = f;
			}
			f.Add (laneDictionary [lf.lane_id].lane);
		}
		// list all the files, with a tooltip saying where each file is used
		foreach (DBLanefile file in response.ExistingFiles) {
			if (shown_files.Contains (file.id))
				continue;
			shown_files.Add (file.id);

			existing_files.AppendFormat ("<option value='{0}'", file.id);
			HashSet<string> lanes_for_file;
			if (lanesUsedByFile.TryGetValue (file.id, out lanes_for_file)) {
				existing_files.Append (" title='Used in: ");
				var any = false;
				foreach (var l in lanes_for_file) {
					if (any) {
						existing_files.Append (", ");
					} else {
						any = true;
					}
					existing_files.Append (l);
				}
				existing_files.Append ("'");
			}
			existing_files.Append (">");
			existing_files.Append (file.name);
			existing_files.Append ("</option>\n");
//				existing_files.AppendFormat ("<option value='{1}' title='Used in: {2}'>{0}</option>\n", file.name, file.id, "somewhere else");
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
				string.Format ("<a href='javascript:editCommandDeadlockTimeout ({2}, {0}, true, \"{3}\")'>{1}</a>", command.id, command.deadlock_timeout == null ? "wrench default" : (command.deadlock_timeout.Value == 0 ? "disabled" : command.deadlock_timeout.Value.ToString () + " minutes"), lane.id, command.deadlock_timeout.HasValue ? command.deadlock_timeout.Value.ToString () : string.Empty),
			    string.Format ("<a href='javascript:editCommandWorkingDirectory ({2}, {0}, true, \"{1}\")'>{3}</a>", command.id, command.working_directory, lane.id, working_directory),
				string.Format ("<a href='javascript:editCommandUploadFiles ({2}, {0}, true, \"{1}\")'>{3}</a>", command.id, command.upload_files, lane.id, upload_files),
				string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;command_id={1}&amp;action=switchTimestamp'>{2}</a>", lane.id, command.id, (command.timestamp ? "yes" : "no")),
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
			"wrench default",
			"-",
			"-",
			"no",
			string.Format ("<a href='javascript:addCommand ({0}, {1})'>Add</a>", lane.id, response.Commands.Count > 0 ? (response.Commands [response.Commands.Count - 1].sequence + 10) : 0),
			"-"));

		// Show all the hosts
		List<string> current_hosts = new List<string> ();
		string html;

		foreach (DBHostLaneView view in response.HostLaneViews) {
			string ed = view.enabled ? "enabled" : "disabled";
			string hid = view.hidden ? "hidden" : "visible";
			string @class = ed + " " + hid;
			row = new TableRow ();

			row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='EditHost.aspx?host_id={0}'>{1}</a>", view.host_id, view.host), @class));
			html = string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;host_id={1}&amp;action=removeHost'>Remove</a> ", lane.id, view.host_id);
			html = html + string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;host_id={1}&amp;action=switchHostEnabled'>{2}</a> ", lane.id, view.host_id, (view.enabled ? "Disable" : "Enable"));
			html = html + string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;host_id={1}&amp;action=switchHostHidden'>{2}</a>", lane.id, view.host_id, (view.hidden ? "Show" : "Hide"));
			row.Cells.Add (Utils.CreateTableCell (html, @class));
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
			case DBLaneDependencyCondition.DependentLaneIssuesOrSuccess:
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
			if ((int) value == 0 || ((DBLaneDependencyCondition) value) == DBLaneDependencyCondition.DependentLaneSuccessWithFile)
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

//			// deletion directives
//			foreach (DBLaneDeletionDirectiveView directive in response.LaneDeletionDirectives)
//				AddDeletionDirectiveRow (directive);
//			if (response.FileDeletionDirectives != null && response.FileDeletionDirectives.Count > 0) {
//				foreach (DBFileDeletionDirective directive in response.FileDeletionDirectives) {
//					lstDeletionDirectives2.Items.Add (new ListItem (directive.name, directive.id.ToString ()));
//				}
//			} else {
//				rowDeletionDirectives2.Visible = false;
//			}
//			foreach (DBDeleteCondition condition in Enum.GetValues (typeof (DBDeleteCondition)))
//				lstDeletionDirectiveCondition1.Items.Add (new ListItem (condition.ToString (), ((int) condition).ToString ()));
//			foreach (DBMatchMode mode in Enum.GetValues (typeof (DBMatchMode)))
//				lstDeletionDirectiveGlobs1.Items.Add (new ListItem (mode.ToString (), ((int) mode).ToString ()));

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
	}

	IEnumerable<DBLane> GetLanesWhereFileIsUsed (DBLanefile file, GetLaneForEditResponse response)
	{
		IEnumerable<DBLanefiles> lane_files = response.LaneFiles.Where ((v) => v.lanefile_id == file.id);
		IEnumerable<DBLane> lanes = response.Lanes.Where ((v) => lane_files.FirstOrDefault ((v2) => v2.lane_id == v.id) != null);
		return lanes;
	}

//	private void AddDeletionDirectiveRow (DBLaneDeletionDirectiveView directive)
//	{
//		TableRow row;
//		int index = tblDeletionDirective.Rows.Count - 2;
//		row = Utils.CreateTableRow (
//			directive.name,
//			directive.filename,
//			((DBMatchMode) directive.match_mode).ToString (),
//			((DBDeleteCondition) directive.condition).ToString (),
//			directive.x.ToString (),
//			Utils.CreateLinkButton ("lnkEnableDirective" + directive.id.ToString (), directive.enabled ? "True" : "False", directive.enabled ? "disableDeletionDirective" : "enableDeletionDirective", directive.id.ToString (), btn_Command),
//			Utils.CreateLinkButton ("lnkUnlinkDirective" + directive.id.ToString (), "Unlink", "unlinkDeletionDirective", directive.id.ToString () + ";" + index.ToString (), btn_Command),
//			Utils.CreateLinkButton ("lnkDeleteDirective" + directive.id.ToString (), "Delete", "deleteDeletionDirective", directive.file_deletion_directive_id.ToString () + ";" + directive.id + ";" + index.ToString (), btn_Command)
//			);
//		tblDeletionDirective.Rows.AddAt (index, row);
//	}

	private void DumpArguments ()
	{
		Console.WriteLine ("> ARGUMENT DUMP");
		foreach (string obj in Request.Params.AllKeys)
			Console.WriteLine (">> '{0}' = '{1}'", obj, Request [obj]);
		Console.WriteLine ("> ARGUMENT DUMP END");

	}
//
//	public void btn_Command (object sender, CommandEventArgs e)
//	{
//		//	Console.WriteLine ("EditLane, got command '{0}' with arguments '{1}'", e.CommandName, e.CommandArgument);
//		int lane_deletion_directive_id;
//
//		switch (e.CommandName) {
//		case "addDeletionDirective":
//			switch ((string) e.CommandArgument) {
//			case "1": {
//					string description = txtDeletionDirective1.Text;
//					string filename = txtDeletionDirectiveFilename1.Text;
//
//					lblDeletionDirectiveErrors.Visible = false;
//					try {
//						if (!string.IsNullOrEmpty (filename) && !string.IsNullOrEmpty (description)) {
//							int directive_id = Utils.LocalWebService.AddFileDeletionDirective (Master.WebServiceLogin, filename, description,
//								(DBMatchMode) int.Parse (lstDeletionDirectiveGlobs1.SelectedValue),
//								int.Parse (txtDeletionDirectiveX1.Text),
//								(DBDeleteCondition) int.Parse (lstDeletionDirectiveCondition1.SelectedValue));
//
//							lane_deletion_directive_id = Utils.LocalWebService.AddLaneDeletionDirective (Master.WebServiceLogin, directive_id, lane.id);
//
//							AddDeletionDirectiveRow (Utils.LocalWebService.FindLaneDeletionDirective (Master.WebServiceLogin, directive_id, lane_deletion_directive_id));
//						}
//					} catch (Exception ex) {
//						lblDeletionDirectiveErrors.Text = ex.Message;
//						lblDeletionDirectiveErrors.Visible = true;
//					}
//					break;
//				}
//			case "2": {
//					int directive_id;
//
//					if (int.TryParse (lstDeletionDirectives2.SelectedValue, out directive_id)) {
//						lblDeletionDirectiveErrors.Visible = false;
//						try {
//							lane_deletion_directive_id = Utils.LocalWebService.AddLaneDeletionDirective (Master.WebServiceLogin, directive_id, lane.id);
//							AddDeletionDirectiveRow (Utils.LocalWebService.FindLaneDeletionDirective (Master.WebServiceLogin, directive_id, lane_deletion_directive_id));
//						} catch (Exception ex) {
//							lblDeletionDirectiveErrors.Text = ex.Message;
//							lblDeletionDirectiveErrors.Visible = true;
//						}
//					}
//
//					break;
//				}
//			}
//			break;
//		case "disableDeletionDirective": {
//				int id;
//
//				if (int.TryParse ((string) e.CommandArgument, out id)) {
//					Utils.LocalWebService.EnableDeletionDirective (Master.WebServiceLogin, id, false);
//					RedirectToSelf (); // This is needed, otherwise hitting F5 will flap the enabled state
//				}
//				break;
//			}
//		case "enableDeletionDirective": {
//				int id;
//
//				if (int.TryParse ((string) e.CommandArgument, out id)) {
//					Utils.LocalWebService.EnableDeletionDirective (Master.WebServiceLogin, id, true);
//					RedirectToSelf (); // This is needed, otherwise hitting F5 will flap the enabled state
//				}
//				break;
//			}
//		case "deleteDeletionDirective": {
//				int file_directive_id, id, index;
//
//				string [] args = ((string) e.CommandArgument).Split (';');
//
//				if (int.TryParse (args [0], out file_directive_id)) {
//					if (int.TryParse (args [1], out id)) {
//						if (int.TryParse (args [2], out index)) {
//							// todo
//							try {
//								Utils.LocalWebService.DeleteDeletionDirective (Master.WebServiceLogin, id, file_directive_id);
//								tblDeletionDirective.Rows.RemoveAt (index);
//								for (int i = 0; i < lstDeletionDirectives2.Items.Count - 1; i++) {
//									if (lstDeletionDirectives2.Items [i].Value == file_directive_id.ToString ()) {
//										lstDeletionDirectives2.Items.RemoveAt (i);
//										break;
//									}
//								}
//							} catch (Exception ex) {
//								lblDeletionDirectiveErrors.Text = ex.Message;
//								lblDeletionDirectiveErrors.Visible = true;
//							}
//						}
//					}
//				}
//				break;
//			}
//		case "unlinkDeletionDirective": {
//				int id, index;
//
//				string [] args = ((string) e.CommandArgument).Split (';');
//
//				if (int.TryParse (args [0], out id)) {
//					if (int.TryParse (args [1], out index)) {
//						Utils.LocalWebService.UnlinkDeletionDirective (Master.WebServiceLogin, id);
//						tblDeletionDirective.Rows.RemoveAt (index);
//					}
//				}
//				break;
//			}
//		default:
//			Console.WriteLine ("EditLane: unknown command '{0}' '{1}'", e.CommandName, e.CommandArgument);
//			break;
//		}
//	}

	protected void cmdSave_Click (object sender, EventArgs e)
	{
		string str_lane = txtID.Text;
		int lane_id;
		int? parent_lane_id = null;
		DBLane lane;

		if (!int.TryParse (str_lane, out lane_id))
			return;

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
		lane.enabled = chkEnabled.Checked;
		lane.additional_roles = txtRoles.Text;
		lane.priority = Int32.Parse (lstPriority.SelectedItem.Value);
		lane.is_protected = chkProtected.Checked;

		Utils.LocalWebService.EditLaneWithTags (Master.WebServiceLogin, lane, !string.IsNullOrEmpty (txtTags.Text) ? txtTags.Text.Split (',') : null);
		RedirectToSelf ();
	}

	protected void lnkAddNotification_Click (object sender, EventArgs e)
	{
		WebServiceResponse response;
		response = Utils.LocalWebService.AddLaneNotification (Master.WebServiceLogin, lane.id, int.Parse (cmbNotifications.SelectedItem.Value));
		if (response.Exception != null) {
			lblMessage.Text = response.Exception.Message;
		} else {
			RedirectToSelf ();
		}
	}

	protected void OnLinkButtonCommand (object sender, CommandEventArgs e)
	{
		WebServiceResponse response;
		switch (e.CommandName) {
		case "RemoveNotification":
			response = Utils.LocalWebService.RemoveLaneNotificationForLane (Master.WebServiceLogin, int.Parse ((string) e.CommandArgument), lane.id);
			if (response.Exception != null) {
				lblMessage.Text = response.Exception.Message;
			} else {
				RedirectToSelf ();
			}
			break;
		}
	}

	protected void cmdDeleteAllWork_Click (object sender, EventArgs e)
	{
		Response.Redirect ("Delete.aspx?action=delete-all-work-for-lane&lane_id=" + lane.id.ToString (), false);
	}

	protected void cmdClearAllWork_Click (object sender, EventArgs e)
	{
		Response.Redirect ("Delete.aspx?action=clear-all-work-for-lane&lane_id=" + lane.id.ToString (), false);
	}

	protected void cmdDeleteAllRevisions_Click (object sender, EventArgs e)
	{
		Response.Redirect ("Delete.aspx?action=delete-all-revisions-for-lane&lane_id=" + lane.id.ToString (), false);
	}

	protected void cmdDontDoWork_Click (object sender, EventArgs e)
	{
		Utils.LocalWebService.MarkAsDontBuild (Master.WebServiceLogin, lane.id);
	}
}

