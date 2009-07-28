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
    [WebServiceBinding (ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem (false)]
    public class WebServices : System.Web.Services.WebService
    {
        public WebServices ()
        {
            Configuration.LoadConfiguration (new string [] { });
        }

        internal void Authenticate (DB db, WebServiceLogin login, WebServiceResponse response)
        {
            Authentication.Authenticate (Context, db, login, response);
        }

        private void VerifyUserInRole (DB db, WebServiceLogin login, string role)
        {
            Authentication.VerifyUserInRole (Context, db, login, role);
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
        public void CreateLanefile (WebServiceLogin login, int lane_id, string filename)
        {
            if (string.IsNullOrEmpty (filename))
                throw new ArgumentException ("filename");

            if (lane_id <= 0)
                throw new ArgumentException ("lane_id");

            using (DB db = new DB ()) {
                VerifyUserInRole (db, login, Roles.Administrator);

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
                using (IDbCommand cmd = db.CreateCommand ()) {
                    cmd.CommandText = "DELETE FROM Lanefiles WHERE lane_id = @lane_id AND lanefile_id = @lanefile_id;";
                    DB.CreateParameter (cmd, "lane_id", lane_id);
                    DB.CreateParameter (cmd, "lanefile_id", lanefile_id);
                    cmd.ExecuteNonQuery ();
                }
            }
        }

        [WebMethod]
        public void EditCommandFilename (WebServiceLogin login, int command_id, string filename)
        {
            using (DB db = new DB ()) {
                VerifyUserInRole (db, login, Roles.Administrator);
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
                DBCommand cmd = DBCommand_Extensions.Create (db, command_id);
                cmd.timeout = timeout;
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
        public GetHostForEditResponse GetHostForEdit (WebServiceLogin login, int? host_id, string host)
        {
            GetHostForEditResponse response = new GetHostForEditResponse ();

            using (DB db = new DB ()) {
                VerifyUserInRole (db, login, Roles.Administrator);

                response.Host = FindHost (db, host_id, host);
                response.Lanes = db.GetAllLanes ();
                if (response.Host != null) {
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

                response.Lane = FindLane (db, lane_id, lane);
                response.Commands = response.Lane.GetCommands (db);
                response.Dependencies = response.Lane.GetDependencies (db);
                response.FileDeletionDirectives = DBFileDeletionDirective_Extensions.GetAll (db);
                response.LaneDeletionDirectives = DBLaneDeletionDirectiveView_Extensions.Find (db, response.Lane);
                response.Files = response.Lane.GetFiles (db);
                response.HostLaneViews = response.Lane.GetHosts (db);
                response.Hosts = db.GetHosts ();
                response.Lanes = db.GetAllLanes ();

                response.ExistingFiles = new List<DBLanefile> ();
                using (IDbCommand cmd = db.CreateCommand ()) {
                    cmd.CommandText = @"
SELECT Lanefile.*
FROM Lanefile
INNER JOIN Lanefiles ON Lanefiles.lanefile_id = Lanefile.id
WHERE Lanefiles.lane_id <> @lane_id 
ORDER BY Lanefiles.lane_id, Lanefile.name ASC";
                    DB.CreateParameter (cmd, "lane_id", response.Lane.id);
                    using (IDataReader reader = cmd.ExecuteReader ()) {
                        while (reader.Read ())
                            response.ExistingFiles.Add (new DBLanefile (reader));
                    }
                }

                response.Variables = DBEnvironmentVariable_Extensions.Find (db, response.Lane.id, null, null);

                return response;
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
                } else {
                    cmd.CommandText = "SELECT * FROM Command WHERE id = @id";
                    DB.CreateParameter (cmd, "id", command_id.Value);
                }

                cmd.CommandText += " AND lane_id = @lane_id";
                DB.CreateParameter (cmd, "lane_id", lane.id);

                using (IDataReader reader = cmd.ExecuteReader ()) {
                    if (reader.Read ())
                        return new DBCommand (reader);
                }
            }

            return null;
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
        public void EditLane (WebServiceLogin login, DBLane lane)
        {
            WebServiceResponse response = new WebServiceResponse ();
            using (DB db = new DB ()) {
                VerifyUserInRole (db, login, Roles.Administrator);
                lane.Save (db);
            }
        }

        [WebMethod]
        public void EditHost (WebServiceLogin login, DBHost host)
        {
            WebServiceResponse response = new WebServiceResponse ();
            using (DB db = new DB ()) {
                VerifyUserInRole (db, login, Roles.Administrator);
                host.Save (db);
            }
        }

        [WebMethod]
        public GetViewLaneDataResponse GetViewLaneData (WebServiceLogin login, int? lane_id, string lane, int? host_id, string host, int? revision_id, string revision)
        {
            GetViewLaneDataResponse response = new GetViewLaneDataResponse ();

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
                    response.WorkFileViews.Add (DBWork_Extensions.GetFiles (db, response.WorkViews [i].id));
                }
            }

            return response;
        }


        [WebMethod]
        public FrontPageResponse GetFrontPageData (WebServiceLogin login, int limit, string lane, int? lane_id)
        {
            FrontPageResponse response = new FrontPageResponse ();
            List<DBLane> Lanes = new List<DBLane> ();
            List<DBHost> Hosts = new List<DBHost> ();
            List<DBHostLane> HostLanes = new List<DBHostLane> ();
            List<DBRevisionWorkView2> RevisionWork;

            limit = Math.Max (limit, 500);

            using (DB db = new DB ()) {
                Authenticate (db, login, response);

                response.Lane = FindLane (db, lane_id, lane);

                using (IDbCommand cmd = db.CreateCommand ()) {
                    cmd.CommandText = DBLane.TableName;
                    cmd.CommandType = CommandType.TableDirect;
                    using (IDataReader reader = cmd.ExecuteReader ()) {
                        while (reader.Read ())
                            Lanes.Add (new DBLane (reader));
                    }
                }

                using (IDbCommand cmd = db.CreateCommand ()) {
                    cmd.CommandText = DBHost.TableName;
                    cmd.CommandType = CommandType.TableDirect;
                    using (IDataReader reader = cmd.ExecuteReader ()) {
                        while (reader.Read ())
                            Hosts.Add (new DBHost (reader));
                    }
                }

                using (IDbCommand cmd = db.CreateCommand ()) {
                    cmd.CommandText = @"
SELECT HostLane.*
FROM HostLane";

                    using (IDataReader reader = cmd.ExecuteReader ()) {
                        while (reader.Read ())
                            HostLanes.Add (new DBHostLane (reader));
                    }
                }

                response.RevisionWorkViews = new List<List<DBRevisionWorkView2>> ();
                response.RevisionWorkHostLaneRelation = new List<int> ();

                foreach (DBHostLane hl in HostLanes) {
                    RevisionWork = new List<DBRevisionWorkView2> ();
                    using (IDbCommand cmd = db.CreateCommand ()) {
                        cmd.CommandText = @"SELECT R.* FROM (" + DBRevisionWorkView2.SQL.Replace (';', ' ') + ") AS R WHERE R.host_id = @host_id AND R.lane_id = @lane_id LIMIT @limit";
                        DB.CreateParameter (cmd, "host_id", hl.host_id);
                        DB.CreateParameter (cmd, "lane_id", hl.lane_id);
                        DB.CreateParameter (cmd, "limit", limit);

                        using (IDataReader reader = cmd.ExecuteReader ()) {
                            while (reader.Read ())
                                RevisionWork.Add (new DBRevisionWorkView2 (reader));
                        }
                    }

                    response.RevisionWorkHostLaneRelation.Add (hl.id);
                    response.RevisionWorkViews.Add (RevisionWork);
                }

                response.Lanes = Lanes;
                response.Hosts = Hosts;
                response.HostLanes = HostLanes;

                return response;
            }
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
                DBRecord_Extensions.Delete (db, host_id, DBHost.TableName);
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
                dbhost.Save (db);
                return dbhost.id;
            }
        }

        [WebMethod]
        public void ClearRevision (WebServiceLogin login, int lane_id, int host_id, int revision_id)
        {
            using (DB db = new DB ()) {
                VerifyUserInRole (db, login, Roles.Administrator);
                db.DeleteFiles (host_id, lane_id, revision_id);
                db.ClearWork (lane_id, revision_id, host_id);
            }
        }

        [WebMethod]
        public void RescheduleRevision (WebServiceLogin login, int lane_id, int host_id, int revision_id)
        {
            using (DB db = new DB ()) {
                VerifyUserInRole (db, login, Roles.Administrator);
                db.DeleteWork (lane_id, revision_id, host_id);
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

            using (DB db = new DB ()) {
                Authenticate (db, login, response);
                response.Lane = FindLane (db, lane_id, lane);
                response.Host = FindHost (db, host_id, host);
                response.Count = DBRevisionWork_Extensions.GetCount (db, response.Lane.id, response.Host.id);
                response.Page = page;
                response.PageSize = page_size;
                response.RevisionWorkViews = DBRevisionWorkView_Extensions.Query (db, response.Lane, response.Host, response.PageSize, response.Page);
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
ORDER BY revision DESC LIMIT 250;
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
                    response.WorkFileViews.Add (DBWork_Extensions.GetFiles (db, response.WorkViews [i].id));
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

            using (DB db = new DB ()) {
                VerifyUserInRole (db, login, Roles.Administrator);

                response.Users = DBPerson_Extensions.GetAll (db);
            }

            return response;
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
                VerifyUserInRole (db, login, Roles.BuildBot);

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
                VerifyUserInRole (db, login, Roles.BuildBot);
                work.Reload (db);
                return work.State;
            }
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
                VerifyUserInRole (db, login, Roles.BuildBot);
                Console.WriteLine ("ReportBuildState, state: {0}, start time: {1}, end time: {2}", work.State, work.starttime, work.endtime);
                if (work.starttime > new DateTime (2000, 1, 1) && work.endtime < work.starttime) {
                    // the issue here is that the server interprets the datetime as local time, while it's always as utc.
                    try {
                        using (IDbCommand cmd = db.CreateCommand ()) {
                            cmd.CommandText = "SELECT starttime FROM Work WHERE id = " + work.id;
                            work.starttime = (DateTime) cmd.ExecuteScalar ();
                        }
                    } catch (Exception ex) {
                        Console.WriteLine ("ReportBuildState: Exception while fixing timezone data: {0}", ex.Message);
                    }
                }
                work.Save (db);
                work.Reload (db);
                Console.WriteLine ("ReportBuildState, state: {0}, start time: {1}, end time: {2} SAVED", work.State, work.starttime, work.endtime);

                response.Work = work;
                
                DBRevisionWork rw = DBRevisionWork_Extensions.Create (db, work.revisionwork_id);
                rw.UpdateState (db);
                response.RevisionWorkCompleted = rw.completed;

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
                    VerifyUserInRole (db, login, Roles.BuildBot);

                    response.Host = FindHost (db, null, host);

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
                        cmd.CommandText = "SELECT * FROM HostLane WHERE enabled = TRUE AND (";
                        for (int i = 0; i < hosts.Count; i++) {
                            if (i > 0)
                                cmd.CommandText += " OR ";
                            cmd.CommandText += " host_id = " + hosts [i].id;
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
                        
                        if (revisionwork.workhost_id != response.Host.id)
                            continue; // couldn't lock this revisionwork.

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
                                dep_revwork = DBRevisionWork_Extensions.Find (db, dependent_lane, dependent_host, dependent_lane.FindRevision (db, revision.revision));

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
                            cmd.CommandText = @"
SELECT * 
FROM EnvironmentVariable 
WHERE 
    (host_id = @host_id OR host_id = @masterhost_id OR host_id IS NULL) AND (lane_id = @lane_id OR lane_id IS NULL)
ORDER BY id;
;";
                            DB.CreateParameter (cmd, "lane_id", lane.id);
                            DB.CreateParameter (cmd, "host_id", revisionwork.workhost_id);
                            DB.CreateParameter (cmd, "masterhost_id", revisionwork.host_id);
                            using (IDataReader reader = cmd.ExecuteReader ()) {
                                while (reader.Read ()) {
                                    if (environment_variables == null)
                                        environment_variables = new List<DBEnvironmentVariable> ();
                                    environment_variables.Add (new DBEnvironmentVariable (reader));
                                }
                            }
                        }
                        
                        foreach (DBWorkView2 work in pending_work) {
                            BuildInfoEntry entry = new BuildInfoEntry ();
                            entry.Lane = lane;
                            entry.HostLane = hl;
                            entry.Revision = revision;
                            entry.Command = DBCommand_Extensions.Create (db, work.command_id);
                            entry.FilesToDownload = files_to_download;
                            entry.DependentLaneOfFiles = dependent_lanes;
                            entry.Work = DBWork_Extensions.Create (db, work.id);
                            entry.LaneFiles = lane.GetFiles (db);
                            entry.EnvironmentVariables = environment_variables;
                            
                            // TODO: put work with the same sequence number into one list of entries.
                            List<BuildInfoEntry> entries = new List<BuildInfoEntry> ();
                            entries.Add (entry);
                            response.Work.Add (entries);
                        }
                    }
                }

                return response;
            } catch (Exception ex) {
                Console.WriteLine ("Exception in GetBuildInfo: {0}", ex);
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
WHERE WorkFile.filename = @filename AND Revision.lane_id = @lane_id 
";
                    if (successful)
                        cmd.CommandText += " AND RevisionWork.result = " + ((int) DBState.Success).ToString () + " ";
                    if (completed)
                        cmd.CommandText += " AND RevisionWork.completed = true ";

                    cmd.CommandText += " ORDER BY Revision.date DESC LIMIT 1;";
                    Console.WriteLine (cmd.CommandText);

                    DB.CreateParameter (cmd, "lane_id", l.id);
                    DB.CreateParameter (cmd, "filename", filename);
                    
                    using (IDataReader reader = cmd.ExecuteReader ()) {
                        if (!reader.Read ())
                            return null;

                        return reader.GetInt32 (0);
                    }
                }
            }
        }
         #region Adminstration methods

         [WebMethod]
         public void ExecuteScheduler (WebServiceLogin login, bool forcefullupdate)
         {
             using (DB db = new DB ()) {
                 VerifyUserInRole (db, login, Roles.Administrator);

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
 		#endregion
    }
}
