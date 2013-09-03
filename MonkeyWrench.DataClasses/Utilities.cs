﻿/*
 * Utilities.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;
using MonkeyWrench.DataClasses;

namespace MonkeyWrench
{
	public static class Utilities
	{
		public static string GetCookie (HttpRequest request, string name)
		{
			HttpCookie cookie = request.Cookies [name];
			return cookie == null ? null : cookie.Value;
		}

		public static string GetExternalIP (HttpRequest request)
		{
			if (request.IsLocal)
				return System.Net.Dns.GetHostEntry (System.Net.Dns.GetHostName ()).AddressList [0].ToString ();
			else
				return request.UserHostAddress;
		}

		public static string CreateWebServiceDownloadUrl (HttpRequest Request, int workfile_id, bool redirect)
		{
			return WebServices.CreateWebServiceDownloadUrl (workfile_id, CreateWebServiceLogin (Request), redirect);
		}

		public static string CreateWebServiceDownloadUrl (HttpRequest Request, int work_id, string filename, bool redirect)
		{
			return WebServices.CreateWebServiceDownloadNamedUrl (work_id, filename, CreateWebServiceLogin (Request), redirect);
		}

		public static WebServiceLogin CreateWebServiceLogin (HttpRequest Request)
		{
			WebServiceLogin web_service_login;
			web_service_login = new WebServiceLogin ();
			web_service_login.Cookie = GetCookie (Request, "cookie");
			if (HttpContext.Current.User != null)
				web_service_login.User = GetCookie (Request, "user");
			web_service_login.Ip4 = GetExternalIP (Request);

			// Console.WriteLine ("Master, Cookie: {0}, User: {1}", web_service_login.Cookie, web_service_login.User);

			return web_service_login;
		}

		public static bool IsInRole (WebServiceResponse response, string role)
		{
			bool result;

			if (response == null)
				return false;

			if (response.UserRoles == null)
				return false;

			result = Array.IndexOf (response.UserRoles, role) >= 0;

			return result;
		}

		public static TimeSpan GetDurationFromWorkView (DBWorkView2 step)
		{
			DateTime starttime = step.starttime.ToLocalTime ();
			DateTime endtime = step.endtime.ToLocalTime ();
			int duration = (int) (endtime - starttime).TotalSeconds;

			if (step.endtime.Year < DateTime.Now.Year - 1 && step.duration == 0) {// Not ended, endtime defaults to year 2000
				duration = (int) (DateTime.Now - starttime).TotalSeconds;
			} else if (step.endtime == DateTime.MinValue) {
				duration = step.duration;
			}

			return TimeSpan.FromSeconds (duration);
		}
	}
}
