
using System;
using System.Web;
using System.Web.UI;
using System.Data.Linq;

using Newtonsoft.Json.Linq;

namespace MonkeyWrench.Web.JSON {
	public class Lanes : System.Web.IHttpHandler {
		public void ProcessRequest (HttpContext context) {
			using (var db = new DB ())
			using (var cmd = db.CreateCommand (@"SELECT id, lane FROM lane WHERE enabled;"))
			using (var reader = cmd.ExecuteReader ()) {
				var results = new JArray ();
				while (reader.Read ()) {
					var lane = new JObject ();
					lane ["id"] = reader.GetInt32 (0);
					lane ["name"] = reader.GetString (1);
					results.Add (lane);
				}

				context.Response.StatusCode = 200;
				context.Response.ContentType = "application/json";
				context.Response.Write (results.ToString ());
			};
		}

		public bool IsReusable {
			get {
				return true;
			}
		}
	}
}
	
