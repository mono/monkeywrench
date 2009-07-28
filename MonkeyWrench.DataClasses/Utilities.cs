/*
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

		public static WebServiceLogin CreateWebServiceLogin (HttpRequest Request)
		{
			WebServiceLogin web_service_login;
			web_service_login = new WebServiceLogin ();
			web_service_login.Cookie = GetCookie (Request, "cookie");
			if (HttpContext.Current.User != null)
				web_service_login.User = HttpContext.Current.User.Identity.Name;
			web_service_login.Ip4 = GetExternalIP (Request);

			// Console.WriteLine ("Master, Cookie: {0}, User: {1}", web_service_login.Cookie, web_service_login.User);

			return web_service_login;
		}

		public static bool IsInRole (string role)
		{
			return HttpContext.Current.User.IsInRole (role);
		}
	}
}
