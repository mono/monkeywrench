
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

	using MonkeyWrench.DataClasses;
	using MonkeyWrench.DataClasses.Logic;
	using MonkeyWrench.Web.WebServices;

	public partial class Json : System.Web.UI.Page
	{
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
					Response.Write (GetLaneInfo (login));
					break;
				case "botinfo":
					Response.Write (GetBotInfo (login, true));
					break;
				default:
					Response.Write (GetBotInfo (login, false));
					break;
			}
		}

		private string GetBotInfo (WebServiceLogin login, bool showHostHistory) {
			var hoststatusresponse = Utils.WebService.GetHostStatus (login);
			var node_information = new Dictionary<string, object> {
				{ "inactiveNodes", GetInactiveHosts (login, hoststatusresponse) },
				{ "activeNodes",   GetActiveHosts (login, hoststatusresponse) },
				{ "downNodes",     GetDownHosts (login, hoststatusresponse) }
				// { "pendingJobs", "asdf" }
			};
			if (showHostHistory)
				node_information.Add ("hostHistory", GetHostHistory (login, limit, offset));
			return JsonConvert.SerializeObject (node_information, Formatting.Indented);
		}

		private Dictionary<string, IEnumerable<HostHistoryEntry>> GetHostHistory (WebServiceLogin web_service_login, int limit, int offset) {
			var hosts = Utils.WebService.GetHosts (login).Hosts.OrderBy(h => h.host);
			var hostHistoryResponses = hosts.Select (host => 
				Utils.WebService.GetWorkHostHistory (login, host.id, "", limit, offset));

			var hostHistories = hostHistoryResponses.ToDictionary (
				hr => hr.Host.host,
				hr => Enumerable.Range(0, hr.RevisionWorks.Count)
					.Select(i =>  new HostHistoryEntry (hr, i))
			);
			return hostHistories;
		}

		private string GetLastJob (WebServiceLogin login, DBLane lane)
		{
//			return Utils.WebService.GetRevisions (login, null, lane.lane, 1, 0).Revisions
//				.Select(rev => rev.date.ToString ("yyyy/MM/dd HH:mm:ss UTC")).FirstOrDefault();

			var revisions = Utils.WebService.GetRevisions (login, null, lane.lane, 1, 0).Revisions;

			return revisions.Any () ? revisions.First().date.ToString ("u") : "";
		}

		private string GetLaneInfo (WebServiceLogin login) {
			var lanesResponse = Utils.WebService.GetLanes (login);

			var lanes = lanesResponse.Lanes.ToDictionary (
				l => l.lane, 
				l => new { 
					branch     = BranchFromRevision (l.max_revision),
					repository = l.repository,
					id         = l.id
				});

			var count = lanes.Where ((kv) => !String.IsNullOrEmpty (kv.Value.repository)).Count();

			var results = new Dictionary<string, object> {
				{ "count", count },
				{ "lanes", lanes }			
			};

			return JsonConvert.SerializeObject (results, Formatting.Indented);
		}

		string BranchFromRevision (string revision) {
			return String.IsNullOrEmpty (revision) ? "remotes/origin/master" : revision;
		}

		private IEnumerable<string> GetActiveHosts (WebServiceLogin login, GetHostStatusResponse hoststatusresponse) {
			return (hoststatusresponse.HostStatus)
				.Where (IsHostActive)
				.Select (status => status.host)
				.OrderBy(h => h);
		}

		private IEnumerable<string> GetInactiveHosts (WebServiceLogin login, GetHostStatusResponse hoststatusresponse) {
			return hoststatusresponse.HostStatus
				.Where (IsHostInactive)
				.Select (status => status.host)
				.OrderBy(h => h);
		}

		private IEnumerable<string> GetDownHosts (WebServiceLogin login, GetHostStatusResponse hoststatusresponse) {
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
	}
}
