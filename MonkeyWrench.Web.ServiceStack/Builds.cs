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
	[Route ("/builds/{LaneID}")]
	public class Builds
	{
		public int LaneID { get; set; }
		[Default (0)]
		public int Offset { get; set; }
		[Default (10)]
		public int Limit  { get; set; }
	}

	public class BuildsValidator : AbstractValidator<Builds>
	{
		public BuildsValidator ()
		{
			RuleFor (bs => bs.LaneID).GreaterThan (0);
			RuleFor (bs => bs.Offset).GreaterThan (0);
			RuleFor (bs => bs.Limit).GreaterThanOrEqualTo (0);
		}
	}
	
	public class BuildsResponse
	{
		public ResponseStatus ResponseStatus { get; set; }
		public List<Build> Builds { get; set; }
	}
	
	[Authenticate]
	[RequiredRole (RoleNames.Admin)]
	public class BuildsService : Service
	{
		public object Any (Builds request)
		{
			List<DBRevisionWorkView> revisions = new List<DBRevisionWorkView> ();
			using (var db = new DB ()) {
				var hostLanes = new List<DBHostLane> ();
				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"SELECT HostLane.* FROM HostLane WHERE hidden = false AND lane_id = @lane_id";
					DB.CreateParameter (cmd, "lane_id", request.LaneID);

					using (IDataReader reader = cmd.ExecuteReader ())
						while (reader.Read ())
							hostLanes.Add (new DBHostLane (reader));
				}

				foreach (var hl in hostLanes) {
					using (IDbCommand cmd = db.CreateCommand ()) {
						cmd.CommandText = DBRevisionWorkView.SQL;
						DB.CreateParameter (cmd, "lane_id", request.LaneID);
						DB.CreateParameter (cmd, "host_id", hl.host_id);
						DB.CreateParameter (cmd, "limit", request.Limit);
						DB.CreateParameter (cmd, "offset", request.Offset);
						
						using (IDataReader reader = cmd.ExecuteReader ())
							while (reader.Read ())
								revisions.Add (new DBRevisionWorkView (reader));
					}
				}
			}
			
			return new BuildsResponse {
				Builds = revisions.Where (rev => rev != null).Distinct (RevisionWorkViewComparer.Instance).Select (rev => new Build {
					Commit = rev.revision,
					CommitId = rev.revision_id,
					Date = rev.State > DBState.Executing ? rev.endtime : rev.starttime,
					Lane = string.IsNullOrEmpty (rev.lane) ?  "(no lane)" : rev.lane,
					Project = string.IsNullOrEmpty (rev.lane) ? "(no parent)" : rev.lane,
					State = rev.RevisionWorkState,
					Author = rev.author,
					BuildBot = rev.host,

				}).ToList ()
			};
		}

		class RevisionWorkViewComparer : IEqualityComparer<DBRevisionWorkView>
		{
			public static readonly RevisionWorkViewComparer Instance = new RevisionWorkViewComparer ();
			
			public bool Equals (DBRevisionWorkView x, DBRevisionWorkView y)
			{
				return x.revision == y.revision;
			}
			
			public int GetHashCode (DBRevisionWorkView obj)
			{
				return obj.revision.GetHashCode ();
			}
		}
	}
}

