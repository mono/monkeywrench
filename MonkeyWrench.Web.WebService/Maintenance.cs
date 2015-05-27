/*
 * Maintenance.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (rolf@xamarin.com)
 *   
 * Copyright 2012 Xamarin Inc (http://xamarin.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using log4net;

using MonkeyWrench;

namespace MonkeyWrench.WebServices {
	public class Maintenance {
#pragma warning disable 414 
		static Timer timer;
#pragma warning restore 414

		private static readonly ILog log = LogManager.GetLogger (typeof (Maintenance));

		public static void Start ()
		{
			timer = new Timer (Maintain, null, TimeSpan.FromHours (1), TimeSpan.FromHours (1));
		}

		static void Maintain (object dummy)
		{
			try {
				CleanupEmptyRevisionWorks ();
				CleanupLogins ();
			} catch (Exception ex) {
				log.ErrorFormat ("Unhandled exception in maintenance thread: {0}", ex);
			}
		}

		static void CleanupEmptyRevisionWorks ()
		{
			int r;
			try {
				Stopwatch watch = new Stopwatch ();
				watch.Start ();
				using (DB db = new DB ()) {
					r = db.ExecuteNonQuery (
@"UPDATE revisionwork SET workhost_id = NULL, state = 10 WHERE id IN
	(SELECT id FROM revisionwork WHERE NOT EXISTS
		(SELECT work.id FROM work WHERE work.revisionwork_id = revisionwork.id)
    )
;");
				}
				watch.Stop ();
				log.InfoFormat ("successfully cleaned up empty revision works ({0} affected records) in {1} seconds", r, watch.Elapsed.TotalSeconds);
			} catch (Exception ex) {
				log.ErrorFormat ("failed to cleanup empty revision work: {0}", ex);
			}
		}

		static void CleanupLogins ()
		{
			int r;
			try {
				Stopwatch watch = new Stopwatch ();
				watch.Start ();
				using (DB db = new DB ()) {
					r = db.ExecuteNonQuery ("DELETE FROM login WHERE expires < now();");
				}
				watch.Stop ();
				log.InfoFormat ("successfully cleaned up logins ({0} affected records) in {1} seconds", r, watch.Elapsed.TotalSeconds);
			} catch (Exception ex) {
				log.ErrorFormat ("failed to cleanup logins: {0}", ex);
			}
		}
	}
}

