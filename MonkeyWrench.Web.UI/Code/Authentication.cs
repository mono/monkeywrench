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

	public static bool IsInRole (WebServiceResponse response, string role)
	{
		if (response.UserRoles == null)
			return false;

		return Array.IndexOf (response.UserRoles, role) >= 0;
	}
}