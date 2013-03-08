using System;
using System.Linq;
using System.Collections.Generic;

using MonkeyWrench.Database;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;

using ServiceStack;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.ServiceHost;

namespace MonkeyWrench.Web.ServiceStack
{
	public class Projects {}

	public class ProjectsResponse
	{
		public ResponseStatus ResponseStatus { get; set; }
		public Dictionary<Lane, List<Lane>> Projects { get; set; }
	}

	[Authenticate]
	[RequiredRole (RoleNames.Admin)]
	public class ProjectsService : Service
	{
		public object Any (Projects request)
		{
			List<DBLane> lanes = null;
			using (DB db = new DB ())
				lanes = db.GetAllLanes ();
			var parents = new HashSet<int?> (lanes.Where (l => l.parent_lane_id != null).Select (l => l.parent_lane_id));
			return new ProjectsResponse {
				Projects = lanes.Where (l => !parents.Contains (l.id))
					            .ToLookup (l => Utils.GetTopMostParent (l, lanes))
					            .ToDictionary (ls => new Lane { ID = ls.Key.id, Name = ls.Key.lane },
					                           ls => ls.Select (l => new Lane { ID = l.id, Name = l.lane}).ToList ())
			};
		}
	}
}

