/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Builder;

public partial class EditLane : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		try {
			DBLane lane;
			TableRow row;
			string strlane = Request ["lane"];
			string laneid = Request ["lane_id"];
			string action = Request ["action"];
			string command_id = Request ["command_id"];

			int id;
			int sequence;
			int timeout;

			Configuration.InitializeApp (null, "Builder.Web");

			if (Master.Login == null) {
				Response.Redirect ("index.aspx");
				return;
			}

			tblCommands.Visible = true;
			tblFiles.Visible = true;

			DB db = Master.DB;

			if (int.TryParse (laneid, out id))
				lane = new DBLane (db, id);
			else
				lane = db.LookupLane (strlane);

			if (lane == null) {
				Response.Redirect ("EditLanes.aspx");
				return;
			}

			lblH2.Text = "Lane: " + lane.lane;

			if (!IsPostBack) {
				txtSourceControl.Text = lane.source_control;
				txtRepository.Text = lane.repository;
				txtMinRevision.Text = lane.min_revision;
				txtMaxRevision.Text = lane.max_revision;
				txtLane.Text = lane.lane;
				txtID.Text = lane.id.ToString ();
			}

			if (!string.IsNullOrEmpty (action)) {
				switch (action) {
				case "createFile": {
						DBLanefile file = new DBLanefile ();
						file.name = Request ["filename"];
						file.contents = "#!/bin/bash -ex\n\n#Your commands here\n";
						file.mime = "text/plain";
						file.Save (db.Connection);

						DBLanefiles lanefile = new DBLanefiles ();
						lanefile.lane_id = lane.id;
						lanefile.lanefile_id = file.id;
						lanefile.Save (db);
						// TODO: Check if filename already exists.
						break;
					}
				case "addFile":
					if (int.TryParse (Request ["lanefile_id"], out id)) {
						DBLanefiles lanefile = new DBLanefiles ();
						lanefile.lane_id = lane.id;
						lanefile.lanefile_id = id;
						lanefile.Save (db);
					}
					break;
				case "deleteFile":
					if (int.TryParse (Request ["file_id"], out id))
						DBLanefiles.Delete (db, lane.id, id);
					break;
				case "editCommandFilename":
					if (int.TryParse (command_id, out id)) {
						DBCommand cmd = new DBCommand (db, id);
						cmd.filename = Request ["filename"];
						cmd.Save (db);
					}
					break;
				case "editCommandSequence":
					if (int.TryParse (command_id, out id)) {
						if (int.TryParse (Request ["sequence"], out sequence)) {
							DBCommand cmd = new DBCommand (db, id);
							cmd.sequence = sequence;
							cmd.Save (db);
						}
					}
					break;
				case "editCommandArguments":
					if (int.TryParse (command_id, out id)) {
						DBCommand cmd = new DBCommand (db, id);
						cmd.arguments = Request ["arguments"];
						cmd.Save (db);
					}
					break;
				case "editCommandTimeout":
					if (int.TryParse (command_id, out id)) {
						if (int.TryParse (Request ["timeout"], out timeout)) {
							DBCommand cmd = new DBCommand (db, id);
							cmd.timeout = timeout;
							cmd.Save (db);
						}
					}
					break;
				case "deletecommand":
					if (int.TryParse (command_id, out id)) {
						// TODO: Check if the command has any work, if not just delete it.
						DBCommand cmd = new DBCommand (db, id);
						cmd.lane_id = null;
						cmd.Save (db);
					}
					break;
				case "switchNonFatal":
					if (int.TryParse (command_id, out id)) {
						DBCommand cmd = new DBCommand (db, id);
						cmd.nonfatal = !cmd.nonfatal;
						cmd.Save (db);
					}
					break;
				case "switchAlwaysExecute":
					if (int.TryParse (command_id, out id)) {
						DBCommand cmd = new DBCommand (db, id);
						cmd.alwaysexecute = !cmd.alwaysexecute;
						cmd.Save (db);
					}
					break;
				case "switchInternal":
					if (int.TryParse (command_id, out id)) {
						DBCommand cmd = new DBCommand (db, id);
						cmd.@internal = !cmd.@internal;
						cmd.Save (db);
					}
					break;
				case "addCommand": {
						DBCommand cmd = new DBCommand ();
						cmd.arguments = "-ex {0}";
						cmd.filename = "bash";
						cmd.command = Request ["command"];
						cmd.lane_id = lane.id;
						cmd.alwaysexecute = false;
						cmd.nonfatal = false;
						cmd.timeout = 60;
						if (int.TryParse (Request ["sequence"], out sequence)) {
							cmd.sequence = sequence;
						} else {
							try {
								cmd.sequence = 10 * (int) (db.ExecuteScalar ("SELECT Count(*) FROM Command WHERE lane_id = " + lane.id.ToString ()));
							} catch {
							}
						}
						cmd.Save (db);
						break;
					}

				case "switchHostEnabled":
					if (int.TryParse (Request ["host_id"], out id)) {
						DBHostLane hostlane = db.GetHostLane (id, lane.id);
						hostlane.enabled = !hostlane.enabled;
						hostlane.Save (db);
					}
					break;
				case "removeHost":
					if (int.TryParse (Request ["host_id"], out id)) {
						DBHost host = new DBHost (db, id);
						host.RemoveLane (db, lane.id);
					}
					break;
				case "addHost":
					if (int.TryParse (Request ["host_id"], out id)) {
						DBHost host = new DBHost (db, id);
						host.AddLane (db, lane.id);
					}
					break;
				case "addDependency":
					if (int.TryParse (Request ["dependent_lane_id"], out id)) {
						int condition;
						if (int.TryParse (Request ["condition"], out condition) && Enum.IsDefined (typeof (DBLaneDependencyCondition), condition)) {
							DBLaneDependency dep = new DBLaneDependency ();
							dep.condition = condition;
							dep.dependent_lane_id = id;
							dep.lane_id = lane.id;
							dep.Save (db);
						}
					}
					break;
				case "editDependencyFilename":
					if (int.TryParse (Request ["lanedependency_id"], out id)) {
						DBLaneDependency dep = new DBLaneDependency (db, id);
						dep.filename = Request ["filename"];
						dep.Save (db);
					}
					break;
				case "deleteDependency":
					if (int.TryParse (Request ["dependency_id"], out id))
						DBLaneDependency.Delete (db, id, DBLaneDependency.TableName);
					break;
				case "editDependencyDownloads":
					if (int.TryParse (Request ["lanedependency_id"], out id)) {
						DBLaneDependency dep = new DBLaneDependency (db, id);
						dep.download_files = Request ["downloads"];
						dep.Save (db);
					}
					break;
				default:
					break;
				}

				Response.Redirect ("EditLane.aspx?lane_id=" + lane.id.ToString ());
			}

			// Files
			tblFiles.Rows.Add (Utils.CreateTableHeaderRow ("Files"));
			tblFiles.Rows [0].Cells [0].ColumnSpan = 4;
			List<DBLanefile> files = lane.GetFiles (db);
			foreach (DBLanefile file in files) {
				string text = file.name;
				if (!string.IsNullOrEmpty (file.mime))
					text += " (" + file.mime + ")";

				tblFiles.Rows.Add (Utils.CreateTableRow (
					string.Format ("<a href='EditLaneFile.aspx?lane_id={1}&amp;file_id={0}'>{2}</a>", file.id, lane.id, file.name),
					file.mime,
					string.Format ("<a href='EditLane.aspx?lane_id={1}&amp;action=deleteFile&amp;file_id={0}'>Delete</a> <a href='ViewLaneFileHistory.aspx?id={0}'>View history</a>", file.id, lane.id)));
			}
			tblFiles.Rows.Add (Utils.CreateTableRow (
				"<input type='text' value='filename' id='txtCreateFileName'></input>",
				"text/plain",
				string.Format ("<a href='javascript:createFile ({0})'>Add</a>", lane.id)
				));
			StringBuilder existing_files = new StringBuilder ();
			existing_files.AppendLine ("<select id='cmbExistingFiles'>");
			List<DBLanefile> result = new List<DBLanefile> ();
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = @"
SELECT Lanefiles.lane_id, Lanefile.id, Lanefile.name, Lane.lane
FROM Lanefiles 
INNER JOIN Lanefile ON Lanefile.id = Lanefiles.lanefile_id
INNER JOIN Lane ON Lanefiles.lane_id = Lane.id
WHERE Lanefiles.lane_id <> @lane_id 
ORDER BY lane_id, name ASC";
				DB.CreateParameter (cmd, "lane_id", lane.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					int lane_index = reader.GetOrdinal ("lane");
					int name_index = reader.GetOrdinal ("name");
					int id_index = reader.GetOrdinal ("id");
					while (reader.Read ()) {
						result.Add (new DBLanefile (reader));
						existing_files.AppendFormat ("<option value='{2}'>{0} - {1}</option>\n", reader.GetString (lane_index), reader.GetString (name_index), reader.GetInt32 (id_index));
					}
				}
			}
			existing_files.AppendLine ("</select>");
			tblFiles.Rows.Add (Utils.CreateTableRow (
				existing_files.ToString (),
				"N/A",
				string.Format ("<a href='javascript:addFile ({0})'>Add</a>", lane.id)
				));
			tblFiles.Visible = true;


			tblCommands.Rows.Add (Utils.CreateTableHeaderRow ("Commands"));
			tblCommands.Rows.Add (Utils.CreateTableHeaderRow ("Sequence", "Command", "Always Execute", "Non Fatal", "Internal", "Executable", "Arguments", "Timeout", ""));
			tblCommands.Rows [0].Cells [0].ColumnSpan = 8;

			List<DBCommand> commands = lane.GetCommands (db);
			foreach (DBCommand command in commands) {
				string filename = command.command;
				DBLanefile file = files.Find (f => f.name == filename);
				if (file != null)
					filename = string.Format ("<a href='EditLaneFile.aspx?lane_id={1}&amp;file_id={0}'>{2}</a>", file.id, lane.id, file.name);

				tblCommands.Rows.Add (Utils.CreateTableRow (
					string.Format ("<a href='javascript:editCommandSequence ({2}, {0}, true, \"{1}\")'>{1}</a>", command.id, command.sequence, lane.id),
					filename,
					string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;command_id={3}&amp;action=switchAlwaysExecute'>{2}</a>", lane.id, (!command.alwaysexecute).ToString (), command.alwaysexecute ? "yes" : "no", command.id),
					string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;command_id={3}&amp;action=switchNonFatal'>{2}</a>", lane.id, (!command.nonfatal).ToString (), command.nonfatal ? "yes" : "no", command.id),
					string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;command_id={1}&amp;action=switchInternal'>{2}</a>", lane.id, command.id, (command.@internal ? "yes" : "no")),
					string.Format ("<a href='javascript:editCommandFilename ({2}, {0}, true, \"{1}\")'>{1}</a>", command.id, command.filename, lane.id),
					string.Format ("<a href='javascript:editCommandArguments ({2}, {0}, true, \"{1}\")'>{1}</a>", command.id, command.arguments, lane.id),
					string.Format ("<a href='javascript:editCommandTimeout ({2}, {0}, true, \"{1}\")'>{1} minutes</a>", command.id, command.timeout, lane.id),
					string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;action=deletecommand&amp;command_id={1}'>Delete</a>", lane.id, command.id)));
			}
			tblCommands.Rows.Add (Utils.CreateTableRow (
				(commands.Count * 10).ToString (),
				string.Format ("<input type='text' value='command' id='txtCreateCommand_name'></input>"),
				"no",
				"no",
				"no",
				"bash",
				"-ex {0}",
				"60 minutes",
				string.Format ("<a href='javascript:addCommand ({0}, {1})'>Add</a>", lane.id, (commands.Count * 10))));

			// Show all the hosts
			List<DBHostLaneView> views = lane.GetHosts (db);
			List<string> current_hosts = new List<string> ();
			List<DBHost> all_hosts = db.GetHosts ();
			string html;

			tblHosts.Rows.Add (Utils.CreateTableHeaderRow ("Hosts"));
			tblHosts.Rows [0].Cells [0].ColumnSpan = 3;
			foreach (DBHostLaneView view in views) {
				string ed = view.enabled ? "enabled" : "disabled";
				row = new TableRow ();

				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='EditHost.aspx?host_id={0}'>{1}</a>", view.host_id, view.host), view.enabled ? "enabled" : "disabled"));
				html = string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;host_id={1}&amp;action=removeHost'>Remove</a> ", lane.id, view.host_id);
				row.Cells.Add (Utils.CreateTableCell (html, ed));
				html = string.Format ("<a href='EditLane.aspx?lane_id={0}&amp;host_id={1}&amp;action=switchHostEnabled'>{2}</a>", lane.id, view.host_id, (view.enabled ? "Disable" : "Enable"));
				row.Cells.Add (Utils.CreateTableCell (html, ed));
				tblHosts.Rows.Add (row);
				current_hosts.Add (view.host);
			}

			if (all_hosts.Count != current_hosts.Count) {
				row = new TableRow ();
				html = "<select id='lstHosts'>";
				foreach (DBHost host in all_hosts) {
					if (!current_hosts.Contains (host.host))
						html += "<option value='" + host.id + "'>" + host.host + "</option>";
				}
				html += "</select>";
				row.Cells.Add (Utils.CreateTableCell (html));
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript:addHost({0})'>Add</a>", lane.id)));
				row.Cells.Add (Utils.CreateTableCell ("-"));
				tblHosts.Rows.Add (row);
			}

			// dependencies
			List<DBLane> lanes = db.GetAllLanes ();
			List<DBLaneDependency> dependencies = lane.GetDependencies (db);
			tblDependencies.Rows.Add (Utils.CreateTableHeaderRow ("Dependent lane", "Condition", "Filename", "Files to download", "Actions"));
			foreach (DBLaneDependency dependency in dependencies) {
				row = new TableRow ();
				for (int i = 0; i < lanes.Count; i++) {
					if (lanes [i].id == dependency.dependent_lane_id) {
						row.Cells.Add (Utils.CreateTableCell (lanes [i].lane));
						break;
					}
				}
				row.Cells.Add (Utils.CreateTableCell (dependency.Condition.ToString ()));
				switch (dependency.Condition) {
				case DBLaneDependencyCondition.DependentLaneSuccessWithFile:
					row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript: editDependencyFilename ({0}, {1}, \"{2}\")'>{2}</a>", lane.id, dependency.id, string.IsNullOrEmpty (dependency.filename) ? "(edit)" : dependency.filename)));
					break;
				case DBLaneDependencyCondition.DependentLaneSuccess:
				default:
					row.Cells.Add (Utils.CreateTableCell ("-"));
					break;
				}
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript: editDependencyDownloads ({0}, {1}, \"{2}\")'>{3}</a>", lane.id, dependency.id,  string.IsNullOrEmpty (dependency.download_files) ? string.Empty : dependency.download_files.Replace ("\"", "\\\""), string.IsNullOrEmpty (dependency.download_files) ? "(edit)" : HttpUtility.HtmlEncode (dependency.download_files))));
				row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript: deleteDependency ({0}, {1})'>Delete</a>", lane.id, dependency.id)));
				tblDependencies.Rows.Add (row);
			}
			// Create new dependency row
			row = new TableRow ();
			html = "<select id='lstDependentLanes'>";
			foreach (DBLane l in lanes) {
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
			row.Cells.Add (Utils.CreateTableCell (string.Empty));
			row.Cells.Add (Utils.CreateTableCell (string.Empty));
			row.Cells.Add (Utils.CreateTableCell (string.Format ("<a href='javascript:addDependency ({0})'>Add</a>", lane.id)));
			tblDependencies.Rows.Add (row);


		} catch (Exception ex) {
			Response.Write (ex.ToString ().Replace ("\n", "<br/>"));
		}
	}

	protected void cmdSave_Click (object sender, EventArgs e)
	{
		string str_lane = txtID.Text;
		int lane_id;
		DBLane lane;

		if (!int.TryParse (str_lane, out lane_id)) {
			return;
		}

		using (DB db = new DB (true)) {
			lane = new DBLane (db, lane_id);
			lane.lane = txtLane.Text;
			lane.max_revision = txtMaxRevision.Text;
			lane.min_revision = txtMinRevision.Text;
			lane.repository = txtRepository.Text;
			lane.source_control = txtSourceControl.Text;
			lane.Save (db);
		}
	}
}
