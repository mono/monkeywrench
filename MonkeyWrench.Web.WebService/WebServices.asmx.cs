/*
 * WebServices.asmx.cs
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
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Services;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;

namespace MonkeyWrench.WebServices
{
	[WebService (Namespace = "http://monkeywrench.novell.com/")]
	[WebServiceBinding (ConformsTo = WsiProfiles.None)]
	[System.ComponentModel.ToolboxItem (false)]
	public class WebServices : System.Web.Services.WebService
	{
		public WebServices () : this(true)
		{
		}

		public WebServices (bool loadConfig) {
			if(loadConfig)
				Configuration.LoadConfiguration (new string [] { });
		}

		internal void Authenticate (DB db, WebServiceLogin login, WebServiceResponse response)
		{
			Authenticate (db, login, response, false);
		}

		internal void Authenticate (DB db, WebServiceLogin login, WebServiceResponse response, bool @readonly)
		{
			Authentication.Authenticate (Context, db, login, response, @readonly);
		}

		private void VerifyUserInRole (DB db, WebServiceLogin login, string role)
		{
			VerifyUserInRole (db, login, role, false);
		}

		private void VerifyUserInRole (DB db, WebServiceLogin login, string role, bool @readonly)
		{
			Authentication.VerifyUserInRole (Context, db, login, role, @readonly);
		}

		[WebMethod]
		public string [] GetRoles (string user)
		{
			using (DB db = new DB ()) {
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = "SELECT roles FROM Person WHERE login = @user;";
					DB.CreateParameter (cmd, "user", user);
					string result = cmd.ExecuteScalar () as string;

					if (result is string)
						return ((string) result).Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				}
			}

			return null;
		}

		[WebMethod]
		public LoginResponse LoginOpenId (WebServiceLogin login, string email, string ip4)
		{
			LoginResponse response = new LoginResponse ();

			using (DB db = new DB ()) {
				try {
					VerifyUserInRole (db, login, Roles.Administrator);
					DBLogin_Extensions.LoginOpenId (db, response, email, ip4);
				} catch (Exception ex) {
					response.Exception = new WebServiceException (ex);
				}
				return response;
			}
		}

		[WebMethod]
		public LoginResponse Login (WebServiceLogin login)
		{
			LoginResponse response = new LoginResponse ();
			using (DB db = new DB ()) {
				Authenticate (db, login, response);
				response.User = login.User;
				return response;
			}
		}

		[WebMethod]
		public void Logout (WebServiceLogin login)
		{
			if (string.IsNullOrEmpty (login.Cookie))
				return;

			using (DB db = new DB ()) {
				db.Audit (login, "WebServices.Logout (login.Cookie: {0})", login.Cookie);

				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = "DELETE FROM Login WHERE cookie = @cookie;";
					DB.CreateParameter (cmd, "cookie", login.Cookie);
					cmd.ExecuteNonQuery ();
				}
			}
		}

		[WebMethod]
		public void CreateLanefile (WebServiceLogin login, int lane_id, string filename)
		{
			if (string.IsNullOrEmpty (filename))
				throw new ArgumentException ("filename");

			if (lane_id <= 0)
				throw new ArgumentException ("lane_id");

			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				db.Audit (login, "WebServices.CreateLaneFile (lane_id: {0}, filename: {1})", lane_id, filename);

				DBLanefile file = new DBLanefile ();
				file.name = filename;
				file.contents = "#!/bin/bash -ex\n\n#Your commands here\n";
				file.mime = "text/plain";
				file.Save (db);

				DBLanefiles lanefile = new DBLanefiles ();
				lanefile.lane_id = lane_id;
				lanefile.lanefile_id = file.id;
				lanefile.Save (db);

				// TODO: Check if filename already exists.
			}
		}

		[WebMethod]
		public void AttachFileToLane (WebServiceLogin login, int lane_id, int lanefile_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				db.Audit (login, "WebServices.AttachFileToLane (lane_id: {0}, lanefile_id: {1})", lane_id, lanefile_id);
				DBLanefiles lanefile = new DBLanefiles ();
				lanefile.lane_id = lane_id;
				lanefile.lanefile_id = lanefile_id;
				lanefile.Save (db);
			}
		}

		[WebMethod]
		public void DeattachFileFromLane (WebServiceLogin login, int lane_id, int lanefile_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				db.Audit (login, "WebServices.DeattachFileFromLane (lane_id: {0}, lanefile_id: {1})", lane_id, lanefile_id);
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = "DELETE FROM Lanefiles WHERE lane_id = @lane_id AND lanefile_id = @lanefile_id;";
					DB.CreateParameter (cmd, "lane_id", lane_id);
					DB.CreateParameter (cmd, "lanefile_id", lanefile_id);
					cmd.ExecuteNonQuery ();
				}
			}
		}

		[WebMethod]
		public void EditCommand (WebServiceLogin login, DBCommand command)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				db.Audit (login, "WebServices.EditCommand (command: {0})", command);
				command.Save (db);
			}
		}

		[WebMethod]
		public void EditCommandFilename (WebServiceLogin login, int command_id, string filename)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				db.Audit (login, "WebServices.EditCommandFilename (command_id: {0}, filename: {1})", command_id, filename);
				DBCommand cmd = DBCommand_Extensions.Create (db, command_id);
				cmd.filename = filename;
				cmd.Save (db);
			}
		}

		[WebMethod]
		public void EditCommandSequence (WebServiceLogin login, int command_id, int sequence)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				db.Audit (login, "WebServices.EditCommandSequence (command_id: {0}, sequence: {1})", command_id, sequence);
				DBCommand cmd = DBCommand_Extensions.Create (db, command_id);
				cmd.sequence = sequence;
				cmd.Save (db);
			}
		}

		[WebMethod]
		public void EditCommandArguments (WebServiceLogin login, int command_id, string arguments)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				db.Audit (login, "WebServices.EditCommandArguments (command_id: {0}, arguments: {1})", command_id, arguments);
				DBCommand cmd = DBCommand_Extensions.Create (db, command_id);
				cmd.arguments = arguments;
				cmd.Save (db);
			}
		}

		[WebMethod]
		public void EditCommandTimeout (WebServiceLogin login, int command_id, int timeout)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				db.Audit (login, "WebServices.EditCommandTimeout (command_id: {0}, timeout: {1})", command_id, timeout);
				DBCommand cmd = DBCommand_Extensions.Create (db, command_id);
				cmd.timeout = timeout;
				cmd.Save (db);
			}
		}
		
		[WebMethod]
		public void EditCommandWorkingDirectory (WebServiceLogin login, int command_id, string working_directory)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBCommand cmd = DBCommand_Extensions.Create (db, command_id);
				if (working_directory.Equals(".") || working_directory.Equals(""))
					cmd.working_directory = null;
				else
					cmd.working_directory = working_directory;
				cmd.Save (db);
			}
		}
		
		[WebMethod]
		public void EditCommandUploadFiles (WebServiceLogin login, int command_id, string upload_files)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBCommand cmd = DBCommand_Extensions.Create (db, command_id);
				if (upload_files.Equals(""))
					cmd.upload_files = null;
				else
					cmd.upload_files = upload_files;
				cmd.Save (db);
			}
		}
		
		[WebMethod]
		public void SwitchCommandNonFatal (WebServiceLogin login, int command_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBCommand cmd = DBCommand_Extensions.Create (db, command_id);
				cmd.nonfatal = !cmd.nonfatal;
				cmd.Save (db);
			}
		}

		[WebMethod]
		public void SwitchCommandAlwaysExecute (WebServiceLogin login, int command_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBCommand cmd = DBCommand_Extensions.Create (db, command_id);
				cmd.alwaysexecute = !cmd.alwaysexecute;
				cmd.Save (db);
			}
		}

		[WebMethod]
		public void SwitchCommandInternal (WebServiceLogin login, int command_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBCommand cmd = DBCommand_Extensions.Create (db, command_id);
				cmd.@internal = !cmd.@internal;
				cmd.Save (db);
			}
		}

		[WebMethod]
		public void DeleteCommand (WebServiceLogin login, int command_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBCommand cmd = DBCommand_Extensions.Create (db, command_id);
				cmd.lane_id = null; // TODO: Check if the command has any work, if not just delete it.
				cmd.Save (db);
			}
		}

		[WebMethod]
		public void AddCommand (WebServiceLogin login, int lane_id, string command, bool always_execute, bool nonfatal, int timeout, int sequence)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBCommand cmd = new DBCommand ();
				cmd.arguments = "-ex {0}";
				cmd.filename = "bash";
				cmd.command = command;
				cmd.lane_id = lane_id;
				cmd.alwaysexecute = always_execute;
				cmd.nonfatal = nonfatal;
				cmd.timeout = 60;
				if (sequence < 0) {
					cmd.sequence = sequence;
				} else {
					cmd.sequence = 10 * (int) (long) (db.ExecuteScalar ("SELECT Count(*) FROM Command WHERE lane_id = " + lane_id.ToString ()));
				}
				cmd.Save (db);
			}
		}

		[WebMethod]
		public void SwitchHostEnabledForLane (WebServiceLogin login, int lane_id, int host_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBHostLane hostlane = db.GetHostLane (host_id, lane_id);
				hostlane.enabled = !hostlane.enabled;
				hostlane.Save (db);
			}
		}

		[WebMethod]
		public void SwitchHostHiddenForLane (WebServiceLogin login, int lane_id, int host_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBHostLane hostlane = db.GetHostLane (host_id, lane_id);
				hostlane.hidden = !hostlane.hidden;
				hostlane.Save (db);
			}
		}

		[WebMethod]
		public void RemoveHostForLane (WebServiceLogin login, int lane_id, int host_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBHost host = DBHost_Extensions.Create (db, host_id);
				host.RemoveLane (db, lane_id);
			}
		}

		[WebMethod]
		public void AddHostToLane (WebServiceLogin login, int lane_id, int host_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBHost host = DBHost_Extensions.Create (db, host_id);
				host.enabled = true;
				host.AddLane (db, lane_id);
			}
		}

		[WebMethod]
		public void AddDependencyToLane (WebServiceLogin login, int lane_id, int dependent_lane_id, int? host_id, DBLaneDependencyCondition condition)
		{
			if (!Enum.IsDefined (typeof (DBLaneDependencyCondition), condition))
				throw new ArgumentOutOfRangeException ("condition");

			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBLaneDependency dep = new DBLaneDependency ();
				dep.Condition = condition;
				dep.dependent_lane_id = dependent_lane_id;
				dep.lane_id = lane_id;
				dep.dependent_host_id = host_id;
				dep.Save (db);
			}
		}

		[WebMethod]
		public void EditLaneDependencyFilename (WebServiceLogin login, int lanedependency_id, string filename)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBLaneDependency dep = DBLaneDependency_Extensions.Create (db, lanedependency_id);
				dep.filename = filename;
				dep.Save (db);
			}
		}

		[WebMethod]
		public void DeleteLaneDependency (WebServiceLogin login, int lanedependency_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBRecord_Extensions.Delete (db, lanedependency_id, DBLaneDependency.TableName);
			}
		}

		[WebMethod]
		public void EditLaneDependencyDownloads (WebServiceLogin login, int lanedependency_id, string downloads)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBLaneDependency dep = DBLaneDependency_Extensions.Create (db, lanedependency_id);
				dep.download_files = downloads;
				dep.Save (db);
			}
		}

		[WebMethod]
		public void UnlinkDeletionDirective (WebServiceLogin login, int directive_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				// todo
				DBRecord_Extensions.Delete (db, directive_id, DBLaneDeletionDirective.TableName);
			}
		}

		[WebMethod]
		public void DeleteDeletionDirective (WebServiceLogin login, int lane_directive_id, int file_directive_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				// todo
				DBRecord_Extensions.Delete (db, lane_directive_id, DBLaneDeletionDirective.TableName);
				DBRecord_Extensions.Delete (db, file_directive_id, DBFileDeletionDirective.TableName);
			}
		}

		[WebMethod]
		public void EnableDeletionDirective (WebServiceLogin login, int lane_deletion_directive_id, bool enabled)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBLaneDeletionDirective view = DBLaneDeletionDirective_Extensions.Create (db, lane_deletion_directive_id);
				view.enabled = enabled;
				view.Save (db);
			}
		}

		[WebMethod]
		public int AddFileDeletionDirective (WebServiceLogin login, string filename, string name, DBMatchMode match_mode, int x, DBDeleteCondition condition)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBFileDeletionDirective directive = new DBFileDeletionDirective ();
				directive.filename = filename;
				directive.name = name;
				directive.match_mode = (int) match_mode;
				directive.x = x;
				directive.condition = (int) condition;
				directive.Save (db);
				return directive.id;
			}
		}

		[WebMethod]
		public int AddLaneDeletionDirective (WebServiceLogin login, int file_deletion_directive_id, int lane_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBLaneDeletionDirective lane_directive = new DBLaneDeletionDirective ();
				lane_directive.file_deletion_directive_id = file_deletion_directive_id;
				lane_directive.lane_id = lane_id;
				lane_directive.Save (db);
				return lane_directive.id;
			}
		}

		[WebMethod]
		public DBLaneDeletionDirectiveView FindLaneDeletionDirective (WebServiceLogin login, int file_deletion_directive_id, int lane_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				return DBLaneDeletionDirectiveView_Extensions.Find (db, file_deletion_directive_id, lane_id);
			}
		}

		[WebMethod]
		public GetLaneResponse GetLane (WebServiceLogin login, int lane_id)
		{
			GetLaneResponse response = new GetLaneResponse ();
			using (DB db = new DB ()) {
				Authenticate (db, login, response);
				response.lane = DBLane_Extensions.Create (db, lane_id);
				return response;
			}
		}

		[WebMethod]
		public GetWorkHostHistoryResponse GetWorkHostHistory (WebServiceLogin login, int? host_id, string host, int limit, int offset)
		{
			GetWorkHostHistoryResponse response = new GetWorkHostHistoryResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response, true);

				response.Host = FindHost (db, host_id, host);
				response.RevisionWorks = new List<DBRevisionWork> ();
				response.Lanes = new List<string> ();
				response.Revisions = new List<string> ();
				response.StartTime = new List<DateTime> ();
				response.Hosts = new List<string> ();
				response.Durations = new List<int> ();

				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"
SELECT RevisionWork.*, Host.host, Lane.lane, Revision.revision, MIN (Work.starttime) AS order_date,
-- calculate the duration of each work and add them up
   SUM (EXTRACT (EPOCH FROM (
		(CASE
			WHEN (Work.starttime = '-infinity' OR Work.starttime < '2001-01-01') AND (Work.endtime = '-infinity' OR Work.endtime < '2001-01-01') THEN LOCALTIMESTAMP - LOCALTIMESTAMP
			WHEN (Work.endtime = '-infinity' OR Work.endtime < '2001-01-01') THEN CURRENT_TIMESTAMP AT TIME ZONE 'UTC' - Work.starttime
			ELSE Work.endtime - Work.starttime
			END)
		))) AS duration
FROM RevisionWork
INNER JOIN Revision ON RevisionWork.revision_id = Revision.id
INNER JOIN Lane ON RevisionWork.lane_id = Lane.id
INNER JOIN Work ON RevisionWork.id = Work.revisionwork_id
INNER JOIN Host ON RevisionWork.host_id = Host.id
WHERE RevisionWork.workhost_id = @host_id AND (Work.starttime > '2001-01-01' AND Work.endtime > '2001-01-01') 
GROUP BY RevisionWork.id, RevisionWork.lane_id, RevisionWork.host_id, RevisionWork.workhost_id, RevisionWork.revision_id, RevisionWork.state, RevisionWork.lock_expires, RevisionWork.completed, RevisionWork.endtime, Lane.lane, Revision.revision, Host.host ";
					cmd.CommandText += " ORDER BY RevisionWork.completed ASC, order_date DESC ";
					if (limit > 0)
						cmd.CommandText += " LIMIT " + limit.ToString ();
					if (offset > 0)
						cmd.CommandText += " OFFSET " + offset.ToString ();
					cmd.CommandText += ";";
					DB.CreateParameter (cmd, "host_id", response.Host.id);

					using (IDataReader reader = cmd.ExecuteReader ()) {
						int lane_idx = reader.GetOrdinal ("lane");
						int revision_idx = reader.GetOrdinal ("revision");
						int starttime_idx = reader.GetOrdinal ("order_date");
						int host_idx = reader.GetOrdinal ("host");
						int duration_idx = reader.GetOrdinal ("duration");
						while (reader.Read ()) {
							response.RevisionWorks.Add (new DBRevisionWork (reader));
							response.Lanes.Add (reader.GetString (lane_idx));
							response.Revisions.Add (reader.GetString (revision_idx));
							response.StartTime.Add (reader.GetDateTime (starttime_idx));
							response.Hosts.Add (reader.GetString (host_idx));
							response.Durations.Add ((int) reader.GetDouble (duration_idx));
						}
					}
				}
			}

			return response;
		}

		[WebMethod]
		public GetHostForEditResponse GetHostForEdit (WebServiceLogin login, int? host_id, string host)
		{
			GetHostForEditResponse response = new GetHostForEditResponse ();

			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);

				response.Host = FindHost (db, host_id, host);
				response.Lanes = db.GetAllLanes ();
				if (response.Host != null) {
					response.Person = FindPerson (db, response.Host.host);
					response.HostLaneViews = response.Host.GetLanes (db);
					response.Variables = DBEnvironmentVariable_Extensions.Find (db, null, response.Host.id, null);
					response.MasterHosts = GetMasterHosts (db, response.Host);
					response.SlaveHosts = GetSlaveHosts (db, response.Host);
				}
				response.Hosts = db.GetHosts ();
			}

			return response;
		}

		[WebMethod]
		public void AddMasterHost (WebServiceLogin login, int host_id, int masterhost_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);

				DBMasterHost mh = new DBMasterHost ();
				mh.master_host_id = masterhost_id;
				mh.host_id = host_id;
				mh.Save (db);
			}
		}

		[WebMethod]
		public void RemoveMasterHost (WebServiceLogin login, int host_id, int masterhost_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);

				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = "DELETE FROM MasterHost WHERE host_id = @host_id AND master_host_id = @masterhost_id;";
					DB.CreateParameter (cmd, "host_id", host_id);
					DB.CreateParameter (cmd, "masterhost_id", masterhost_id);
					cmd.ExecuteNonQuery ();
				}
			}
		}

		[WebMethod]
		public GetLaneForEditResponse GetLaneForEdit (WebServiceLogin login, int lane_id, string lane)
		{
			GetLaneForEditResponse response = new GetLaneForEditResponse ();
			using (DB db = new DB ()) {
				Authenticate (db, login, response);
				VerifyUserInRole (db, login, Roles.Administrator);

				// We do 2 trips to the database: first to get a list of all the lanes,
				// then to get all the rest of the information.

				response.Lanes = db.GetAllLanes ();

				if (lane_id > 0) {
					response.Lane = response.Lanes.Find ((l) => l.id == lane_id);
				} else {
					response.Lane = response.Lanes.Find ((l) => l.lane == lane);
				}

				var cmdText = new StringBuilder ();

				using (var cmd = db.CreateCommand ()) {
					// 1: db.GetAllLanes
					cmdText.AppendLine ("SELECT * FROM Lane ORDER BY lane;");

					// 2: response.Lane.GetCommandsInherited (db, response.Lanes);
					cmdText.Append ("SELECT * FROM Command WHERE lane_id = ").Append (response.Lane.id);
					DBLane parent = response.Lane;
					while (null != (parent = response.Lanes.FirstOrDefault ((v) => v.id == parent.parent_lane_id))) {
						cmdText.Append (" OR lane_id = ").Append (parent.id);
					}
					cmdText.AppendLine (" ORDER BY sequence;");

					// 3: response.Dependencies = response.Lane.GetDependencies (db);
					cmdText.AppendFormat ("SELECT * FROM LaneDependency WHERE lane_id = {0} ORDER BY dependent_lane_id;", response.Lane.id).AppendLine ();

//					// 4: response.FileDeletionDirectives = DBFileDeletionDirective_Extensions.GetAll (db);
//					cmdText.AppendLine ("SELECT * FROM FileDeletionDirective;");
//
//					// 5: response.LaneDeletionDirectives = DBLaneDeletionDirectiveView_Extensions.Find (db, response.Lane);
//					cmdText.AppendFormat ("SELECT * FROM LaneDeletionDirectiveView WHERE lane_id = {0};", response.Lane.id).AppendLine ();

					// 6: response.Files = response.Lane.GetFiles (db, response.Lanes);
					cmdText.Append (@"
SELECT Lanefile.id, LaneFile.name, '' AS contents, LaneFile.mime, Lanefile.original_id, LaneFile.changed_date 
FROM Lanefile 
INNER JOIN Lanefiles ON Lanefiles.lanefile_id = Lanefile.id 
WHERE Lanefile.original_id IS NULL AND Lanefiles.lane_id = ").Append (response.Lane.id);
					parent = response.Lane;
					while (null != (parent = response.Lanes.FirstOrDefault ((v) => v.id == parent.parent_lane_id))) {
						cmdText.Append (" OR LaneFiles.lane_id = ").Append (parent.id);
					}
					cmdText.AppendLine (" ORDER BY name ASC;");

					// 7: response.LaneFiles = db.GetAllLaneFiles ();
					cmdText.AppendLine ("SELECT * FROM LaneFiles;");

					// 8: response.HostLaneViews = response.Lane.GetHosts (db);
					cmdText.AppendFormat ("SELECT * FROM HostLaneView WHERE lane_id = {0} ORDER BY host;", response.Lane.id).AppendLine ();

					// 9: response.Hosts = db.GetHosts ();
					cmdText.AppendLine ("SELECT * FROM Host ORDER BY host;");

					// 10: response.ExistingFiles = new List<DBLanefile> (); [...]
					cmdText.AppendFormat (@"
SELECT Lanefile.id, LaneFile.name, '' AS contents, LaneFile.mime, Lanefile.original_id, LaneFile.changed_date 
FROM Lanefile
INNER JOIN Lanefiles ON Lanefiles.lanefile_id = Lanefile.id
WHERE Lanefile.original_id IS NULL AND Lanefiles.lane_id <> {0}
ORDER BY Lanefiles.lane_id, Lanefile.name ASC;", response.Lane.id).AppendLine ();

					// 11: response.Variables = DBEnvironmentVariable_Extensions.Find (db, response.Lane.id, null, null);
					cmdText.AppendFormat ("SELECT * FROM EnvironmentVariable WHERE lane_id = {0} AND host_id IS NULL ORDER BY id ASC;", response.Lane.id).AppendLine ();

					// 12: response.Notifications = new List<DBNotification> ();
					cmdText.AppendLine ("SELECT * FROM Notification;");

					// 13: response.LaneNotifications = new List<DBLaneNotification> ();
					cmdText.AppendFormat ("SELECT * FROM LaneNotification WHERE lane_id = {0};", response.Lane.id).AppendLine ();

					// 14
					cmdText.AppendFormat ("SELECT * FROM LaneTag WHERE lane_id = {0};", response.Lane.id).AppendLine ();

					cmd.CommandText = cmdText.ToString ();

					using (IDataReader reader = cmd.ExecuteReader ()) {
						// 1: db.GetAllLanes
						response.Lanes = new List<DBLane> ();
						while (reader.Read ())
							response.Lanes.Add (new DBLane (reader));

						// 2: response.Lane.GetCommandsInherited (db, response.Lanes);
						reader.NextResult ();
						response.Commands = new List<DBCommand> ();
						while (reader.Read ())
							response.Commands.Add (new DBCommand (reader));
						
						// 3: response.Dependencies = response.Lane.GetDependencies (db);
						reader.NextResult ();
						response.Dependencies = new List<DBLaneDependency> ();
						while (reader.Read ())
							response.Dependencies.Add (new DBLaneDependency (reader));

//						// 4: response.FileDeletionDirectives = DBFileDeletionDirective_Extensions.GetAll (db);
//						reader.NextResult ();
//						response.FileDeletionDirectives = new List<DBFileDeletionDirective> ();
//						while (reader.Read ()) {
//							response.FileDeletionDirectives.Add (new DBFileDeletionDirective (reader));
//						}
//
//						// 5: response.LaneDeletionDirectives = DBLaneDeletionDirectiveView_Extensions.Find (db, response.Lane);
//						reader.NextResult ();
//						response.LaneDeletionDirectives = new List<DBLaneDeletionDirectiveView> ();
//						while (reader.Read ())
//							response.LaneDeletionDirectives.Add (new DBLaneDeletionDirectiveView (reader));
					
						// 6: response.Files = response.Lane.GetFiles (db, response.Lanes);
						reader.NextResult ();
						response.Files = new List<DBLanefile> ();
						while (reader.Read ())
							response.Files.Add (new DBLanefile (reader));

						// 7: response.LaneFiles = db.GetAllLaneFiles ();
						reader.NextResult ();
						response.LaneFiles = new List<DBLanefiles> ();
						while (reader.Read ())
							response.LaneFiles.Add (new DBLanefiles (reader));

						// 8: response.HostLaneViews = response.Lane.GetHosts (db);
						reader.NextResult ();
						response.HostLaneViews = new List<DBHostLaneView> ();
						while (reader.Read ()) {
							response.HostLaneViews.Add (new DBHostLaneView (reader));
						}

						// 9: response.Hosts = db.GetHosts ();
						reader.NextResult ();
						response.Hosts = new List<DBHost> ();
						while (reader.Read ())
							response.Hosts.Add (new DBHost (reader));

						// 10: response.ExistingFiles = new List<DBLanefile> (); [...]
						reader.NextResult ();
						response.ExistingFiles = new List<DBLanefile> ();
						while (reader.Read ())
							response.ExistingFiles.Add (new DBLanefile (reader));

						// 11: response.Variables = DBEnvironmentVariable_Extensions.Find (db, response.Lane.id, null, null);
						reader.NextResult ();
						response.Variables = new List<DBEnvironmentVariable> ();
						while (reader.Read ())
							response.Variables.Add (new DBEnvironmentVariable (reader));

						// 12: response.Notifications = new List<DBNotification> ();
						reader.NextResult ();
						response.Notifications = new List<DBNotification> ();
						while (reader.Read ())
							response.Notifications.Add (new DBNotification (reader));

						// 13: response.LaneNotifications = new List<DBLaneNotification> ();
						reader.NextResult ();
						response.LaneNotifications = new List<DBLaneNotification> ();
						while (reader.Read ())
							response.LaneNotifications.Add (new DBLaneNotification (reader));

						// 14
						reader.NextResult ();
						if (reader.Read ()) {
							response.Tags = new List<DBLaneTag> ();
							do {
								response.Tags.Add (new DBLaneTag (reader));
							} while (reader.Read ());
						}
					}
				}

				return response;
			}
		}

		private DBLane FindLane (List<DBLane> lanes, int? lane_id, string lane)
		{
			if (lane_id.HasValue) {
				return lanes.Find ((DBLane l) => l.id == lane_id.Value);
			} else if (string.IsNullOrEmpty (lane)) {
				return null;
			} else {
				return lanes.Find ((DBLane l) => l.lane == lane);
			}
		}

		private DBLane FindLane (DB db, int? lane_id, string lane)
		{
			if ((lane_id == null || lane_id.Value <= 0) && string.IsNullOrEmpty (lane))
				return null;

			using (IDbCommand cmd = db.CreateCommand ()) {

				if (!lane_id.HasValue) {
					cmd.CommandText = "SELECT * FROM Lane WHERE lane = @lane;";
					DB.CreateParameter (cmd, "lane", lane);
				} else {
					cmd.CommandText = "SELECT * FROM Lane WHERE id = @id;";
					DB.CreateParameter (cmd, "id", lane_id.Value);
				}

				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						return new DBLane (reader);
				}
			}

			return null;
		}

		private int FindLaneId (DB db, int? lane_id, string lane)
		{
			DBLane l;
			if (lane_id.HasValue)
				return lane_id.Value;
			l = FindLane (db, lane_id, lane);
			if (l == null)
				return 0;
			return l.id;
		}

		/// <summary>
		/// Finds the person with the specified login name. Returns null if the person doesn't exist.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		private DBPerson FindPerson (DB db, string name)
		{
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Person WHERE login = @name";
				DB.CreateParameter (cmd, "name", name);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						return new DBPerson (reader);
				}
			}
			return null;
		}

		[WebMethod]
		public FindHostResponse FindHost (WebServiceLogin login, int? host_id, string host)
		{
			FindHostResponse response = new FindHostResponse ();
			using (DB db = new DB ()) {
				Authenticate (db, login, response);

				response.Host = FindHost (db, host_id, host);

				return response;
			}
		}

		private DBHost FindHost (DB db, int? host_id, string host)
		{
			if ((host_id == null || host_id.Value <= 0) && string.IsNullOrEmpty (host))
				return null;

			using (IDbCommand cmd = db.CreateCommand ()) {

				if (!host_id.HasValue) {
					cmd.CommandText = "SELECT * FROM Host WHERE host = @host;";
					DB.CreateParameter (cmd, "host", host);
				} else {
					cmd.CommandText = "SELECT * FROM Host WHERE id = @id;";
					DB.CreateParameter (cmd, "id", host_id.Value);
				}

				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						return new DBHost (reader);
				}
			}

			return null;
		}

		private DBRevision FindRevision (DB db, int? revision_id, string revision)
		{
			if ((revision_id == null || revision_id.Value <= 0) && string.IsNullOrEmpty (revision))
				return null;

			using (IDbCommand cmd = db.CreateCommand ()) {

				if (!revision_id.HasValue) {
					cmd.CommandText = "SELECT * FROM Revision WHERE revision = @revision;";
					DB.CreateParameter (cmd, "revision", revision);
				} else {
					cmd.CommandText = "SELECT * FROM Revision WHERE id = @id;";
					DB.CreateParameter (cmd, "id", revision_id.Value);
				}

				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						return new DBRevision (reader);
				}
			}

			return null;
		}

		private DBCommand FindCommand (DB db, DBLane lane, int? command_id, string command)
		{
			if ((command_id == null || command_id.Value <= 0) && string.IsNullOrEmpty (command))
				return null;

			using (IDbCommand cmd = db.CreateCommand ()) {

				if (!command_id.HasValue) {
					cmd.CommandText = "SELECT * FROM Command WHERE command = @command";
					DB.CreateParameter (cmd, "command", command);
					cmd.CommandText += " AND lane_id = @lane_id";
					DB.CreateParameter (cmd, "lane_id", lane.id);
				} else {
					cmd.CommandText = "SELECT * FROM Command WHERE id = @id";
					DB.CreateParameter (cmd, "id", command_id.Value);
				}

				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (reader.Read ())
						return new DBCommand (reader);
				}
			}

			return null;
		}

		[WebMethod]
		public FindRevisionResponse FindRevisionForLane (WebServiceLogin login, int? revision_id, string revision, int? lane_id, string lane)
		{
			FindRevisionResponse response = new FindRevisionResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response, true);
				if ((revision_id == null || revision_id.Value <= 0) && string.IsNullOrEmpty (revision))
					return response;

				if ((lane_id == null || lane_id.Value <= 0) && string.IsNullOrEmpty (lane))
					return response;

				using (IDbCommand cmd = db.CreateCommand ()) {
					if (!lane_id.HasValue) {
						if (!revision_id.HasValue) {
							cmd.CommandText = "SELECT * FROM Revision INNER JOIN Lane ON Revision.lane_id = Lane.id WHERE Revision.revision = @revision AND Lane.lane = @lane;";
							DB.CreateParameter (cmd, "revision", revision);
						} else {
							cmd.CommandText = "SELECT * FROM Revision INNER JOIN Lane ON Revision.lane_id = Lane.id WHERE id = @id AND Lane.lane = @lane;";
							DB.CreateParameter (cmd, "id", revision_id.Value);
						}
						DB.CreateParameter (cmd, "lane", lane);
					} else {
						if (!revision_id.HasValue) {
							cmd.CommandText = "SELECT * FROM Revision WHERE revision = @revision AND lane_id = @lane_id;";
							DB.CreateParameter (cmd, "revision", revision);
						} else {
							cmd.CommandText = "SELECT * FROM Revision WHERE id = @id AND lane_id = @lane_id;";
							DB.CreateParameter (cmd, "id", revision_id.Value);
						}
						DB.CreateParameter (cmd, "lane_id", lane_id.Value);
					}
					DB.CreateParameter (cmd, "lane_id", lane_id);

					using (IDataReader reader = cmd.ExecuteReader ()) {
						if (reader.Read ()) {
							response.Revision = new DBRevision (reader);
						}
					}
				}
			}

			return response;
		}

		[WebMethod]
		public FindRevisionResponse FindRevision (WebServiceLogin login, int? revision_id, string revision)
		{
			FindRevisionResponse response = new FindRevisionResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response);
				response.Revision = FindRevision (db, revision_id, revision);
			}

			return response;
		}

		[WebMethod]
		public FindLaneResponse FindLane (WebServiceLogin login, int? lane_id, string lane)
		{
			FindLaneResponse response = new FindLaneResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response);

				if (response.lane == null)
					response.lane = FindLane (db, lane_id, lane);

				return response;
			}
		}

		[WebMethod]
		public FindLaneWithDependenciesResponse FindLaneWithDependencies (WebServiceLogin login, int? lane_id, string lane)
		{
			var response = new FindLaneWithDependenciesResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response);

				response.lane = FindLane (db, lane_id, lane);
				if (response.lane != null)
					response.dependencies = response.lane.GetDependentLanes (db);

				Logger.Log ("*** * *** FindLaneWithDependencies for {0}: {1} results\n", response.lane.id, response.dependencies.Count);

				return response;
			}
		}

		[WebMethod]
		public void EditLane (WebServiceLogin login, DBLane lane)
		{
			//WebServiceResponse response = new WebServiceResponse ();
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				lane.Save (db);
			}
		}

		[WebMethod]
		public void EditLaneWithTags (WebServiceLogin login, DBLane lane, string[] tags)
		{
			Logger.Log ("EditLaneWithTags ({0}, {1})", lane.id, tags == null ? "null" : tags.Length.ToString ());
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				lane.Save (db);

				using (var cmd = db.CreateCommand ()) {
					var cmdText = new StringBuilder ();
					cmdText.AppendFormat ("DELETE FROM LaneTag WHERE lane_id = {0};", lane.id).AppendLine ();
					if (tags != null) {
						for (int i = 0; i < tags.Length; i++) {
							cmdText.AppendFormat ("INSERT INTO LaneTag (lane_id, tag) VALUES ({0}, @tag{1});", lane.id, i).AppendLine ();
							DB.CreateParameter (cmd, "tag" + i.ToString (), tags [i]);
						}
					}
					cmd.CommandText = cmdText.ToString ();
					cmd.ExecuteNonQuery ();
				}
			}
		}

		[WebMethod]
		[Obsolete]
		public void EditHost (WebServiceLogin login, DBHost host)
		{
			//WebServiceResponse response = new WebServiceResponse ();
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				host.Save (db);
			}
		}

		[WebMethod]
		public void EditHostWithPassword (WebServiceLogin login, DBHost host, string password)
		{
			using (DB db = new DB ()) {
				using (IDbTransaction transaction = db.BeginTransaction ()) {
					VerifyUserInRole (db, login, Roles.Administrator);
					host.Save (db);

					// NOTE: it is possible to change the password of an existing account by creating 
					// a host with the same name and specify the password. Given that admin rights
					// are required to create/modify hosts, it shouldn't pose a security issue.

					// TODO: if host changed name, delete the old user account.
					DBPerson person = FindPerson (db, host.host);

					if (person == null) {
						person = new DBPerson ();
						person.login = host.host;
						person.roles = Roles.BuildBot;
					} else {
						if (person.roles != Roles.BuildBot)
							throw new ArgumentException ("The hosts entry in the person table must have its roles set to 'BuildBot'.");
					}
					person.password = password;
					person.Save (db);
					transaction.Commit ();
				}
			}
		}

		[WebMethod]
		public GetViewLaneDataResponse GetViewLaneData (WebServiceLogin login, int? lane_id, string lane, int? host_id, string host, int? revision_id, string revision)
		{
			return GetViewLaneData2 (login, lane_id, lane, host_id, host, revision_id, revision, true);
		}

		[WebMethod]
		public GetViewLaneDataResponse GetViewLaneData2 (WebServiceLogin login, int? lane_id, string lane, int? host_id, string host, int? revision_id, string revision, bool include_hidden_files)
		{
			GetViewLaneDataResponse response = new GetViewLaneDataResponse ();
			try {
				using (DB db = new DB ()) {
					Authenticate (db, login, response);

					response.Now = db.Now;
					response.Lane = FindLane (db, lane_id, lane);
					response.Host = FindHost (db, host_id, host);
					response.Revision = FindRevision (db, revision_id, revision);
					response.RevisionWork = DBRevisionWork_Extensions.Find (db, response.Lane, response.Host, response.Revision);
					if (response.RevisionWork != null && response.RevisionWork.workhost_id.HasValue) {
						response.WorkHost = FindHost (db, response.RevisionWork.workhost_id, null);
					}
					response.WorkViews = db.GetWork (response.RevisionWork);
					response.WorkFileViews = new List<List<DBWorkFileView>> ();
					for (int i = 0; i < response.WorkViews.Count; i++) {
						response.WorkFileViews.Add (DBWork_Extensions.GetFiles (db, response.WorkViews [i].id, include_hidden_files));
					}
					response.Links = DBWork_Extensions.GetLinks (db, response.WorkViews.Select<DBWorkView2, int> ((DBWorkView2 a, int b) => a.id));
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}


		[WebMethod]
		public FrontPageResponse GetFrontPageData (WebServiceLogin login, int limit, string lane, int? lane_id)
		{
			if (lane_id.HasValue) {
				return GetFrontPageData2 (login, limit, new string [] { lane }, new int [] { lane_id.Value });
			} else {
				return GetFrontPageData2 (login, limit, new string [] { lane }, null);
			}
		}

		[WebMethod]
		public FrontPageResponse GetFrontPageData2 (WebServiceLogin login, int limit, string [] lanes, int [] lane_ids)
		{
			return GetFrontPageData3 (login, limit, 0, lanes, lane_ids);
		}
		[WebMethod]
		public FrontPageResponse GetFrontPageData3 (WebServiceLogin login, int page_size, int page, string [] lanes, int [] lane_ids)
		{
			return GetFrontPageData4 (login, page_size, page, lanes, lane_ids, 30);
		}

		[WebMethod]
		public FrontPageResponse GetFrontPageData4 (WebServiceLogin login, int page_size, int page, string [] lanes, int [] lane_ids, int latest_days)
		{
			return GetFrontPageDataWithTags (login, page_size, page, lanes, lane_ids, latest_days, null);
		}

		// for some unknown reason we receive all elements in int? [] arrays with HasValue = false:
		[WebMethod]
		public FrontPageResponse GetFrontPageDataWithTags (WebServiceLogin login, int page_size, int page, string [] lanes, int [] lane_ids, int latest_days, string[] tags)
		{
			FrontPageResponse response = new FrontPageResponse ();
			List<DBLane> Lanes;
			List<DBHost> Hosts;
			List<DBHostLane> HostLanes;
			List<int> TaggedLaneIds = null;

			Logger.Log ("GetFrontPageDataWithTags (page_size: {0} page: {1} lanes: {2} lane_ids: {3} latest_days: {4} tags: {5})",
				page_size, page, lanes == null ? "null" : lanes.Length.ToString (), lane_ids == null ? "null" : lane_ids.Length.ToString (), latest_days, tags == null ? "null" : tags.Length.ToString ());

			page_size = Math.Min (page_size, 500);

			string single_lane = string.Empty;
			if ((lanes != null && lanes.Length == 1))
				single_lane = lanes [0];

			try {
				using (DB db = new DB ()) {
					Authenticate (db, login, response, true);

					using (IDbCommand cmd = db.CreateCommand ()) {
						var latest_only = latest_days != 0;
						var last_month = string.Empty;

						if (!string.IsNullOrEmpty (single_lane)) {
							// this will ignore the @afterdate condition below if the selected lane is not a parent of other lanes
							last_month = " AND (NOT EXISTS (SELECT id FROM Lane WHERE parent_lane_id = (SELECT id FROM Lane WHERE lane = @single_lane)) OR \n";
							DB.CreateParameter (cmd, "single_lane", single_lane);
						} else {
							last_month = " AND (";
						}

						last_month += @"
			(
				EXISTS (SELECT id FROM Revision WHERE date > @afterdate AND lane_id = Lane.id)
					OR
				EXISTS (SELECT id FROM RevisionWork WHERE lane_id = Lane.id AND ((completed = TRUE AND endtime > @afterdate) OR (state <> 9 AND state <> 11 AND completed = FALSE)))
					OR
				NOT EXISTS (SELECT id FROM Revision WHERE lane_id = Lane.id)
					OR
				EXISTS (SELECT id FROM Lane AS ParentLane WHERE ParentLane.parent_lane_id = Lane.id)
			))";
						/*
						 */

						cmd.CommandText = "SELECT * FROM Lane WHERE enabled = TRUE";
						if (latest_only)
							cmd.CommandText += last_month;
						cmd.CommandText += ";\n";
						cmd.CommandText += "SELECT * FROM Host;\n";
						cmd.CommandText += @"
SELECT HostLane.*
FROM HostLane
INNER JOIN Lane ON Lane.id = HostLane.lane_id
WHERE hidden = false AND Lane.enabled = TRUE";
						if (latest_only)
							cmd.CommandText += last_month;
						cmd.CommandText += ";\n";
						if (latest_only)
							DB.CreateParameter (cmd, "afterdate", DateTime.Now.AddDays (-latest_days));
						if (tags != null && tags.Length > 0) {
							cmd.CommandText += "SELECT DISTINCT lane_id FROM LaneTag WHERE ";
							for (int i = 0; i < tags.Length; i++) {
								if (i > 0)
									cmd.CommandText += " OR ";
								cmd.CommandText += " tag = @tag" + i.ToString ();
								DB.CreateParameter (cmd, "tag" + i.ToString (), tags [i]);
							}
							cmd.CommandText += ";";
						}
						cmd.CommandText = "SET enable_seqscan = false;\n" + cmd.CommandText + "\nSET enable_seqscan = true;\n";
						using (IDataReader reader = cmd.ExecuteReader ()) {
							Lanes = DBRecord.LoadMany<DBLane> (reader);

							reader.NextResult ();
							Hosts = DBRecord.LoadMany<DBHost> (reader);

							reader.NextResult ();
							HostLanes = DBRecord.LoadMany<DBHostLane> (reader);

							if (tags != null && tags.Length > 0) {
								reader.NextResult ();
								TaggedLaneIds = new List<int> (tags.Length);
								while (reader.Read ())
									TaggedLaneIds.Add (reader.GetInt32 (0));
							}
						}
					}

					// get a list of the lanes to show
					// note that the logic here is slightly different from the usual "string lane, int? lane_id" logic in other methods,
					// where we only use the string parameter if the id parameter isn't provided, here we add everything we can to the 
					// list of selected lanes, so if you provide both a string and an id parameter both are used (assuming they correspond
					// with different lanes of course).
					response.SelectedLanes = Lanes.FindAll (delegate (DBLane l) {
						if (lane_ids != null) {
							for (int i = 0; i < lane_ids.Length; i++) {
								if (lane_ids [i] == l.id)
									return true;
							}
						}
						if (lanes != null) {
							for (int i = 0; i < lanes.Length; i++) {
								if (!string.IsNullOrEmpty (lanes [i]) && lanes [i] == l.lane)
									return true;
							}
						}
						if (TaggedLaneIds != null) {
							if (TaggedLaneIds.Contains (l.id))
								return true;
						}
						return false;
					});

					Logger.Log ("We have {0} selected lanes", response.SelectedLanes.Count);

					// backwards compat
					if (response.SelectedLanes.Count == 1)
						response.Lane = response.SelectedLanes [0];

					response.RevisionWorkViews = new List<List<DBRevisionWorkView2>> (HostLanes.Count);
					response.RevisionWorkHostLaneRelation = new List<int> (HostLanes.Count);

					if (HostLanes.Count > 0) {
						using (IDbCommand cmd = db.CreateCommand ()) {
							// FIXME: use this instead: https://gist.github.com/rolfbjarne/cf73bf22209c8a8ef844

							for (int i = 0; i < HostLanes.Count; i++) {
								DBHostLane hl = HostLanes [i];

								var stri = i.ToString ();
								cmd.CommandText += @"SELECT R.* FROM (" + DBRevisionWorkView2.SQL.Replace (';', ' ') + ") AS R WHERE " +
									"R.host_id = @host_id" + stri + " AND R.lane_id = @lane_id" + stri + " LIMIT @limit OFFSET @offset;\n";
								DB.CreateParameter (cmd, "host_id" + stri, hl.host_id);
								DB.CreateParameter (cmd, "lane_id" + stri, hl.lane_id);

								response.RevisionWorkHostLaneRelation.Add (hl.id);
							}

							DB.CreateParameter (cmd, "limit", page_size);
							DB.CreateParameter (cmd, "offset", page * page_size);

							using (IDataReader reader = cmd.ExecuteReader ()) {
								do {
									response.RevisionWorkViews.Add (DBRecord.LoadMany<DBRevisionWorkView2> (reader));
								} while (reader.NextResult ());
							}
						}
					}

					// Create a list of all the lanes which have hostlanes
					var enabled_set = new HashSet<int> ();
					foreach (DBHostLane hl in HostLanes) {
						if (enabled_set.Contains (hl.lane_id))
							continue;
						enabled_set.Add (hl.lane_id);

						// Walk up the tree of parent lanes, marking all the parents too
						var l = Lanes.FirstOrDefault ((v) => v.id == hl.lane_id);
						if (l == null) {
							Logger.Log ("GetFrontPageDataWithTags: could not find lane {0} for host lane {1}", hl.lane_id, hl.id);
							l = DBLane_Extensions.Create (db, hl.lane_id);
							l.enabled = true; // This will prevent us from having to load the lane manually again.
							l.Save (db);
							Lanes.Add (l);
							continue;
						}
						while (true) {
							if (!l.parent_lane_id.HasValue)
								break;

							if (enabled_set.Contains (l.parent_lane_id.Value))
								break;

							enabled_set.Add (l.parent_lane_id.Value);

							var old_l = l;
							l = Lanes.FirstOrDefault ((v) => v.id == l.parent_lane_id.Value);
							if (l == null) {
								Logger.Log ("GetFrontPageDataWithTags: could not find parent lane {0} for lane {1} (host lane {2})", old_l.parent_lane_id.Value, old_l.id, hl.id);
								l = DBLane_Extensions.Create (db, old_l.parent_lane_id.Value);
								l.enabled = true; // This will prevent us from having to load the lane manually again.
								l.Save (db);
								Lanes.Add (l);
								break;
							}
						}
					}

					// Remove the lanes which aren't marked
					for (int i = Lanes.Count - 1; i >= 0; i--) {
						if (!enabled_set.Contains (Lanes [i].id)) {
							Lanes.RemoveAt (i);
						}
					}

					response.Lanes = Lanes;
					response.Hosts = Hosts;
					response.HostLanes = HostLanes;
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}


		[WebMethod]
		public GetLanesResponse GetLanes (WebServiceLogin login)
		{
			GetLanesResponse response = new GetLanesResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response);
				response.Lanes = db.GetAllLanes ();
			}

			return response;
		}

		[WebMethod]
		public GetHostLanesResponse GetHostLanes (WebServiceLogin login)
		{
			GetHostLanesResponse response = new GetHostLanesResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response);
				response.HostLanes = db.GetAllHostLanes ();
			}

			return response;
		}

		[WebMethod]
		public GetHostsResponse GetHosts (WebServiceLogin login)
		{
			GetHostsResponse response = new GetHostsResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response);
				response.Hosts = db.GetHosts ();
			}

			return response;
		}

		[WebMethod]
		public GetHostStatusResponse GetHostStatus (WebServiceLogin login)
		{
			var response = new GetHostStatusResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response);

				response.UploadStatus = Global.UploadStatus;
				response.HostStatus = new List<DBHostStatusView> ();

				using (var cmd = db.CreateCommand ()) {
					cmd.CommandText = @"HostStatusView";
					cmd.CommandType = CommandType.TableDirect;

					using (var reader = cmd.ExecuteReader ()) {
						while (reader.Read ()) {
							response.HostStatus.Add (new DBHostStatusView (reader));
						}
					}
				}
			}

			return response;
		}

		[WebMethod]
		public GetLeftTreeDataResponse GetLeftTreeData (WebServiceLogin login)
		{
			var response = new GetLeftTreeDataResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response, true);
				using (var cmd = db.CreateCommand ()) {
					var sql = new StringBuilder ();
					sql.AppendLine ("SELECT * FROM Lane ORDER BY lane;");
					sql.AppendLine ("SELECT * FROM HostStatusView;");
					sql.AppendLine ("SELECT DISTINCT tag FROM LaneTag ORDER BY tag;");
					cmd.CommandText = sql.ToString ();

					using (var reader = cmd.ExecuteReader ()) {
						response.Lanes = DBRecord.LoadMany<DBLane> (reader);

						reader.NextResult ();
						response.HostStatus = DBRecord.LoadMany<DBHostStatusView> (reader);

						reader.NextResult ();
						response.Tags = new List<string> ();
						while (reader.Read ())
							response.Tags.Add (reader.GetString (0));
					}

					response.UploadStatus = Global.UploadStatus;
				}
			}

			return response;
		}

		[WebMethod]
		public GetRevisionsResponse GetRevisions (WebServiceLogin login, int? lane_id, string lane, int limit, int offset)
		{
			GetRevisionsResponse response = new GetRevisionsResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response, true);
				response.Revisions = db.GetDBRevisions (FindLaneId (db, lane_id, lane), limit, offset);
			}

			return response;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="login"></param>
		/// <param name="lane_id">You can pass 0 to get commands for all lanes</param>
		/// <returns></returns>
		[WebMethod]
		public GetCommandsResponse GetCommands (WebServiceLogin login, int lane_id)
		{
			GetCommandsResponse response = new GetCommandsResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response, true);
				response.Commands = db.GetCommands (lane_id);
			}

			return response;
		}

		[WebMethod]
		public int CloneLane (WebServiceLogin login, int lane_id, string new_name, bool copy_files)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				return db.CloneLane (lane_id, new_name, copy_files).id;
			}
		}

		[WebMethod]
		public void DeleteLane (WebServiceLogin login, int lane_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBLane_Extensions.Delete (db, lane_id);
			}
		}

		[WebMethod]
		public int AddLane (WebServiceLogin login, string lane)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);

				if (string.IsNullOrEmpty (lane))
					throw new ArgumentOutOfRangeException ("name");


				for (int i = 0; i < lane.Length; i++) {
					if (char.IsLetterOrDigit (lane [i])) {
						continue;
					} else if (lane [i] == '-' || lane [i] == '_' || lane [i] == '.') {
						continue;
					} else {
						throw new ArgumentOutOfRangeException (string.Format ("The character '{0}' isn't valid.", lane [i]));
					}
				}

				if (db.LookupLane (lane, false) != null)
					throw new ApplicationException (string.Format ("The lane '{0}' already exists.", lane));

				DBLane dblane = new DBLane ();
				dblane.lane = lane;
				dblane.source_control = "svn";
				dblane.Save (db);
				return dblane.id;
			}
		}

		[WebMethod]
		public void DeleteHost (WebServiceLogin login, int host_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBHost_Extensions.Delete (db, host_id);
			}
		}

		[WebMethod]
		public int AddHost (WebServiceLogin login, string host)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);

				if (string.IsNullOrEmpty (host))
					throw new ArgumentNullException ("name");

				for (int i = 0; i < host.Length; i++) {
					if (char.IsLetterOrDigit (host [i])) {
						continue;
					} else if (host [i] == '-' || host [i] == '_') {
						continue;
					} else {
						throw new ArgumentOutOfRangeException (string.Format ("The character '{0}' isn't valid.", host [i]));
					}
				}

				DBHost dbhost = new DBHost ();
				dbhost.host = host;
				dbhost.enabled = true;
				dbhost.Save (db);
				return dbhost.id;
			}
		}

		[WebMethod]
		public void IgnoreRevision (WebServiceLogin login, int lane_id, int host_id, int revision_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				db.Audit (login, "WebServices.IgnoreRevision (lane_id: {0}, host_id: {1}, revision_id: {2})", lane_id, host_id, revision_id);
				db.IgnoreWork (lane_id, revision_id, host_id);
			}
		}

		[WebMethod]
		public void ClearRevision (WebServiceLogin login, int lane_id, int host_id, int revision_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				db.Audit (login, "WebServices.ClearRevision (lane_id: {0}, host_id: {1}, revision_id: {2})", lane_id, host_id, revision_id);
				db.DeleteFiles (host_id, lane_id, revision_id);
				db.DeleteLinks (host_id, lane_id, revision_id);
				db.ClearWork (lane_id, revision_id, host_id);
			}
		}

		[WebMethod]
		public void RescheduleRevision (WebServiceLogin login, int lane_id, int host_id, int revision_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				db.Audit (login, "WebServices.RescheduleRevision (lane_id: {0}, host_id: {1}, revision_id: {2})", lane_id, host_id, revision_id);
				db.DeleteFiles (host_id, lane_id, revision_id);
				db.DeleteLinks (host_id, lane_id, revision_id);
				db.ClearWork (lane_id, revision_id, host_id);
				db.DeleteWork (lane_id, revision_id, host_id);
			}
		}

		[WebMethod]
		public WebServiceResponse ClearAllWorkForHost (WebServiceLogin login, int host_id)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = @"
UPDATE Work SET state = DEFAULT, summary = DEFAULT, starttime = DEFAULT, endtime = DEFAULT, duration = DEFAULT, logfile = DEFAULT, host_id = DEFAULT
WHERE Work.revisionwork_id IN (SELECT RevisionWork.id FROM RevisionWork WHERE RevisionWork.host_id = @host_id);

UPDATE RevisionWork SET state = DEFAULT, lock_expires = DEFAULT, completed = DEFAULT, workhost_id = DEFAULT WHERE host_id = @host_id;
";
						DB.CreateParameter (cmd, "host_id", host_id);
						cmd.ExecuteNonQuery ();
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}


		[WebMethod]
		public WebServiceResponse ClearAllWorkForLane (WebServiceLogin login, int lane_id)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = @"
UPDATE Work SET state = DEFAULT, summary = DEFAULT, starttime = DEFAULT, endtime = DEFAULT, duration = DEFAULT, logfile = DEFAULT, host_id = DEFAULT
WHERE Work.revisionwork_id IN (SELECT RevisionWork.id FROM RevisionWork WHERE RevisionWork.lane_id = @lane_id);

UPDATE RevisionWork SET state = DEFAULT, lock_expires = DEFAULT, completed = DEFAULT, workhost_id = DEFAULT WHERE lane_id = @lane_id;
";
						DB.CreateParameter (cmd, "lane_id", lane_id);
						cmd.ExecuteNonQuery ();
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse DeleteAllWorkForHost (WebServiceLogin login, int host_id)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = @"
DELETE FROM Work WHERE revisionwork_id IN (SELECT id FROM RevisionWork WHERE host_id = @host_id);
UPDATE RevisionWork SET state = 10, workhost_id = DEFAULT, completed = DEFAULT WHERE host_id = @host_id;
";
						DB.CreateParameter (cmd, "host_id", host_id);
						cmd.ExecuteNonQuery ();
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse DeleteAllWorkForLane (WebServiceLogin login, int lane_id)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = @"
DELETE FROM Work WHERE revisionwork_id IN (SELECT id FROM RevisionWork WHERE lane_id = @lane_id);
UPDATE RevisionWork SET state = 10, workhost_id = DEFAULT, completed = DEFAULT WHERE lane_id = @lane_id;
";
						DB.CreateParameter (cmd, "lane_id", lane_id);
						cmd.ExecuteNonQuery ();
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse DeleteAllRevisionsForLane (WebServiceLogin login, int lane_id)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = @"DELETE FROM Revision WHERE lane_id = @lane_id;";
						DB.CreateParameter (cmd, "lane_id", lane_id);
						cmd.ExecuteNonQuery ();
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}
		[WebMethod]
		public void AbortRevision (WebServiceLogin login, int lane_id, int host_id, int revision_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"
UPDATE RevisionWork SET state = @state, completed = true WHERE lane_id = @lane_id AND revision_id = @revision_id AND host_id = @host_id;
UPDATE Work SET state = @state WHERE Work.revisionwork_id = (SELECT RevisionWork.id FROM RevisionWork WHERE lane_id = @lane_id AND revision_id = @revision_id AND host_id = @host_id);";
					DB.CreateParameter (cmd, "lane_id", lane_id);
					DB.CreateParameter (cmd, "revision_id", revision_id);
					DB.CreateParameter (cmd, "host_id", host_id);
					DB.CreateParameter (cmd, "state", (int) DBState.Aborted);
					cmd.ExecuteNonQuery ();
				}
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"SELECT * FROM RevisionWork WHERE lane_id = @lane_id AND revision_id = @revision_id AND host_id = @host_id";
					DB.CreateParameter (cmd, "lane_id", lane_id);
					DB.CreateParameter (cmd, "revision_id", revision_id);
					DB.CreateParameter (cmd, "host_id", host_id);
				}
			}
		}

		[WebMethod]
		public void ClearWork (WebServiceLogin login, int work_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBWork_Extensions.Clear (db, work_id);
			}
		}

		[WebMethod]
		public void AbortWork (WebServiceLogin login, int work_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBWork_Extensions.Abort (db, work_id);
			}
		}

		[WebMethod]
		public void PauseWork (WebServiceLogin login, int work_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBWork_Extensions.Pause (db, work_id);
			}
		}

		[WebMethod]
		public void ResumeWork (WebServiceLogin login, int work_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBWork_Extensions.Resume (db, work_id);
			}
		}

		[WebMethod]
		public GetViewTableDataResponse GetViewTableData (WebServiceLogin login, int? lane_id, string lane, int? host_id, string host, int page, int page_size)
		{
			GetViewTableDataResponse response = new GetViewTableDataResponse ();

			try {
				using (DB db = new DB ()) {
					Authenticate (db, login, response);
					response.Lane = FindLane (db, lane_id, lane);
					response.Host = FindHost (db, host_id, host);
					response.Count = DBRevisionWork_Extensions.GetCount (db, response.Lane.id, response.Host.id);
					response.Page = page;
					response.PageSize = page_size;
					response.RevisionWorkViews = DBRevisionWorkView_Extensions.Query (db, response.Lane, response.Host, response.PageSize, response.Page);
					var hl = db.GetHostLane (response.Host.id, response.Lane.id);
					if (hl != null)
						response.Enabled = hl.enabled;
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public GetViewWorkTableDataResponse GetViewWorkTableData (WebServiceLogin login, int? lane_id, string lane, int? host_id, string host, int? command_id, string command)
		{
			GetViewWorkTableDataResponse response = new GetViewWorkTableDataResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response);

				response.Host = FindHost (db, host_id, host);
				response.Lane = FindLane (db, lane_id, lane);
				response.Command = FindCommand (db, response.Lane, command_id, command);
				response.WorkViews = new List<DBWorkView2> ();
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"
SELECT * 
FROM WorkView2
WHERE command_id = @command_id AND masterhost_id = @host_id AND lane_id = @lane_id
ORDER BY date DESC LIMIT 250;
";
					DB.CreateParameter (cmd, "command_id", response.Command.id);
					DB.CreateParameter (cmd, "host_id", response.Host.id);
					DB.CreateParameter (cmd, "lane_id", response.Lane.id);
					using (IDataReader reader = cmd.ExecuteReader ()) {
						while (reader.Read ())
							response.WorkViews.Add (new DBWorkView2 (reader));
					}
				}
				response.WorkFileViews = new List<List<DBWorkFileView>> ();
				for (int i = 0; i < response.WorkViews.Count; i++) {
					// This takes too long when there are many files: response.WorkFileViews.Add (DBWork_Extensions.GetFiles (db, response.WorkViews [i].id, false));
					response.WorkFileViews.Add (new List<DBWorkFileView> ());
				}
			}

			return response;
		}

		[WebMethod]
		public GetLaneFileForEditResponse GetLaneFileForEdit (WebServiceLogin login, int lanefile_id)
		{
			GetLaneFileForEditResponse response = new GetLaneFileForEditResponse ();

			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				response.Lanefile = DBLanefile_Extensions.Create (db, lanefile_id);
				response.Lanes = DBLanefile_Extensions.GetLanesForFile (db, response.Lanefile);
			}

			return response;
		}

		[WebMethod]
		public void EditLaneFile (WebServiceLogin login, DBLanefile lanefile)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);

				DBLanefile original = DBLanefile_Extensions.Create (db, lanefile.id);

				if (original.original_id == null) {// This is the latest version of the file
					DBLanefile old_file = new DBLanefile ();
					old_file.contents = lanefile.contents;
					old_file.mime = lanefile.mime;
					old_file.name = lanefile.name;
					old_file.original_id = lanefile.id;
					old_file.changed_date = DBRecord.DatabaseNow;
					old_file.Save (db);

					lanefile.Save (db);
				} else {
					throw new ApplicationException ("You can only change the newest version of a lane file.");
				}
			}
		}

		[WebMethod]
		public GetViewLaneFileHistoryDataResponse GetViewLaneFileHistoryData (WebServiceLogin login, int lanefile_id)
		{
			GetViewLaneFileHistoryDataResponse response = new GetViewLaneFileHistoryDataResponse ();

			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);

				response.Lanefiles = new List<DBLanefile> ();

				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = "SELECT * FROM LaneFile WHERE original_id = @lanefile_id;";
					DB.CreateParameter (cmd, "lanefile_id", lanefile_id);
					using (IDataReader reader = cmd.ExecuteReader ()) {
						while (reader.Read ()) {
							response.Lanefiles.Add (new DBLanefile (reader));
						}
					}
				}
			}

			return response;
		}

		[WebMethod]
		public GetUsersResponse GetUsers (WebServiceLogin login)
		{
			GetUsersResponse response = new GetUsersResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);
					
					response.Users = DBPerson_Extensions.GetAll (db);
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse DeleteUser (WebServiceLogin login, int id)
		{
			WebServiceResponse response = new WebServiceResponse ();
			
			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);

					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = "DELETE FROM Person WHERE id = @id;";
						DB.CreateParameter (cmd, "id", id);
						cmd.ExecuteNonQuery ();
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}
			return response;
		}

		[WebMethod]
		public WebServiceResponse AddUserEmail (WebServiceLogin login, int? id, string username, string email)
		{
			DBPerson user;
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					Authenticate (db, login, response, true);

					user = FindUser (db, id, username);
					if (user == null) {
						/* user doesn't exist */
						response.Exception = new WebServiceException (new HttpException (403, "You're not allowed to edit this user"));
					} else if (Utilities.IsInRole (response, Roles.Administrator)) {
						/* admin editing (or adming editing self) */
						user.AddEmail (db, email);
					} else if (response.UserName == user.login) {
						/* editing self */
						user.AddEmail (db, email);
					} else {
						/* somebody else editing some other person */
						response.Exception = new WebServiceException (new HttpException (403, "You're not allowed to edit this user"));
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse RemoveUserEmail (WebServiceLogin login, int? id, string username, string email)
		{
			WebServiceResponse response = new WebServiceResponse ();
			DBPerson user;

			try {
				using (DB db = new DB ()) {
					Authenticate (db, login, response, true);

					user = FindUser (db, id, username);

					if (user == null) {
						/* user doesn't exist */
						response.Exception = new WebServiceException (new HttpException (403, "You're not allowed to edit this user"));
					} else if (Utilities.IsInRole (response, Roles.Administrator)) {
						/* admin editing (or adming editing self) */
						user.RemoveEmail (db, email);
					} else if (response.UserName == user.login) {
						/* editing self */
						user.RemoveEmail (db, email);
					} else {
						/* somebody else editing some other person */
						response.Exception = new WebServiceException (new HttpException (403, "You're not allowed to edit this user"));
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse EditUser (WebServiceLogin login, DBPerson user)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					Authenticate (db, login, response, true);
					
					if (user.id == 0) {
						/* new user, anybody can create new users */
						/* create a new person object, and only copy over the fields self is allowed to edit */

						if (string.IsNullOrEmpty (user.password) || user.password.Length < 8) {
							response.Exception = new WebServiceException ("Password must be at least 8 characters long");
							return response;
						}

						DBPerson person = new DBPerson ();
						person.fullname = user.fullname;
						person.login = user.login;
						person.password = user.password;
						person.irc_nicknames = user.irc_nicknames;
						person.Save (db);
					} else {
						if (Utilities.IsInRole (response, Roles.Administrator)) {
							/* admin editing (or adming editing self) */
							user.Save (db); // no restrictions
						} else if (response.UserName == user.login) {
							/* editing self */
							/* create another person object, and only copy over the fields self is allowed to edit */
							DBPerson person = DBPerson_Extensions.Create (db, user.id);
							person.fullname = user.fullname;
							person.password = user.password;
							person.irc_nicknames = user.irc_nicknames;
							person.Save (db);
						} else {
							/* somebody else editing some other person */
							response.Exception = new WebServiceException (new HttpException (403, "You're not allowed to edit this user"));
						}
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		private DBPerson FindUser (DB db, int? id, string username)
		{
			if (!id.HasValue) {
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = "SELECT * FROM Person WHERE login = @login;";
					DB.CreateParameter (cmd, "login", username);
					using (IDataReader reader = cmd.ExecuteReader ()) {
						if (reader.Read ())
							return new DBPerson (reader);
					}
				}
			} else {
				return DBPerson_Extensions.Create (db, id.Value);
			}

			return null;
		}

		[WebMethod]
		public GetUserResponse GetUser (WebServiceLogin login, int? id, string username)
		{
			DBPerson result = null;
			GetUserResponse response = new GetUserResponse ();

			try {
				using (DB db = new DB ()) {
					Authenticate (db, login, response, true);

					if (!id.HasValue) {
						using (IDbCommand cmd = db.CreateCommand ()) {
							cmd.CommandText = "SELECT * FROM Person WHERE login = @login;";
							DB.CreateParameter (cmd, "login", username);
							using (IDataReader reader = cmd.ExecuteReader ()) {
								if (reader.Read ())
									result = new DBPerson (reader);
							}
						}
					} else {
						result = DBPerson_Extensions.Create (db, id.Value);
					}

					if (result != null && (result.login == response.UserName || Utilities.IsInRole (response, Roles.Administrator))) {
						result.Emails = result.GetEmails (db).ToArray ();
						response.User = result;
					} else {
						response.Exception = new WebServiceException (new HttpException (403, "You don't have access to this user's data"));
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public int GetUploadPort ()
		{
			return Upload.GetListenPort ();
		}

		[WebMethod]
		public int AddEnvironmentVariable (WebServiceLogin login, int? lane_id, int? host_id, string name, string value)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);

				DBEnvironmentVariable var = new DBEnvironmentVariable ();
				var.name = name;
				var.value = value;
				var.host_id = host_id;
				var.lane_id = lane_id;
				var.Save (db);
				return var.id;
			}
		}

		[WebMethod]
		public void EditEnvironmentVariable (WebServiceLogin login, DBEnvironmentVariable variable)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				variable.Save (db);
			}
		}

		[WebMethod]
		public void DeleteEnvironmentVariable (WebServiceLogin login, int variable_id)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				DBRecord_Extensions.Delete (db, variable_id, DBEnvironmentVariable.TableName);
			}
		}

		[WebMethod]
		public void UploadCompressedFile (WebServiceLogin login, DBWork work, string filename, byte [] contents, bool hidden, string compressed_mime)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.BuildBot, true);

				string tmp = null;
				try {
					tmp = Path.GetTempFileName ();
					File.WriteAllBytes (tmp, contents);
					work.AddFile (db, tmp, filename, hidden, compressed_mime);
				} finally {
					if (!string.IsNullOrEmpty (tmp)) {
						try {
							File.Delete (tmp);
						} catch {
							// ignore exceptions
						}
					}
				}
			}
		}

		[WebMethod]
		public void UploadLinks (WebServiceLogin login, DBWork work, string [] links)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.BuildBot, true);
				using (var cmd = db.CreateCommand ()) {
					StringBuilder sql = new StringBuilder ();
					for (int i = 0; i < links.Length; i++) {
						sql.AppendFormat ("INSERT INTO FileLink (link, work_id) VALUES (@link{0}, {1});", i, work.id);
						DB.CreateParameter (cmd, "link" + i.ToString (), links [i]);
					}
					cmd.CommandText = sql.ToString ();
					cmd.ExecuteNonQuery ();
				}
			}
		}

		[WebMethod]
		public void UploadFile (WebServiceLogin login, DBWork work, string filename, byte [] contents, bool hidden)
		{
			UploadCompressedFile (login, work, filename, contents, hidden, null);
		}

		/// <summary>
		/// Returns the current state of the specified work
		/// </summary>
		/// <param name="login"></param>
		/// <param name="work"></param>
		/// <returns></returns>
		[WebMethod]
		public DBState GetWorkState (WebServiceLogin login, DBWork work)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.BuildBot, true);
				work.Reload (db);
				return work.State;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="login"></param>
		/// <param name="revisionwork_id"></param>
		/// <param name="command_id">Can be 0 to check all commmands for a revisionwork</param>
		/// <param name="filename"></param>
		/// <returns></returns>
		[WebMethod]
		public GetFilesForWorkResponse GetFilesForWork (WebServiceLogin login, int revisionwork_id, int command_id, string filename)
		{
			GetFilesForWorkResponse response = new GetFilesForWorkResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator, true);

					response.WorkFileIds = new List<List<int>> ();
					response.Files = new List<List<DBFile>> ();
					response.Commands = new List<int> ();

					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = @"
SELECT File.*, Work.command_id, WorkFile.id AS workfile_id FROM File
INNER JOIN WorkFile ON File.id = WorkFile.file_id
INNER JOIN Work ON Work.id = WorkFile.work_id
WHERE Work.revisionwork_id = @revisionwork_id ";
						if (command_id > 0) {
							cmd.CommandText += "AND Work.command_id = @command_id ";
							DB.CreateParameter (cmd, "command_id", command_id);
						}
						if (!string.IsNullOrEmpty (filename)) {
							cmd.CommandText += " AND WorkFile.filename = @filename";
							DB.CreateParameter (cmd, "filename", filename);
						}
						cmd.CommandText += ";";

						DB.CreateParameter (cmd, "revisionwork_id", revisionwork_id);

						using (IDataReader reader = cmd.ExecuteReader ()) {
							int command_id_idx = reader.GetOrdinal ("command_id");
							int workfile_id_idx = reader.GetOrdinal ("workfile_id");
							while (reader.Read ()) {
								List<DBFile> files = null;
								List<int> workfile_ids = null;
								int cmd_id = reader.GetInt32 (command_id_idx);
								int workfile_id = reader.GetInt32 (workfile_id_idx);
								
								for (int i = 0; i < response.Commands.Count; i++) {
									if (response.Commands [i] == cmd_id) {
										files = response.Files [i];
										workfile_ids = response.WorkFileIds [i];
										break;
									}
								}

								if (files == null) {
									files = new List<DBFile> ();
									workfile_ids = new List<int> ();
									response.Files.Add (files);
									response.WorkFileIds.Add (workfile_ids);
									response.Commands.Add (cmd_id);
								}

								files.Add (new DBFile (reader));
								workfile_ids.Add (workfile_id);
							}
						}
					}
				}
			} catch (Exception ex) {
				Logger.Log ("GetFilesForWork exception: {0}", ex);
			}

			return response;
		}

		/// <summary>
		/// Returns true if the matching revisionwork is finished.
		/// </summary>
		/// <param name="login"></param>
		/// <param name="work"></param>
		/// <returns></returns>
		[WebMethod]
		public ReportBuildStateResponse ReportBuildState (WebServiceLogin login, DBWork work)
		{
			ReportBuildStateResponse response = new ReportBuildStateResponse ();

			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.BuildBot, true);
				Logger.Log (2, "ReportBuildState, state: {0}, start time: {1}, end time: {2}", work.State, work.starttime, work.endtime);
				if (work.starttime > new DateTime (2000, 1, 1) && work.endtime < work.starttime) {
					// the issue here is that the server interprets the datetime as local time, while it's always as utc.
					try {
						using (IDbCommand cmd = db.CreateCommand ()) {
							cmd.CommandText = "SELECT starttime FROM Work WHERE id = " + work.id;
							var value = cmd.ExecuteScalar ();
							if (value != null && value is DateTime)
								work.starttime = (DateTime) value;
						}
					} catch (Exception ex) {
						Logger.Log ("ReportBuildState: Exception while fixing timezone data: {0}", ex.Message);
					}
				}
				work.Save (db);
				work.Reload (db);

				response.Work = work;

				DBRevisionWork rw = DBRevisionWork_Extensions.Create (db, work.revisionwork_id);
				bool was_completed = rw.completed;
				rw.UpdateState (db);

				if (!was_completed && rw.completed) {
					rw.endtime = DBRecord.DatabaseNow;
					rw.Save (db);
				}

				Notifications.Notify (work, rw);

				if (!was_completed && rw.completed) {
					var notifyInfo = new GenericNotificationInfo ();
					notifyInfo.laneID = rw.lane_id;
					notifyInfo.hostID = rw.host_id;
					notifyInfo.revisionID = rw.revision_id;
					notifyInfo.message = "Completed";
					notifyInfo.state = rw.State;

					Notifications.NotifyGeneric (notifyInfo);
				}
				response.RevisionWorkCompleted = rw.completed;

				using (var cmd = db.CreateCommand ()) {
					cmd.CommandText = "UPDATE Lane SET changed_date = @date WHERE id = @id;";
					DB.CreateParameter (cmd, "date", DateTime.UtcNow);
					DB.CreateParameter (cmd, "id", rw.lane_id);
					cmd.ExecuteNonQuery ();
				}

				// Check if any other lane depends on this one
				if (response.RevisionWorkCompleted) {
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = "SELECT 1 FROM LaneDependency WHERE dependent_lane_id = @lane_id LIMIT 1;";
						DB.CreateParameter (cmd, "lane_id", rw.lane_id);

						object value = cmd.ExecuteScalar ();
						if (value is int) {
							// If so, run the scheduler
							MonkeyWrench.Scheduler.Scheduler.ExecuteSchedulerAsync (false);
						}
					}
				}

				return response;
			}
		}

		private List<DBHost> GetSlaveHosts (DB db, DBHost host)
		{
			List<DBHost> result = null;

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT Host.* FROM Host INNER JOIN MasterHost ON MasterHost.host_id = Host.id WHERE MasterHost.master_host_id = @host_id";
				DB.CreateParameter (cmd, "host_id", host.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						if (result == null)
							result = new List<DBHost> ();
						result.Add (new DBHost (reader));
					}
				}
			}

			return result;
		}

		private List<DBHost> GetMasterHosts (DB db, DBHost host)
		{
			List<DBHost> result = null;

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT Host.* FROM Host INNER JOIN MasterHost ON MasterHost.master_host_id = Host.id WHERE MasterHost.host_id = @host_id";
				DB.CreateParameter (cmd, "host_id", host.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						if (result == null)
							result = new List<DBHost> ();
						result.Add (new DBHost (reader));
					}
				}
			}

			return result;
		}

		private List<DBMasterHost> FindMasterHosts (DB db, DBHost host)
		{
			List<DBMasterHost> result = null;

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM MasterHost WHERE host_id = @host_id";
				DB.CreateParameter (cmd, "host_id", host.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						if (result == null)
							result = new List<DBMasterHost> ();
						result.Add (new DBMasterHost (reader));
					}
				}
			}

			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="login"></param>
		/// <param name="lane_id"></param>
		/// <param name="revision_id"></param>
		/// <param name="host_id">May be 0 to return revision work for all hosts for this lane/revision</param>
		/// <returns></returns>
		[WebMethod]
		public GetRevisionWorkForLaneResponse GetRevisionWorkForLane (WebServiceLogin login, int lane_id, int revision_id, int host_id)
		{
			GetRevisionWorkForLaneResponse response = new GetRevisionWorkForLaneResponse ();

			using (DB db = new DB ()) {
				Authenticate (db, login, response, true);

				response.RevisionWork = new List<DBRevisionWork> ();
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = "SELECT * FROM RevisionWork WHERE lane_id = @lane_id AND revision_id = @revision_id";
					DB.CreateParameter (cmd, "lane_id", lane_id);
					DB.CreateParameter (cmd, "revision_id", revision_id);
					if (host_id > 0) {
						cmd.CommandText = " AND host_id = @host_id";
						DB.CreateParameter (cmd, "host_id", host_id);
					}
					using (IDataReader reader = cmd.ExecuteReader ()) {
						while (reader.Read ())
							response.RevisionWork.Add (new DBRevisionWork (reader));
					}
				}
			}

			return response;
		}

		[WebMethod]
		public ReportBuildBotStatusResponse ReportBuildBotStatus (WebServiceLogin login, BuildBotStatus status)
		{
			ReportBuildBotStatusResponse response = new ReportBuildBotStatusResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.BuildBot, true);

					Logger.Log (2, "BuildBot '{2}' reported in. v{0}: {1}", status.AssemblyVersion, status.AssemblyDescription, status.Host);

					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = @"
DELETE FROM BuildBotStatus WHERE host_id = (SELECT id FROM Host WHERE host = @host);
INSERT INTO BuildBotStatus (host_id, version, description) VALUES ((SELECT id FROM Host WHERE host = @host), @version, @description);
";
						DB.CreateParameter (cmd, "host", status.Host);
						DB.CreateParameter (cmd, "version", status.AssemblyVersion);
						DB.CreateParameter (cmd, "description", status.AssemblyDescription);
						cmd.ExecuteNonQuery ();
					}
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = "SELECT * FROM Release INNER JOIN Host ON Host.release_id = Release.id WHERE Host.host = @host;";
						DB.CreateParameter (cmd, "host", status.Host);
						using (IDataReader reader = cmd.ExecuteReader ()) {
							if (reader.Read ()) {
								DBRelease release = new DBRelease (reader);
								response.ConfiguredVersion = release.version;
								response.ConfiguredRevision = release.revision;
							}
						}
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public GetBuildBotStatusResponse GetBuildBotStatus (WebServiceLogin login)
		{
			GetBuildBotStatusResponse response = new GetBuildBotStatusResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator, true);

					response.Status = new List<DBBuildBotStatus> ();
					response.Hosts = new List<DBHost> ();
					response.Releases = new List<DBRelease> ();
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = "SELECT * FROM BuildBotStatus; SELECT * FROM Host; SELECT * FROM Release;";
						using (IDataReader reader = cmd.ExecuteReader ()) {
							while (reader.Read ()) {
								response.Status.Add (new DBBuildBotStatus (reader));
							}
							if (reader.NextResult ()) {
								while (reader.Read ()) {
									response.Hosts.Add (new DBHost (reader));
								}
								if (reader.NextResult ()) {
									while (reader.Read ()) {
										response.Releases.Add (new DBRelease (reader));
									}
								}
							}
						}
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public GetBuildInfoResponse GetBuildInfo (WebServiceLogin login, string host)
		{
			return GetBuildInfoMultiple (login, host, false);
		}

		[WebMethod]
		public GetBuildInfoResponse GetBuildInfoMultiple (WebServiceLogin login, string host, bool multiple_work)
		{
			try {
				List<DBHost> hosts = new List<DBHost> (); // list of hosts to find work for
				List<DBHostLane> hostlanes = new List<DBHostLane> ();
				List<DBLane> lanes = new List<DBLane> ();

				GetBuildInfoResponse response = new GetBuildInfoResponse ();

				response.Work = new List<List<BuildInfoEntry>> ();

				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.BuildBot, true);

					response.Host = FindHost (db, null, host);

					if (!response.Host.enabled)
						return response;

					// find the master hosts for this host (if any)
					response.MasterHosts = FindMasterHosts (db, response.Host);

					// get the hosts to find work for
					if (response.MasterHosts != null && response.MasterHosts.Count > 0) {
						foreach (DBMasterHost mh in response.MasterHosts)
							hosts.Add (DBHost_Extensions.Create (db, mh.master_host_id));
					} else {
						hosts.Add (response.Host);
					}

					// find the enabled hostlane combinations for these hosts
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = "SELECT HostLane.* FROM HostLane INNER JOIN Lane ON Lane.id = HostLane.lane_id WHERE Lane.enabled = TRUE AND HostLane.enabled = TRUE AND (";
						for (int i = 0; i < hosts.Count; i++) {
							if (i > 0)
								cmd.CommandText += " OR ";
							cmd.CommandText += " HostLane.host_id = " + hosts [i].id;
						}
						cmd.CommandText += ")";
						using (IDataReader reader = cmd.ExecuteReader ()) {
							while (reader.Read ())
								hostlanes.Add (new DBHostLane (reader));
						}
					}

					if (hostlanes.Count == 0)
						return response; // nothing to do here

					lanes = db.GetAllLanes ();

					switch (response.Host.QueueManagement) {
					case DBQueueManagement.OneRevisionWorkAtATime:
						if (hostlanes.Count > 1) {
							int latest = -1;
							DateTime latest_date = DateTime.MaxValue;

							// we need to find the latest revisionwork each hostlane has completed.
							// we want to work on the hostlane which has waited the longest amount
							// of time without getting work done (but which has pending work to do).

							for (int i = 0; i < hostlanes.Count; i++) {
								DBHostLane hl = hostlanes [i];
								// check if this hostlane has pending work.
								// this would ideally be included in the query below, but I'm not sure
								// how to do that while still distinguising the case where nothing has
								// been done ever for a hostlane.
								using (IDbCommand cmd = db.CreateCommand ()) {
									cmd.CommandText = @"
SELECT RevisionWork.id
FROM RevisionWork
WHERE
        RevisionWork.host_id = @host_id
AND (RevisionWork.workhost_id = @workhost_id OR RevisionWork.workhost_id IS NULL)
AND RevisionWork.completed = false
AND RevisionWork.state <> 9 AND RevisionWork.state <> 10 AND RevisionWork.state <> 11
AND lane_id = @lane_id
LIMIT 1;
        ";
									DB.CreateParameter (cmd, "lane_id", hl.lane_id);
									DB.CreateParameter (cmd, "host_id", hl.host_id);
									DB.CreateParameter (cmd, "workhost_id", response.Host.id);

									object obj = cmd.ExecuteScalar ();
									if (obj == DBNull.Value || obj == null) {
										// there is nothing to do for this hostlane
										continue;
									}

								}

								// find the latest completed (this may not be correct, maybe find the latest unstarted?)
								// revisionwork for this hostlane.
								using (IDbCommand cmd = db.CreateCommand ()) {
									cmd.CommandText = @"
SELECT 	RevisionWork.endtime
FROM RevisionWork
WHERE 
	RevisionWork.host_id = @host_id
AND (RevisionWork.workhost_id = @workhost_id OR RevisionWork.workhost_id IS NULL)
AND RevisionWork.completed = true
AND lane_id = @lane_id
ORDER BY RevisionWork.endtime DESC
LIMIT 1;
	";

									DB.CreateParameter (cmd, "lane_id", hl.lane_id);
									DB.CreateParameter (cmd, "host_id", hl.host_id);
									DB.CreateParameter (cmd, "workhost_id", response.Host.id);

									object obj = cmd.ExecuteScalar ();
									if (obj is DateTime) {
										DateTime dt = (DateTime) obj;
										if (dt < latest_date) {
											latest_date = dt;
											latest = i;
										}
									} else {
										// nothing has ever been done for this hostlane.
										latest_date = DateTime.MinValue;
										latest = i;
									}
								}

							}
							if (latest >= 0) {
								DBHostLane tmp = hostlanes [latest];
								hostlanes.Clear ();
								hostlanes.Add (tmp);
							} else {
								hostlanes.Clear (); // there is nothing to do at all
							}
						}
						break;
					}

					foreach (DBHostLane hl in hostlanes) {
						int counter = 10;
						DBRevisionWork revisionwork;
						DBLane lane = null;
						DBHost masterhost = null;

						foreach (DBLane l in lanes) {
							if (l.id == hl.lane_id) {
								lane = l;
								break;
							}
						}
						foreach (DBHost hh in hosts) {
							if (hh.id == hl.host_id) {
								masterhost = hh;
								break;
							}
						}

						do {
							revisionwork = db.GetRevisionWork (lane, masterhost, response.Host);
							if (revisionwork == null)
								break;
						} while (!revisionwork.SetWorkHost (db, response.Host) && counter-- > 0);

						if (revisionwork == null)
							continue;

						if (!revisionwork.workhost_id.HasValue || revisionwork.workhost_id != response.Host.id)
							continue; // couldn't lock this revisionwork.

						Logger.Log ("Found work for host {0} {4}: {1} (lane: {2} {3})", response.Host.id, revisionwork.id, revisionwork.lane_id, lane.lane, response.Host.host);

						DBRevision revision = DBRevision_Extensions.Create (db, revisionwork.revision_id);
						List<DBWorkFile> files_to_download = null;
						List<DBLane> dependent_lanes = null;

						// get dependent files
						List<DBLaneDependency> dependencies = lane.GetDependencies (db);
						if (dependencies != null && dependencies.Count > 0) {
							foreach (DBLaneDependency dep in dependencies) {
								DBLane dependent_lane;
								DBHost dependent_host;
								DBRevisionWork dep_revwork;
								List<DBWorkFile> work_files;

								if (string.IsNullOrEmpty (dep.download_files))
									continue;

								dependent_lane = DBLane_Extensions.Create (db, dep.dependent_lane_id);
								dependent_host = dep.dependent_host_id.HasValue ? DBHost_Extensions.Create (db, dep.dependent_host_id.Value) : null;
								DBRevision dep_lane_rev = dependent_lane.FindRevision (db, revision.revision);
								if (dep_lane_rev == null)
									continue; /* Something bad happened: the lane we're dependent on does not have the same revisions we have */
								dep_revwork = DBRevisionWork_Extensions.Find (db, dependent_lane, dependent_host, dep_lane_rev);

								work_files = dep_revwork.GetFiles (db);

								foreach (DBWorkFile file in work_files) {
									bool download = true;
									foreach (string exp in dep.download_files.Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
										if (!System.Text.RegularExpressions.Regex.IsMatch (file.filename, FileUtilities.GlobToRegExp (exp))) {
											download = false;
											break;
										}
									}
									if (!download)
										continue;
									if (files_to_download == null) {
										files_to_download = new List<DBWorkFile> ();
										dependent_lanes = new List<DBLane> ();
									}
									files_to_download.Add (file);
									dependent_lanes.Add (dependent_lane);
								}
							}
						}


						List<DBWorkView2> pending_work = revisionwork.GetNextWork (db, lane, masterhost, revision, multiple_work);

						if (pending_work == null || pending_work.Count == 0)
							continue;

						List<DBEnvironmentVariable> environment_variables = null;
						using (IDbCommand cmd = db.CreateCommand ()) {
							foreach (int li in db.GetLaneHierarchy (lane.id)) {
								cmd.CommandText += string.Format (@"
SELECT * 
FROM EnvironmentVariable 
WHERE 
    (host_id = {0} OR host_id = {1} OR host_id IS NULL) AND (lane_id = {2} OR lane_id IS NULL)
ORDER BY id;
;", revisionwork.workhost_id, revisionwork.host_id, li);
								Logger.Log ("SQL to execute:\n{0}", cmd.CommandText);
							}
							using (IDataReader reader = cmd.ExecuteReader ()) {
								var set = new HashSet<string> ();
								do {
									Logger.Log ("Reading result... {0} matches so far", environment_variables == null ? 0 : environment_variables.Count);
									while (reader.Read ()) {
										if (environment_variables == null)
											environment_variables = new List<DBEnvironmentVariable> ();

										var ev = new DBEnvironmentVariable (reader);
										if (!set.Contains (ev.name)) {
											environment_variables.Add (ev);
											set.Add (ev.name);
										}
									}
								} while (reader.NextResult ());
							}
						}

						DBHost host_being_worked_for = hosts.Find (h => h.id == revisionwork.host_id);

						foreach (DBWorkView2 work in pending_work) {
							BuildInfoEntry entry = new BuildInfoEntry ();
							entry.Lane = lane;
							entry.HostLane = hl;
							entry.Revision = revision;
							entry.Command = DBCommand_Extensions.Create (db, work.command_id);
							entry.FilesToDownload = files_to_download;
							entry.DependentLaneOfFiles = dependent_lanes;
							entry.Work = DBWork_Extensions.Create (db, work.id);
							entry.LaneFiles = lane.GetFiles (db, lanes);
							entry.EnvironmentVariables = environment_variables;
							entry.Host = host_being_worked_for;

							// TODO: put work with the same sequence number into one list of entries.
							List<BuildInfoEntry> entries = new List<BuildInfoEntry> ();
							entries.Add (entry);
							response.Work.Add (entries);
						}

						// Notify that the revision is assigned
						var notifyInfo = new GenericNotificationInfo ();
						notifyInfo.laneID = revisionwork.lane_id;
						notifyInfo.hostID = revisionwork.host_id;
						notifyInfo.revisionID = revisionwork.revision_id;
						notifyInfo.message = String.Format("Assigned to host '{0}' ({1})", response.Host.host, response.Host.id);
						notifyInfo.state = DBState.Executing;

						Notifications.NotifyGeneric (notifyInfo);
					}
				}

				return response;
			} catch (Exception ex) {
				Logger.Log ("Exception in GetBuildInfo: {0}", ex);
				throw;
			}
		}

		/// <summary>
		/// Finds the latest workfile id for the search data provided
		/// </summary>
		/// <param name="login"></param>
		/// <param name="lane_id"></param>
		/// <param name="lane"></param>
		/// <param name="filename">The filename to find</param>
		/// <param name="completed">Only completed revisions.</param>
		/// <param name="successful">Only successful (and completed) revisions.</param>
		[WebMethod]
		public int? FindLatestWorkFileId (WebServiceLogin login, int? lane_id, string lane, string filename, bool completed, bool successful)
		{
			using (DB db = new DB ()) {
				DBLane l = FindLane (db, lane_id, lane);
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"
SELECT WorkFile.id
FROM WorkFile
INNER JOIN Work ON WorkFile.work_id = Work.id
INNER JOIN RevisionWork ON RevisionWork.id = Work.revisionwork_id
INNER JOIN Revision ON Revision.id = RevisionWork.revision_id
WHERE Revision.lane_id = @lane_id AND ";
					bool is_glob = filename.IndexOfAny (new char [] {'*', '?'}) >= 0;
					if (is_glob) {
						cmd.CommandText += @" WorkFile.filename LIKE @filename ";
					} else {
						cmd.CommandText += @" WorkFile.filename = @filename ";
					}
					if (successful)
						cmd.CommandText += " AND RevisionWork.result = " + ((int) DBState.Success).ToString () + " ";
					if (completed)
						cmd.CommandText += " AND RevisionWork.completed = true ";

					cmd.CommandText += " ORDER BY Revision.date DESC LIMIT 1;";

					DB.CreateParameter (cmd, "lane_id", l.id);
					DB.CreateParameter (cmd, "filename", is_glob ? filename.Replace ('*', '%').Replace ('?', '_') : filename);

					using (IDataReader reader = cmd.ExecuteReader ()) {
						if (!reader.Read ())
							return null;

						return reader.GetInt32 (0);
					}
				}
			}
		}

		[WebMethod]
		public WebServiceResponse EditIdentity (WebServiceLogin login, DBIrcIdentity irc_identity, DBEmailIdentity email_identity)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);

					if (irc_identity != null) {
						irc_identity.Save (db);
					}
					if (email_identity != null) {
						email_identity.Save (db);
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse RemoveIdentity (WebServiceLogin login, int? irc_identity, int? email_identity)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);

					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = string.Empty;

						if (irc_identity.HasValue) {
							cmd.CommandText += "DELETE FROM IrcIdentity WHERE id = @irc_id;";
							DB.CreateParameter (cmd, "irc_id", irc_identity.Value);
						}
						if (email_identity.HasValue) {
							cmd.CommandText += "DELETE FROM EmailIdentity WHERE id = @email_id;";
							DB.CreateParameter (cmd, "email_id", email_identity.Value);
						}

						cmd.ExecuteNonQuery ();
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public GetIdentitiesResponse GetIdentities (WebServiceLogin login)
		{
			GetIdentitiesResponse response = new GetIdentitiesResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);

					response.EmailIdentities = new List<DBEmailIdentity> ();
					response.IrcIdentities = new List<DBIrcIdentity> ();

					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = "SELECT * FROM IrcIdentity; SELECT * FROM EmailIdentity;";
						using (IDataReader reader = cmd.ExecuteReader ()) {
							while (reader.Read ()) {
								response.IrcIdentities.Add (new DBIrcIdentity (reader));
							}
							if (reader.NextResult ()) {
								while (reader.Read ()) {
									response.EmailIdentities.Add (new DBEmailIdentity (reader));
								}
							}
						}
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse EditNotification (WebServiceLogin login, DBNotification notification)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);
					notification.Save (db);
					Notifications.Restart ();
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse RemoveNotification (WebServiceLogin login, int id)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);

					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = "DELETE FROM Notification WHERE id = @id;";
						DB.CreateParameter (cmd, "id", id);
						cmd.ExecuteNonQuery ();
						Notifications.Restart ();
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public GetNotificationsResponse GetNotifications (WebServiceLogin login)
		{
			GetNotificationsResponse response = new GetNotificationsResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);

					response.EmailIdentities = new List<DBEmailIdentity> ();
					response.IrcIdentities = new List<DBIrcIdentity> ();
					response.Notifications = new List<DBNotification> ();

					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = "SELECT * FROM IrcIdentity; SELECT * FROM EmailIdentity; SELECT * FROM Notification;";
						using (IDataReader reader = cmd.ExecuteReader ()) {
							while (reader.Read ()) {
								response.IrcIdentities.Add (new DBIrcIdentity (reader));
							}
							if (reader.NextResult ()) {
								while (reader.Read ()) {
									response.EmailIdentities.Add (new DBEmailIdentity (reader));
								}
								if (reader.NextResult ()) {
									while (reader.Read ()) {
										response.Notifications.Add (new DBNotification (reader));
									}
								}
							}
						}
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse AddLaneNotification (WebServiceLogin login, int lane_id, int notification_id)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);

					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = "INSERT INTO LaneNotification (lane_id, notification_id) VALUES (@lane_id, @notification_id);";
						DB.CreateParameter (cmd, "lane_id", lane_id);
						DB.CreateParameter (cmd, "notification_id", notification_id);
						cmd.ExecuteNonQuery ();
						Notifications.Restart ();
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse RemoveLaneNotification (WebServiceLogin login, int id)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);

					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = "DELETE FROM LaneNotification WHERE id = @id;";
						DB.CreateParameter (cmd, "id", id);
						cmd.ExecuteNonQuery ();
						Notifications.Restart ();
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse AddRelease (WebServiceLogin login, DBRelease release)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.BuildBot);
					release.Save (db);
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public GetReleasesResponse GetReleases (WebServiceLogin login)
		{
			GetReleasesResponse response = new GetReleasesResponse ();

			try {
				using (DB db = new DB ()) {
					response.Releases = new List<DBRelease> ();
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = "SELECT * FROM Release ORDER BY version;";
						using (IDataReader reader = cmd.ExecuteReader ()) {
							while (reader.Read ()) {
								response.Releases.Add (new DBRelease (reader));
							}
						}
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse DeleteRelease (WebServiceLogin login, int id)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = "DELETE FROM Release WHERE id = @id;";
						DB.CreateParameter (cmd, "id", id);
						cmd.ExecuteNonQuery ();
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		[WebMethod]
		public WebServiceResponse MarkAsDontBuild (WebServiceLogin login, int lane_id)
		{
			WebServiceResponse response = new WebServiceResponse ();

			try {
				using (DB db = new DB ()) {
					VerifyUserInRole (db, login, Roles.Administrator);
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = "UPDATE RevisionWork SET state = 11 WHERE state = 0 AND lane_id = @lane_id;";
						DB.CreateParameter (cmd, "lane_id", lane_id);
						cmd.ExecuteNonQuery ();
					}
				}
			} catch (Exception ex) {
				response.Exception = new WebServiceException (ex);
			}

			return response;
		}

		#region Adminstration methods

		[WebMethod]
		public void ExecuteScheduler (WebServiceLogin login, bool forcefullupdate)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);
				db.Audit (login, "WebServices.ExecuteScheduler (forcefullupdate: {0})", forcefullupdate);

				MonkeyWrench.Scheduler.Scheduler.ExecuteSchedulerAsync (forcefullupdate);
			}
		}

		[WebMethod]
		public void ExecuteDeletionDirectives (WebServiceLogin login)
		{
			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);

				MonkeyWrench.Database.DeletionDirectives.ExecuteAsync ();
			}
		}

		[WebMethod]
		public GetAdminInfoResponse GetAdminInfo (WebServiceLogin login)
		{
			GetAdminInfoResponse response = new GetAdminInfoResponse ();

			using (DB db = new DB ()) {
				VerifyUserInRole (db, login, Roles.Administrator);

				response.IsSchedulerExecuting = MonkeyWrench.Scheduler.Scheduler.IsExecuting;
				response.IsDeletionDirectivesExecuting = MonkeyWrench.Database.DeletionDirectives.IsExecuting;
			}

			return response;
		}
		#endregion
	}
}

