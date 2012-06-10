using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Mono.Options;
using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public class WrenchCmdClient
{
	WebServices ws;
	WebServiceLogin login;

	DBLane FindLaneByName (string lane_name) {
		var lane = ws.FindLane (login, null, lane_name).lane;
		if (lane == null)
			Console.WriteLine ("Lane '" + lane_name + "' not found.");
		return lane;
	}

	DBCommand FindCmdByName (DBLane lane, string cmd_name) {
		var res = ws.GetLaneForEdit (login, lane.id, null);
		var cmd = (from c in res.Commands where c.command == cmd_name select c).FirstOrDefault ();
		if (cmd == null)
			Console.WriteLine ("Step '" + cmd_name + "' not found in lane '" + lane.lane + "'");
		return cmd;
	}

	bool CheckCmdExists (DBLane lane, string cmd_name) {
		var res = ws.GetLaneForEdit (login, lane.id, null);
		var cmd = (from c in res.Commands where c.command == cmd_name select c).FirstOrDefault ();
		if (cmd != null)
			Console.WriteLine ("Step '" + cmd_name + "' already exists in lane '" + lane.lane + "'");
		return cmd == null;
	}

	void PrintCmd (DBCommand cmd) {
		Console.WriteLine ("cmd=[" + cmd.command + "], exec=[" + cmd.filename + " " + cmd.arguments + "], seq=" + cmd.sequence + ", timeout=" + cmd.timeout + ", alwaysexecute=" + cmd.alwaysexecute + ", nonfatal=" + cmd.nonfatal + ", upload-files=[" + cmd.upload_files + "]");
	}

	void PrintLaneTree (Dictionary<DBLane, List<DBLane>> tree, DBLane lane, int level) {
		for (int i = 0; i < level; ++i)
			Console.Write (' ');
		Console.WriteLine (lane.lane);
		foreach (var l in tree [lane])
			PrintLaneTree (tree, l, level + 1);
	}

	void PrintUsage () {
		Console.WriteLine ("Usage: <command> <command args>");
		Console.WriteLine ("Available commands:");
		Console.WriteLine ("help");
		Console.WriteLine ("\tPrint help.");
		Console.WriteLine ("lanes");
		Console.WriteLine ("\tPrint the names of all the lanes.");
		Console.WriteLine ("lane-tree");
		Console.WriteLine ("\tPrint the names of all the lanes in a tree structure.");
		Console.WriteLine ("lane-config <lane>");
		Console.WriteLine ("\tPrint the configuration of <lane>.");
		Console.WriteLine ("show-step <lane> <step name>");
		Console.WriteLine ("\tPrint the configuration of <step> in <lane>.");
		Console.WriteLine ("add-step <lane> <step name>");
		Console.WriteLine ("\tAdd a new step named <step> to <lane>.");
		Console.WriteLine ("edit-step <lane> <step name> [--seq=<val>] [--timeout=<val>] [--nonfatal=true|false] [--alwaysexecute=true|false] [--filename=<val>] [--arguments=<val>] [--upload-files=<val>]");
		Console.WriteLine ("\tEdit the configuration of <step> in <lane> using the supplied options.");
		Console.WriteLine ("add-lane-file <lane> <lane file name>");
		Console.WriteLine ("\tAdd a new lane file named <lane file name> with defaults contents to <lane>.");
		Console.WriteLine ("edit-lane-file <lane> <lane file name> <filename>");
		Console.WriteLine ("\tSet the contents of <file name> as the contents of <lane file name> in <lane>.");
		Console.WriteLine ("rev <lane> <host> <revision>");
		Console.WriteLine ("\tPrint the result of building <revision> on <lane>/<host>.");
		Console.WriteLine ("lane <lane> <host> [--limit=<val>]");
		Console.WriteLine ("\tPrint the results of the last <limit> builds on <lane>/<host>. <limit> defaults to 10.");
	}

	public static int Main (String[] args) {
		return new WrenchCmdClient ().Run (args);
	}

	int Run (String[] args) {
		if (!Configuration.LoadConfiguration (new string[0])) {
			Console.WriteLine ("Couldn't load configuration.");
			return 1;
		}
		ws = WebServices.Create ();
		ws.CreateLogin (Configuration.Host, Configuration.WebServicePassword);
		login = ws.WebServiceLogin;
		ws.Login (login);

		string command = args.Length > 0 ? args [0] : "";
		if (command == "lane-config") {
			// Requires admin rights (because it uses GetLaneForEdit)
			if (args.Length < 2) {
				Console.WriteLine ("Usage: lane-config <lane name>");
				return 1;
			}
			string lane_name = args [1];
			var lane = FindLaneByName (lane_name);
			if (lane == null)
				return 1;
			Console.WriteLine ("Lane: " + lane.lane);
			if (lane.parent_lane_id != null) {
				var parent_lane = ws.FindLane (login, lane.parent_lane_id, null).lane;
				if (parent_lane != null)
					Console.WriteLine ("Parent: " + parent_lane.lane);
			}
			Console.WriteLine ("Source control: " + lane.source_control);
			Console.WriteLine ("Repository: " + lane.repository);
			Console.WriteLine ("Mini/Max revision: " + lane.min_revision + "/" + (lane.max_revision != "" ? lane.max_revision : "<none>"));

			var res = ws.GetLaneForEdit (login, lane.id, null);
			foreach (var c in res.Commands) {
				Console.WriteLine ("\t " + c.sequence + " " + c.command + " (" + c.filename + " " + c.arguments + ")");
			}
			Console.WriteLine ("Files:");
			foreach (var f in res.Files) {
				Console.WriteLine ("\t" + f.name);
			}
			Console.WriteLine ("Hosts:");
			foreach (var h in res.HostLaneViews) {
				Console.WriteLine ("\t" + h.host);
			}
		} else if (command == "show-step") {
			// Requires admin rights
			if (args.Length < 3) {
				Console.WriteLine ("Usage: show-step <lane> <step name>");
				return 1;
			}
			string lane_name = args [1];
			string cmd_name = args [2];
			var lane = FindLaneByName (lane_name);
			if (lane == null)
				return 1;
			var cmd = FindCmdByName (lane, cmd_name);
			if (cmd == null)
				return 1;
			PrintCmd (cmd);
		} else if (command == "add-step") {
			if (args.Length < 3) {
				Console.WriteLine ("Usage: add-step <lane> <step name>");
				return 1;
			}
			string lane_name = args [1];
			string cmd_name = args [2];
			var lane = FindLaneByName (lane_name);
			if (lane == null)
				return 1;
			if (!CheckCmdExists (lane, cmd_name))
				return 1;
			bool always = true;
			bool nonfatal = true;
			int timeout = 5;
			// FIXME: AddCommand () ignores this
			int seq = 0;
			ws.AddCommand (login, lane.id, cmd_name, always, nonfatal, timeout, seq);
		} else if (command == "edit-step") {
			if (args.Length < 3) {
				Console.WriteLine ("Usage: edit-step <lane> <step name> [<edit options>]");
				return 1;
			}
			string lane_name = args [1];
			string cmd_name = args [2];
			var lane = FindLaneByName (lane_name);
			if (lane == null)
				return 1;
			var cmd = FindCmdByName (lane, cmd_name);
			if (cmd == null)
				return 1;

			int? seq = null;
			int? timeout = null;
			bool nonfatal = cmd.nonfatal;
			bool alwaysexecute = cmd.alwaysexecute;
			string filename = null;
			string arguments = null;
			string upload_files = null;
			OptionSet p = new OptionSet ()
				.Add ("seq=", v => seq = Int32.Parse (v))
				.Add ("timeout=", v => timeout = Int32.Parse (v))
				.Add ("nonfatal=", v => nonfatal = Boolean.Parse (v))
				.Add ("alwaysexecute=", v => alwaysexecute = Boolean.Parse (v))
				.Add ("filename=", v => filename = v)
				.Add ("arguments=", v => arguments = v)
				.Add ("upload-files=", v => upload_files = v);
			var new_args = p.Parse (args.Skip (3).ToArray ());
			if (new_args.Count > 0) {
				Console.WriteLine ("Unknown arguments: " + String.Join (" ", new_args.ToArray ()));
				return 1;
			}

			PrintCmd (cmd);

			if (seq != null)
				ws.EditCommandSequence (login, cmd.id, (int)seq);
			if (filename != null)
				ws.EditCommandFilename (login, cmd.id, filename);
			if (arguments != null)
				ws.EditCommandArguments (login, cmd.id, arguments);
			if (upload_files != null)
				ws.EditCommandUploadFiles (login, cmd.id, upload_files);
			if (timeout != null)
				ws.EditCommandTimeout (login, cmd.id, (int)timeout);
			if (alwaysexecute != cmd.alwaysexecute)
				ws.SwitchCommandAlwaysExecute (login, cmd.id);
			if (nonfatal != cmd.nonfatal)
				ws.SwitchCommandNonFatal (login, cmd.id);

			cmd = FindCmdByName (lane, cmd_name);
			if (cmd == null)
				return 1;
			Console.WriteLine ("=>");
			PrintCmd (cmd);
		} else if (command == "add-lane-file") {
			if (args.Length < 3) {
				Console.WriteLine ("Usage: add-lane-file <lane> <lane file name>");
				return 1;
			}
			string lane_name = args [1];
			string lane_file_name = args [2];
			var lane = FindLaneByName (lane_name);
			if (lane == null)
				return 1;
			var res = ws.GetLaneForEdit (login, lane.id, null);
			var file = (from f in res.Files where f.name == lane_file_name select f).FirstOrDefault ();
			if (file != null) {
				Console.WriteLine ("A file named '" + lane_file_name + "' already exists in lane '" + lane.lane + "'.");
				return 1;
			}
			ws.CreateLanefile (login, lane.id, lane_file_name);
		} else if (command == "edit-lane-file") {
			if (args.Length < 4) {
				Console.WriteLine ("Usage: edit-lane-file <lane> <lane file name> <filename>");
				return 1;
			}
			string lane_name = args [1];
			string lane_file_name = args [2];
			string filename = args [3];
			var lane = FindLaneByName (lane_name);
			if (lane == null)
				return 1;

			var res = ws.GetLaneForEdit (login, lane.id, null);
			var file = (from f in res.Files where f.name == lane_file_name select f).FirstOrDefault ();
			if (file == null) {
				Console.WriteLine ("No file named '" + lane_file_name + "' in lane '" + lane.lane + "'.");
				return 1;
			}

			file.contents = File.ReadAllText (filename);
			ws.EditLaneFile (login, file);
		} else if (command == "lanes") {
			var res = ws.GetLanes (login);
			foreach (var l in res.Lanes)
				Console.WriteLine (l.lane);
		} else if (command == "lane-tree") {
			var res = ws.GetLanes (login);
			var tree = new Dictionary<DBLane, List<DBLane>> ();
			foreach (var l in res.Lanes) {
				tree [l] = new List<DBLane> ();
			}
			foreach (var l in res.Lanes) {
				if (l.parent_lane_id != null) {
					var parent_lane = (from l2 in res.Lanes where l2.id == l.parent_lane_id select l2).First ();
					tree [parent_lane].Add (l);
				}
			}
			foreach (var l in tree.Keys) {
				if (l.parent_lane_id == null)
					PrintLaneTree (tree, l, 0);
			}
		} else if (command == "rev") {
			if (args.Length < 4) {
				Console.WriteLine ("Usage: rev <lane> <host> <revision>");
				return 1;
			}
			string lane_name = args [1];
			string host_name = args [2];
			string rev_name = args [3];

			var lane = ws.FindLane (login, null, lane_name).lane;
			if (lane == null) {
				Console.WriteLine ("Lane '" + lane_name + "' not found.");
				return 1;
			}
			var host = ws.FindHost (login, null, host_name).Host;
			var res = ws.FindRevisionForLane (login, null, rev_name, lane.id, null);
			var rev = res.Revision;
			if (res.Revision == null) {
				Console.WriteLine ("Revision '" + rev_name + "' not found.");
				return 1;
			}
			var res2 = ws.GetViewLaneData (login, lane.id, null, host.id, null, rev.id, null);
			Console.WriteLine ("" + rev.revision + " " + rev.author + " " + (DBState)res2.RevisionWork.state);
			int vindex = 0;
			foreach (var view in res2.WorkViews) {
				Console.WriteLine ("\t " + view.command + " " + (DBState)view.state);
				foreach (var fview in res2.WorkFileViews [vindex]) {
					if (fview.work_id == view.id)
						Console.WriteLine ("\t\t" + fview.filename);
				}
				vindex ++;
			}
		} else if (command == "lane") {
			if (args.Length < 3) {
				Console.WriteLine ("Usage: lane <lane> <host>");
				return 1;
			}				

			string lane_name = args [1];
			string host_name = args [2];

			int limit = 10;
			OptionSet p = new OptionSet ()
				.Add ("limit=", v => limit = Int32.Parse (v));
			p.Parse (args.Skip (3).ToArray ());

			var lane = ws.FindLane (login, null, lane_name).lane;
			if (lane == null) {
				Console.WriteLine ("Lane '" + lane_name + "' not found.");
				return 1;
			}
			//var cmds = ws.GetCommands (login, lane.id);
			//var cmd = (from c in cmds.Commands where c.command == "package" select c).First ();
			var hosts = ws.GetHosts (login);
			var host = (from h in hosts.Hosts where h.host == host_name select h).FirstOrDefault ();
			if (host == null) {
				Console.WriteLine ("Host '" + host_name + "' not found.");
				return 1;
			}
			var revs = ws.GetRevisions (login, null, lane.lane, limit, 0);
			foreach (var rev in revs.Revisions) {
				var watch = new Stopwatch ();
				watch.Start ();
				var res3 = ws.GetViewLaneData (login, lane.id, null, host.id, null, rev.id, null);
				watch.Stop ();
				if (res3.RevisionWork == null) {
					Console.WriteLine ("Host/lane pair not found.");
					return 1;
				}
				DBState state = (DBState)res3.RevisionWork.state;
				string extra = "";
				if (state == DBState.Executing || state == DBState.Issues) {
					int nsteps = 0;
					int ndone = 0;
					string step = "";
					foreach (var view in res3.WorkViews) {
						nsteps ++;
						DBState viewstate = (DBState)view.state;
						if (viewstate != DBState.NotDone && viewstate != DBState.Executing)
							ndone ++;
						if (viewstate == DBState.Executing)
							step = view.command;
					}
					extra = " (" + nsteps + "/" + ndone + (step != "" ? " " + step : "") + ")";
				}
				if (state == DBState.Issues || state == DBState.Failed) {
					string failed = "";
					foreach (var view in res3.WorkViews) {
						if ((DBState)view.state == DBState.Failed) {
							if (failed != "")
								failed += " ";
							failed += view.command;
						}
					}
					if (failed != "")
						extra += " (" + failed + ")";
				}
				Console.WriteLine ("" + rev.revision + " " + rev.author + " " + state + extra);
			}
		} else if (command == "help") {
			PrintUsage ();
		} else {
			PrintUsage ();
			return 1;
		}
		ws.Logout (login);
		return 0;
	}
}
