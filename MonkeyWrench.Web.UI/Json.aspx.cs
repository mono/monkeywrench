
namespace MonkeyWrench.Web.UI
{
	using System;
	using System.Web;
	using System.Web.UI;
	using System.Web.UI.WebControls;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using log4net;

	using MonkeyWrench.DataClasses;
	using MonkeyWrench.DataClasses.Logic;
	using MonkeyWrench.Web.WebServices;

	public partial class Json : System.Web.UI.Page
	{
		private static readonly ILog log = LogManager.GetLogger (typeof(Json));

		private struct HostHistoryEntry
		{
			public string date;
			public int duration;
			public string revision;
			public string lane;
			public string state;
			public bool completed;

			public HostHistoryEntry(GetWorkHostHistoryResponse hr, int i)
			{
				this.date = hr.StartTime[i].ToString("u");
				this.duration = hr.Durations[i];
				this.revision = hr.Revisions[i];
				this.completed = hr.RevisionWorks[i].completed;
				this.state = hr.RevisionWorks[i].State.ToString();
				this.lane = hr.Lanes[i];
			}
		}
		private string requestType;
		private int limit;
		private int offset;

		private new Master Master
		{
			get { return base.Master as Master; }
		}

		private WebServiceLogin login;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			login = Authentication.CreateLogin (Request);

			requestType = Request.QueryString ["type"];
			limit = Utils.TryParseInt32 (Request.QueryString ["limit"]) ?? 50;
			offset = Utils.TryParseInt32 (Request.QueryString ["offset"]) ?? 0;

			Response.AppendHeader("Access-Control-Allow-Origin", "*");
			switch (requestType) {
				case "laneinfo":
					Response.Write (GetLaneInfo ());
					break;
				case "taginfo":
					Response.Write (GetTagInfo ());
					break;
				case "botinfo":
					GetBotInfo ();
					break;
				case "botstatus":
					Response.Write (GetBotStatusTimes ());
					break;
				default:
					GetBotStatus ();
					break;
			}
		}

		private string GetTagInfo() {
			using (var db = new DB ()) {

				string[] tags = null;
				var results = new List<object>();

				if (!string.IsNullOrEmpty (Request ["tags"]))
					tags = Request ["tags"].Split (',');

				MonkeyWrench.WebServices.Authentication.Authenticate (Context, db, login, null, true);
				FrontPageResponse data = Utils.LocalWebService.GetFrontPageDataWithTags (login, limit, 0, null, null, 30, tags);

				var rows = new List<StringBuilder> ();

				for (int i = 0; i < data.SelectedLanes.Count; i++) {
					var lane = data.SelectedLanes [i];
					var hostlanes = data.HostLanes.FindAll ((hl) => hl.lane_id == lane.id);

					foreach (var hostlane in hostlanes) {
						var work_views = FindRevisionWorkViews (data, hostlane.id);

						for (int r = 0; r < work_views.Count; r++) {
							results.Add (new Dictionary<string, object> {
								{ "id", work_views[r].id },
								{ "author", work_views[r].author },
								{ "host_id", work_views[r].host_id },
								{ "host", data.Hosts.First( h => h.id == work_views[r].host_id).host },
								{ "lane", lane.lane },
								{ "revision", work_views[r].revision },
								{ "completed", work_views[r].completed },
								{ "status", work_views[r].state.ToString().ToLowerInvariant()},
							});
						}
					}
				}

				return JsonConvert.SerializeObject (results, Formatting.Indented);
			}
		}

		List<DBRevisionWorkView2> FindRevisionWorkViews (FrontPageResponse data, int hostlane_id)
		{
			for (int k = 0; k < data.RevisionWorkHostLaneRelation.Count; k++) {
				if (data.RevisionWorkHostLaneRelation [k] == hostlane_id)
					return data.RevisionWorkViews [k];
			}

			return null;
		}

		private string GetBotStatusTimes() {
			using (var db = new DB ()) {
				MonkeyWrench.WebServices.Authentication.Authenticate (Context, db, login, null, true);

				limit = Utils.TryParseInt32 (Request.QueryString ["limit"]) ?? 1; // We only want the last job by default or more if asked.

				// Get hosts and statuses
				GetHostsResponse response = Utils.LocalWebService.GetHosts (login);
				GetBuildBotStatusResponse statuses = Utils.LocalWebService.GetBuildBotStatus (login);

				var results = new List<object>();

				foreach (DBHost host in response.Hosts) {
					DBBuildBotStatus status = null;

					foreach (var tmp in statuses.Status) {
						if (tmp.host_id == host.id) {
							status = tmp;
							break;
						}
					}

					results.Add (new Dictionary<string, object> {
						{ "id", host.id },
						{ "host", host.host },
						{ "enabled", host.enabled },
						{ "description", host.description },
						{ "architecture", host.architecture },
						{ "last_seen", status != null ? status.report_date.ToString ("yyyy/MM/dd HH:mm:ss UTC") : "" },
						{ "last_job", this.GetHostHistory (db, host.id)},
					});
				}

				return JsonConvert.SerializeObject (results, Formatting.Indented);
			}
		}

		private void GetBotStatus() {
			using (var db = new DB ()) {
				MonkeyWrench.WebServices.Authentication.Authenticate (Context, db, login, null, true);
				Response.Write (this.GetHostStatuses (db).ToString ());
			}
		}

		private void GetBotInfo () {
			
			using (var db = new DB ()) {
				MonkeyWrench.WebServices.Authentication.Authenticate (Context, db, login, null, true);

				var result = this.GetHostStatuses(db);

				var history = new JObject ();
				var hosts = new Dictionary<int, string> ();

				using (var cmd = db.CreateCommand (@"
					SELECT id, host FROM Host;
				"))
				using (var reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						int id = reader.GetInt32 (0);
						string hostName = reader.GetString (1);
						hosts [id] = hostName;
					}
				}

				foreach (var entry in hosts)
					history [entry.Value] = this.GetHostHistory (db, entry.Key);
				
				result ["hostHistory"] = history;

				Response.Write (result.ToString());
			}
		}

		private JObject GetHostStatuses(DB db) {
			var result = new JObject ();

			var activeHosts = new JArray ();
			var inactiveHosts = new JArray ();
			var downHosts = new JArray ();

			using (var cmd = db.CreateCommand (@"
				SELECT * FROM HostStatusView;
			"))
			using (var reader = cmd.ExecuteReader ()) {
				while (reader.Read ()) {
					var status = new DBHostStatusView (reader);
					if (IsHostActive (status))
						activeHosts.Add (status.host);
					else if (IsHostInactive (status))
						inactiveHosts.Add (status.host);
					else if (IsHostDead (status))
						downHosts.Add (status.host);
					else
						log.ErrorFormat ("Host {0} ({1}) isn't active, inactive, or dead.", status.host, status.id);
				}
			}

			result ["inactiveNodes"] = inactiveHosts;
			result ["activeNodes"] = activeHosts;
			result ["downNodes"] = downHosts;
			return result;
		}

		private JArray GetHostHistory (DB db, int hostid) {
			using (var cmd = db.CreateCommand ()) {
				cmd.CommandText = @"
					SELECT
						RevisionWork.id AS revisionwork_id,
						RevisionWork.completed,
						RevisionWork.state,
						
						RevisionWork.createdtime,
						RevisionWork.assignedtime,
						NULLIF(RevisionWork.startedtime, '2000-01-01 00:00:00+00'::timestamp) AS startedtime,
						RevisionWork.endtime,
						
						Host.host,
						Host.id AS host_id,
						Lane.lane,
						Lane.id AS lane_id,
						Revision.revision,
						Revision.id AS revision_id,
						WorkHost.host AS workhost,
						WorkHost.id AS workhost_id

					FROM RevisionWork
					INNER JOIN Revision ON RevisionWork.revision_id = Revision.id
					INNER JOIN Lane ON RevisionWork.lane_id = Lane.id
					INNER JOIN Host ON RevisionWork.host_id = Host.id
					INNER JOIN Host AS WorkHost ON RevisionWork.workhost_id = WorkHost.id

					WHERE RevisionWork.workhost_id = @host AND RevisionWork.createdtime IS NOT NULL
					ORDER BY RevisionWork.createdtime DESC
					LIMIT @limit
					OFFSET @offset
				";
				DB.CreateParameter (cmd, "host", hostid);
				if (limit == 0)
					DB.CreateParameter (cmd, "limit", null);
				else
					DB.CreateParameter (cmd, "limit", limit);
				DB.CreateParameter (cmd, "offset", offset);

				var history = new JArray ();

				using (var reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						var obj = new JObject ();
						obj ["revisionwork_id"] = reader.GetInt32 (0);
						obj ["completed"] = reader.GetBoolean (1);
						obj ["state"] = ((DBState)reader.GetInt32 (2)).ToString ();
						obj ["createdtime"] = dateTimeToMilliseconds (reader.IsDBNull (3) ? null : (DateTime?)reader.GetDateTime (3));
						obj ["assignedtime"] = dateTimeToMilliseconds (reader.IsDBNull (4) ? null : (DateTime?)reader.GetDateTime (4));
						obj ["startedtime"] = dateTimeToMilliseconds (reader.IsDBNull (5) ? null : (DateTime?)reader.GetDateTime (5));
						obj ["endtime"] = dateTimeToMilliseconds (reader.IsDBNull (6) ? null : (DateTime?)reader.GetDateTime (6));

						obj ["host"] = reader.GetString (7);
						obj ["host_id"] = reader.GetInt32 (8);
						obj ["lane"] = reader.GetString (9);
						obj ["lane_id"] = reader.GetInt32 (10);
						obj ["revision"] = reader.GetString (11);
						obj ["revision_id"] = reader.GetInt32 (12);
						obj ["workhost"] = reader.GetString (13);
						obj ["workhost_id"] = reader.GetInt32 (14);

						// Backwards compatibility fields
						obj ["date"] = obj ["startedtime"];
						if (!reader.IsDBNull (5) && !reader.IsDBNull (6))
							obj ["duration"] = (reader.GetDateTime (6) - reader.GetDateTime (5)).TotalSeconds;

						history.Add (obj);
					}
				}
				return history;
			}
		}

		private string GetLaneInfo () {
			using (var db = new DB ()) {
				var lanesResponse = Utils.LocalWebService.GetLanes (login);

				var lanes = lanesResponse.Lanes.Select(l => 
					new {
						lane       = l.lane,
						branch     = BranchFromRevision (l.max_revision),
						repository = l.repository,
						id         = l.id,
						tags       = Utils.LocalWebService.GetTagsForLane(login,l.id).Select(tag => tag.tag)
					});

				var count = lanes.Count (l => !String.IsNullOrEmpty (l.repository));

				var results = new Dictionary<string, object> {
					{ "count", count },
					{ "lanes", lanes }
				};

				return JsonConvert.SerializeObject (results, Formatting.Indented);
			}
		}

		string BranchFromRevision (string revision) {
			return String.IsNullOrEmpty (revision) ? "remotes/origin/master" : revision;
		}

		private IEnumerable<string> GetActiveHosts (GetHostStatusResponse hoststatusresponse) {
			return (hoststatusresponse.HostStatus)
				.Where (IsHostActive)
				.Select (status => status.host)
				.OrderBy(h => h);
		}

		private IEnumerable<string> GetInactiveHosts (GetHostStatusResponse hoststatusresponse) {
			return hoststatusresponse.HostStatus
				.Where (IsHostInactive)
				.Select (status => status.host)
				.OrderBy(h => h);
		}

		private IEnumerable<string> GetDownHosts (GetHostStatusResponse hoststatusresponse) {
			return hoststatusresponse.HostStatus
				.Where (IsHostDead)
				.Select (status => status.host)
				.OrderBy(h => h);
		}

		private bool IsHostActive (DBHostStatusView host) {
			var has_lanes = !String.IsNullOrEmpty (host.lane);
			return has_lanes && !IsHostDead (host);
		}

		private bool IsHostInactive (DBHostStatusView host) {
			var has_no_lanes = String.IsNullOrEmpty (host.lane);
			return has_no_lanes && !IsHostDead (host);
		}

		private bool IsHostDead (DBHostStatusView status) {
			var silence = DateTime.Now - status.report_date;
			return silence.TotalHours >= 3;
		}

		private static ulong? dateTimeToMilliseconds(DateTime? t) {
			return t == null ? null : (ulong?)((t.Value - new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
		}
	}
}
