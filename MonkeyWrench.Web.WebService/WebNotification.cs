using System;
using System.Threading;
using System.Web;
using System.Web.Handlers;
using System.Collections.Generic;

using Newtonsoft.Json;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Database;

namespace MonkeyWrench.WebServices
{
	public class WebNotification : IHttpAsyncHandler
	{
		class ObserverSlot : IAsyncResult
		{
			ManualResetEvent evt = new ManualResetEvent (false);
			AsyncCallback callback;
			object extraData;

			public ObserverSlot (HttpContext context, AsyncCallback callback, object extraData)
			{
				this.Context = context;
				this.callback = callback;
				this.extraData = extraData;
			}

			public void Process (Build aBuild)
			{
				Build = aBuild;
				callback (this);
				evt.Set ();
			}

			public Build Build {
				get;
				private set;
			}

			public HttpContext Context {
				get;
				private set;
			}

			public object AsyncState {
				get {
					return extraData;
				}
			}

			public WaitHandle AsyncWaitHandle {
				get {
					return evt;
				}
			}

			public bool CompletedSynchronously {
				get {
					return false;
				}
			}

			public bool IsCompleted {
				get {
					return evt.WaitOne (0);
				}
			}
		}

		static List<ObserverSlot> slots = new List<ObserverSlot> ();

		public static void BroadcastBuildNotification (DBWork work, DBRevisionWork revisionWork)
		{
			BroadcastBuildNotification (new Lazy<Build> (() => GetBuildFromDBItems (work, revisionWork)));
		}

		public static void BroadcastBuildNotification (Build aBuild)
		{
			BroadcastBuildNotification (new Lazy<Build> (() => aBuild));
		}

		static void BroadcastBuildNotification (Lazy<Build> build)
		{
			List<ObserverSlot> currentSlots = null;
			lock (slots) {
				if (slots.Count == 0)
					return;
				currentSlots = new List<ObserverSlot> (slots);
				slots.Clear ();
			}

			foreach (var slot in currentSlots)
				slot.Process (build.Value);
		}

		static Build GetBuildFromDBItems (DBWork work, DBRevisionWork revisionWork)
		{
			DBRevision revision;
			DBLane lane, parentLane;
			DBHost host;

			using (DB db = new DB ()) {
				revision = DBRevision_Extensions.Create (db, revisionWork.revision_id);
				lane = DBLane_Extensions.Create (db, revisionWork.lane_id);
				parentLane = GetTopMostParent (lane, db);
				host = DBHost_Extensions.Create (db, revisionWork.host_id);
			}

			var url = Configuration.GetWebSiteUrl ();
			url += string.Format ("/ViewLane.aspx?lane_id={0}&host_id={1}&revision_id={2}", lane.id, host.id, revision.id);

			return new Build {
				Commit = revision.revision,
				CommitId = revision.id,
				Date = revisionWork.completed ? revisionWork.endtime : revision.date,
				Lane = lane.lane,
				Project = parentLane.lane,
				State = revisionWork.State,
				Author = revision.author,
				BuildBot = host.host,
				Url = url
			};
		}

		static DBLane GetTopMostParent (DBLane forLane, DB db)
		{
			var parent = forLane;
			while (parent.parent_lane_id != null)
				parent = DBLane_Extensions.Create (db, parent.parent_lane_id.Value);
			return parent;
		}

		public IAsyncResult BeginProcessRequest (HttpContext context, AsyncCallback cb, object extraData)
		{
			var login = CreateLogin (context.Request);
			using (var db = new DB ())
				Authentication.VerifyUserInRole (context, db, login, Roles.Administrator, @readonly: true);

			var slot = new ObserverSlot (context, cb, extraData);
			lock (slots)
				slots.Add (slot);
			return slot;
		}

		public void EndProcessRequest (IAsyncResult result)
		{
			var slot = result as ObserverSlot;
			var message = JsonConvert.SerializeObject (slot.Build);
			var ctx = slot.Context;

			ctx.Response.ContentType = "application/json";
			ctx.Response.Write (message);
		}

		public void ProcessRequest (HttpContext context)
		{
			throw new NotSupportedException ();
		}

		public bool IsReusable {
			get {
				return false;
			}
		}

		WebServiceLogin CreateLogin (HttpRequest request)
		{
			WebServiceLogin login = new WebServiceLogin ();
			
			login.Cookie = request ["cookie"];
			login.Password = request ["password"];
			if (string.IsNullOrEmpty (login.Cookie)) {
				if (request.Cookies ["cookie"] != null) {
					login.Cookie = request.Cookies ["cookie"].Value;
				}
			}

			login.User = request ["user"];
			if (string.IsNullOrEmpty (login.User)) {
				if (request.Cookies ["user"] != null) {
					login.User = request.Cookies ["user"].Value;
				}
			}
			
			login.Ip4 = request ["ip4"];
			if (string.IsNullOrEmpty (login.Ip4)) {
				login.Ip4 = Utilities.GetExternalIP (request);
			}
			
			return login;
		}
	}
}

