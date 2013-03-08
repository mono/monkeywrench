using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;

using ServiceStack;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace MonkeyWrench.Web.ServiceStack
{
	public class LatestBuilds {}

	public class LatestBuildsResponse
	{
		public ResponseStatus ResponseStatus { get; set; }
		public List<Build> LatestBuilds { get; set; }
	}
	
	[Authenticate]
	[RequiredRole (RoleNames.Admin)]
	public class LatestBuildsService : Service
	{
		public object Any (LatestBuilds request)
		{
			var result = new List<KeyValuePair<DBHost, DBRevisionWorkView2>> ();
			List<DBLane> lanes = null;
			var hostLanes = new List<DBHostLane> ();

			using (DB db = new DB ()) {
				lanes = db.GetAllLanes ();

				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"
SELECT HostLane.*
FROM HostLane
WHERE hidden = false";
					
					using (IDataReader reader = cmd.ExecuteReader ())
						while (reader.Read ())
							hostLanes.Add (new DBHostLane (reader));
				}

				foreach (DBHostLane hl in hostLanes) {
					DBRevisionWorkView2 revisionWork = null;
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = @"SELECT R.* FROM (" + DBRevisionWorkView2.SQL.Replace (';', ' ') + ") AS R WHERE R.host_id = @host_id AND R.lane_id = @lane_id LIMIT @limit";
						DB.CreateParameter (cmd, "host_id", hl.host_id);
						DB.CreateParameter (cmd, "lane_id", hl.lane_id);
						DB.CreateParameter (cmd, "limit", 1);
						
						using (IDataReader reader = cmd.ExecuteReader ())
							while (reader.Read ())
								revisionWork = new DBRevisionWorkView2 (reader);
					}

					result.Add (new KeyValuePair<DBHost, DBRevisionWorkView2> (Utils.FindHost (db, revisionWork.host_id), revisionWork));
				}
			}

			var list = result.Where (view => view.Value != null).Select (view => {
				var item = view.Value;
				var lane = lanes.Where (l => l.id == item.lane_id).FirstOrDefault ();
				var parent = Utils.GetTopMostParent (lane, lanes);
				return new Build {
					Commit = item.revision,
					CommitId = item.revision_id,
					Date = item.completed ? item.endtime : item.date,
					Lane = lane.lane,
					LaneID = lane.id,
					Project = parent.lane,
					State = item.State,
					Author = item.author,
					BuildBot = view.Key == null ? string.Empty : view.Key.host,
					HostID = item.host_id,
					Url = Utils.MakeBuildUrl (item.lane_id, item.host_id, item.revision_id)
				};
			}).OrderByDescending (b => b.Date).ToList ();

			return new LatestBuildsResponse {
				LatestBuilds = list
			};
		}
	}
}

