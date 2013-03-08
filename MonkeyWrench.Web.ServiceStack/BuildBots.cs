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
	public class BuildBots {}

	public class BuildBotsResponse
	{
		public ResponseStatus ResponseStatus { get; set; }
		public List<BuildBot> BuildBots { get; set; }
	}
	
	[Authenticate]
	[RequiredRole (RoleNames.Admin)]
	public class BuildBotsService : Service
	{
		public object Any (BuildBots request)
		{
			List<DBHost> hosts = null;
			using (DB db = new DB ())
				hosts = db.GetHosts ();

			return new BuildBotsResponse {
				BuildBots = hosts.Select (h => new BuildBot {
					ID = h.id,
					Name = h.host,
					Arch = h.architecture,
					Description = h.description
				}).ToList ()
			};
		}
	}
}

