using System;
using System.Data;
using System.Web;
using System.Web.UI;
using Newtonsoft.Json.Linq;

using MonkeyWrench;

namespace MonkeyWrench.Web.JSON {
	/**
	 * JSON API for fetching time info (created, assigned, started, and ended) for RevisionWorks.
	 */
	public class RecentRevWorks : System.Web.IHttpHandler {
		const int LIMIT = 200;

		/**
		 * Converts DateTime to milliseconds past the unix epoch
		 */
		private static ulong? dateTimeToMilliseconds(DateTime? t) {
			return t == null ? null : (ulong?)((t.Value - new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
		}

		public void ProcessRequest (HttpContext context) {
			var lane_id = context.Request ["lane_id"].ToUInt32 ();
			if (lane_id == null) {
				context.Response.StatusCode = 400;
				context.Response.ContentType = "text/plain";
				context.Response.Write ("lane_id GET parameter is required.");
				return;
			}

			using (var db = new DB ())
			using (var cmd = db.CreateCommand ()) {
				DB.CreateParameter (cmd, "nlimit", LIMIT);
				DB.CreateParameter (cmd, "lane_id", (int) lane_id.Value);

				cmd.CommandText = @"SELECT 1 FROM lane WHERE id = @lane_id";
				if (cmd.ExecuteScalar () == null) {
					context.Response.StatusCode = 404;
					context.Response.ContentType = "text/plain";
					context.Response.Write ("No such lane.");
					return;
				}

				cmd.CommandText = @"
					SELECT rw.id, lane, host, revision, rw.state, rw.createdtime, rw.assignedtime, rw.startedtime, rw.endtime
					FROM revisionwork AS rw
					INNER JOIN lane ON lane.id = rw.lane_id
					INNER JOIN host ON host.id = rw.host_id
					INNER JOIN revision ON revision.id = rw.revision_id
					WHERE lane.id = @lane_id AND createdtime IS NOT NULL
					ORDER BY rw.createdtime DESC
					LIMIT @nlimit;
				";

				using (var reader = cmd.ExecuteReader ()) {
					var commits = new JArray ();
					while (reader.Read ()) {
						var commit = new JObject ();
						commit ["id"] = reader.GetInt32 (0);
						commit ["lane"] = reader.GetString (1);
						commit ["host"] = reader.GetString (2);
						commit ["revision"] = reader.GetString (3);
						commit ["status"] = reader.GetInt32 (4);
						commit ["createdtime"] = dateTimeToMilliseconds(reader.GetDateTimeOrNull(5));
						commit ["assignedtime"] = dateTimeToMilliseconds(reader.GetDateTimeOrNull(6));
						commit ["startedtime"] = dateTimeToMilliseconds(reader.GetDateTimeOrNull(7));
						commit ["endtime"] = dateTimeToMilliseconds(reader.GetDateTimeOrNull(8));
						commits.Add (commit);
					}
					context.Response.StatusCode = 200;
					context.Response.ContentType = "application/json";
					context.Response.Write (commits.ToString ());
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

