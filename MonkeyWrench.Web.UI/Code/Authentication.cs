/*
 * Authentication.cs
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
using System.Web;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public class Authentication
{
	public static void SavePassword (HttpResponse response, LoginResponse ws_response)
	{
		HttpCookie cookie = new HttpCookie ("cookie", ws_response.Cookie);
		cookie.Expires = DateTime.Now.AddDays (1);
		response.Cookies.Add (cookie);
		HttpCookie person = new HttpCookie ("user", ws_response.User);
		person.Expires = DateTime.Now.AddDays (1);
		response.Cookies.Add (person);
	}

	public static bool IsLoggedIn (WebServiceResponse response)
	{
		if (response == null)
			return false;

		return !string.IsNullOrEmpty (response.UserName);
	}

	public static bool IsInRole (WebServiceResponse response, string role)
	{
		bool result;

		if (response == null) {
			MonkeyWrench.Logger.Log (2, "IsInRole: no response");
			return false;
		}

		if (response.UserRoles == null) {
			MonkeyWrench.Logger.Log (2, "IsInRole: no userroles");
			return false;
		}
		
		result = Array.IndexOf (response.UserRoles, role) >= 0;

		MonkeyWrench.Logger.Log (2, "IsInRole ({0}) => {1} (roles: {2})", role, result, string.Join (";", response.UserRoles));

		return result;
	}

	public static bool IsInCookieRole (HttpRequest request, string role)
	{
		HttpCookie cookie;

		if (request == null)
			return false;

		if (request.Cookies ["cookie"] == null || string.IsNullOrEmpty (request.Cookies ["cookie"].Value))
			return false;

		cookie = request.Cookies ["roles"];
		if (cookie == null)
			return false;

		return Array.IndexOf<string> (cookie.Value.ToLowerInvariant ().Split (','), role.ToLowerInvariant ()) >= 0;
	}
	
	public static bool Login (string user, string password, HttpRequest Request, HttpResponse Response)
	{
		LoginResponse response;

		WebServiceLogin login = new WebServiceLogin ();
		login.User = user;
		login.Password = password;

		login.Ip4 = MonkeyWrench.Utilities.GetExternalIP (Request);
		response = Utils.WebService.Login (login);
		if (response == null) {
			Logger.Log ("Login failed");
			return false;
		} else {
			SetCookies (Response, response);
			return true;
		}
	}
	public static WebServiceLogin CreateLogin (HttpRequest Request)
	{
		WebServiceLogin login = new WebServiceLogin ();

		login.Cookie = Request ["cookie"];
		login.Password = Request ["password"];
		if (string.IsNullOrEmpty (login.Cookie)) {
			if (Request.Cookies ["cookie"] != null) {
				login.Cookie = Request.Cookies ["cookie"].Value;
			}
		}

		login.User = Request ["user"];
		if (string.IsNullOrEmpty (login.User)) {
			if (Request.Cookies ["user"] != null) {
				login.User = Request.Cookies ["user"].Value;
			}
		}

		login.Ip4 = Request ["ip4"];
		if (string.IsNullOrEmpty (login.Ip4)) {
			login.Ip4 = Utilities.GetExternalIP (Request);
		}

		return login;
	}

	public static void SetCookies (HttpResponse Response, LoginResponse response)
	{
		Logger.Log ("Login succeeded, cookie: {0}", response.Cookie);
		Response.Cookies.Add (new HttpCookie ("cookie", response.Cookie));
		Response.Cookies ["cookie"].Expires = DateTime.Now.AddDays (10);
		Response.Cookies.Add (new HttpCookie ("user", response.User));
		Response.Cookies ["user"].Expires = DateTime.Now.AddDays (10);
		/* Note that the 'roles' cookie is only used to determine the web ui to show, it's not used to authorize anything */
		Response.Cookies.Add (new HttpCookie ("roles", response.UserRoles == null ? string.Empty : string.Join (", ", response.UserRoles)));
		Response.Cookies ["roles"].Expires = DateTime.Now.AddDays (10);
	}
}