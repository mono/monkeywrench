using System;
using System.Collections.Generic;
using System.Web;

using Builder;

/// <summary>
/// Summary description for Authentication
/// </summary>
public class Authentication
{
	/// <summary>
	/// </summary>
	/// <param name="request"></param>
	/// <returns></returns>
	public static DBLoginView GetLogin (DB db, HttpRequest request, HttpResponse response)
	{
			HttpCookie cookie;
			HttpCookie person;
			DBLoginView login;

			cookie = request.Cookies.Get ("password");
			person = request.Cookies.Get ("person");

			Console.WriteLine ("GetLogin... cookie: {0} person: {1}", cookie, person);

			if (cookie == null || person == null || string.IsNullOrEmpty (person.Value) || string.IsNullOrEmpty (cookie.Value))
				return null;

			Console.WriteLine ("GetLogin, person: {0}, password: {1}", person.Value, cookie.Value);

			login = DBLoginView.VerifyLogin (db, person.Value, cookie.Value, request.UserHostAddress);

			if (login != null) {
				Console.WriteLine ("GetLogin: Success");

				// TODO: update login's expiry to another X hours.
				cookie.Expires = DateTime.Now.AddDays (1);
				person.Expires = DateTime.Now.AddDays (1);
				response.Cookies.Add (cookie);
				response.Cookies.Add (person);
				return login;
			} else {
				cookie.Expires = DateTime.Now.AddDays (-1);
				response.Cookies.Add (cookie);
				Console.WriteLine ("GetLogin: Failed.");
				return null;
			}
	}

	public static void SavePassword (HttpResponse response, DBLogin login, string user)
	{
		HttpCookie cookie = new HttpCookie ("password", login.cookie);
		cookie.Expires = DateTime.Now.AddDays (1);
		response.Cookies.Add (cookie);
		HttpCookie person = new HttpCookie ("person", user);
		person.Expires = DateTime.Now.AddDays (1);
		response.Cookies.Add (person);
	}

	public static void DeletePassword (HttpRequest request, HttpResponse response)
	{
		HttpCookie cookie = request.Cookies ["password"];

		if (cookie == null || string.IsNullOrEmpty (cookie.Value))
			return;

		response.Cookies.Remove ("password");

		using (DB db = new DB (true)) {
			DBLogin.Logout (db, cookie.Value);
		}
	}
}