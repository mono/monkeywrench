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

	[Route ("/buildsteps/{LaneID}/{HostID}/{RevisionID}")]
	public class BuildSteps
	{
		public int LaneID { get; set; }
		public int HostID { get; set; }
		public int RevisionID { get; set; }
	}

	public class BuildStepsValidator : AbstractValidator<BuildSteps>
	{
		public BuildStepsValidator ()
		{
			RuleFor (bs => bs.LaneID).GreaterThan (0);
			RuleFor (bs => bs.HostID).GreaterThan (0);
			RuleFor (bs => bs.RevisionID).GreaterThan (0);
		}
	}

	public class BuildStepsResponse
	{
		public ResponseStatus ResponseStatus { get; set; }
		public List<BuildStep> BuildSteps { get; set; }
	}

	[Authenticate]
	[RequiredRole (RoleNames.Admin)]
	public class BuildStepsService : Service
	{
		public object Any (BuildSteps request)
		{
			List<DBWorkView2> workViews = null;
			List<List<DBWorkFileView>> files = null;

			using (var db = new DB ()) {
				var lane = Utils.FindLane (db, request.LaneID);
				var host = Utils.FindHost (db, request.HostID);
				var revision = Utils.FindRevision (db, request.RevisionID);
				var revisionWork = DBRevisionWork_Extensions.Find (db, lane, host, revision);
				workViews = db.GetWork (revisionWork);
				files = new List<List<DBWorkFileView>> ();
				for (int i = 0; i < workViews.Count; i++)
					files.Add (DBWork_Extensions.GetFiles (db, workViews [i].id, false));
			}

			var list = workViews
				.Where (step => step != null)
				.Select ((step, i) => new BuildStep {
					Name = step.command,
					State = (DBState)step.state,
					StartDate = step.state > (int)DBState.NotDone && step.state != (int)DBState.Paused ? step.starttime : (DateTime?)null,
					Duration = step.state >= (int)DBState.Executing && step.state != (int)DBState.Paused ? Utils.GetDurationFromWorkView (step) : (TimeSpan?)null,
					Author = step.author,
					LogId = files[i].Count > 0 ? files[i].FirstOrDefault (f => f.filename.StartsWith (step.command)).id.ToString () : null
				})
				.ToList ();

			return new BuildStepsResponse {
				BuildSteps = list
			};
		}
	}
}

