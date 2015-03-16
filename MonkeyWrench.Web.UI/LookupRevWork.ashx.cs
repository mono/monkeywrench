using System;
using System.Web;
using System.Web.UI;

using MonkeyWrench;
using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Web.UI
{
	
	public class LookupRevWork : System.Web.IHttpHandler
	{
	
		public void ProcessRequest (HttpContext context)
		{
			if (context.Request ["host"] == null || context.Request ["lane"] == null || context.Request ["revision"] == null) {
				context.Response.StatusCode = 400;
				context.Response.ContentType = "text/plain";
				context.Response.Write ("The following parameters are required: host, lane, revision");
				return;
			}

			using (var db = new DB ())
			using (var cmd = db.CreateCommand (@"
				SELECT revisionwork.host_id, revisionwork.lane_id, revisionwork.revision_id
				FROM revisionwork
				INNER JOIN host ON host.id = revisionwork.host_id
				INNER JOIN lane ON lane.id = revisionwork.lane_id
				INNER JOIN revision ON revision.id = revisionwork.revision_id
				WHERE host = @host AND lane = @lane AND revision = @revision
			")) {
				DB.CreateParameter (cmd, "host", context.Request ["host"]);
				DB.CreateParameter (cmd, "lane", context.Request ["lane"]);
				DB.CreateParameter (cmd, "revision", context.Request ["revision"]);

				using (var reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						throw new HttpException (404, "Revision work not found.");

					context.Response.RedirectPermanent (String.Format ("/ViewLane.aspx?host_id={0}&lane_id={1}&revision_id={2}",
						reader.GetInt32 (0), reader.GetInt32 (1), reader.GetInt32 (2)
					));
				}
			}
		}

		public bool IsReusable {
			get {
				return true;
			}
		}
	}
}
	
