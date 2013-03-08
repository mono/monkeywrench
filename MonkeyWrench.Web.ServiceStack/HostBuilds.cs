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
using ServiceStack.ServiceHost;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;

namespace MonkeyWrench.Web.ServiceStack
{
	[Route ("/hostbuilds/{HostID}")]
	public class HostBuilds
	{
		public int HostID { get; set; }
		[Default (0)]
		public int Offset { get; set; }
		[Default (10)]
		public int Limit  { get; set; }
	}

	public class HostBuildsValidator : AbstractValidator<HostBuilds>
	{
		public HostBuildsValidator ()
		{
			RuleFor (hb => hb.HostID).GreaterThan (0);
			RuleFor (hb => hb.Offset).GreaterThan (0);
			RuleFor (hb => hb.Limit).GreaterThanOrEqualTo (0);
		}
	}

	public class HostBuildsResponse
	{
		public ResponseStatus ResponseStatus { get; set; }
		public List<Build> Builds { get; set; }
	}

	[Authenticate]
	[RequiredRole (RoleNames.Admin)]
	public class HostBuildsService : Service
	{
		public object Any (HostBuilds request)
		{
			var revisionWorks = new List<DBRevisionWork> ();
			var revisions = new List<string> ();
			var lanes = new List<string> ();
			var startTimes = new List<DateTime> ();

			using (DB db = new DB ()) {
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"
SELECT RevisionWork.*, Host.host, Lane.lane, Revision.revision, MAX (Work.starttime) AS order_date,
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
WHERE RevisionWork.workhost_id = @host_id 
GROUP BY RevisionWork.id, RevisionWork.lane_id, RevisionWork.host_id, RevisionWork.workhost_id, RevisionWork.revision_id, RevisionWork.state, RevisionWork.lock_expires, RevisionWork.completed, RevisionWork.endtime, Lane.lane, Revision.revision, Host.host ";
					cmd.CommandText += " ORDER BY order_date DESC ";
					if (request.Limit > 0)
						cmd.CommandText += " LIMIT " + request.Limit.ToString ();
					if (request.Offset > 0)
						cmd.CommandText += " OFFSET " + request.Offset.ToString ();
					cmd.CommandText += ";";
					DB.CreateParameter (cmd, "host_id", request.HostID);
					
					using (IDataReader reader = cmd.ExecuteReader ()) {
						int lane_idx = reader.GetOrdinal ("lane");
						int revision_idx = reader.GetOrdinal ("revision");
						int starttime_idx = reader.GetOrdinal ("order_date");
						while (reader.Read ()) {
							revisionWorks.Add (new DBRevisionWork (reader));
							lanes.Add (reader.GetString (lane_idx));
							revisions.Add (reader.GetString (revision_idx));
							startTimes.Add (reader.GetDateTime (starttime_idx));
						}
					}
				}
			}

			var list = revisionWorks.Select ((e, i) => new Build {
				Commit = revisions [i],
				CommitId = revisionWorks[i].revision_id,
				State = e.State,
				Date = e.completed ? e.endtime : startTimes[i],
				Lane = lanes [i]
			}).ToList ();

			return new HostBuildsResponse {
				Builds = list
			};
		}
	}
}

