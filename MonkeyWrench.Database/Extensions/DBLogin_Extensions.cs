/*
 * DBLogin_Extensions.cs
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
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;

namespace MonkeyWrench.Database
{
	public static class DBLogin_Extensions
	{

		static RandomNumberGenerator random = RandomNumberGenerator.Create ();

		/// <summary>
		/// Returns null if login failed.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="user"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public static DBLogin Login (DB db, string login, string password, string ip4, bool @readonly)
		{
			DBLogin result;
			int id;

			Logger.Log (2, "DBLogin.Login ('{0}', '{1}', '{2}'. {3})", login, password, ip4, @readonly);

			using (IDbCommand cmd = db.CreateCommand ()) {
				// TODO: Encrypt passwords somehow, not store as plaintext.
				cmd.CommandText = "SELECT id FROM Person WHERE login = @login AND password = @password;";
				DB.CreateParameter (cmd, "login", login);
				DB.CreateParameter (cmd, "password", password);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						return null;

					id = reader.GetInt32 (0);

					//if (reader.Read ())
					//	return null;
				}
			}

			result = new DBLogin ();
			result.person_id = id;
			result.ip4 = ip4;

			if (!@readonly) {
				result.expires = DateTime.Now.AddDays (1);
				result.cookie = CreateCookie ();

				result.Save (db);
			}

			return result;
		}

		public static void LoginOpenId (DB db, LoginResponse response, string email, string ip4)
		{
			Logger.Log (2, "DBLogin.LoginOpenId ({0}, {1})", email, ip4);

			if (string.IsNullOrEmpty (Configuration.OpenIdProvider))
				throw new Exception ("No OpenId provider available");

			if (string.IsNullOrEmpty (Configuration.OpenIdRoles))
				throw new Exception ("No OpenId roles specified");

			if (string.IsNullOrEmpty (email))
				throw new Exception ("OpenId authentication requires an email");
			
			string [] specs = Configuration.OpenIdRoles.Split (';');
			foreach (var spec in specs) {
				// email:role1,role2
				string [] split = spec.Split (':');
				if (split.Length != 2) {
					Logger.Log ("AuthenticateOpenId: Invalid role spec: {0}", spec);
					continue;
				}

				if (string.IsNullOrEmpty (split [1])) {
					Logger.Log ("AuthenticateOpenId: No roles specified for {0}", split [0]);
					continue;
				}

				if (!Regex.IsMatch (email, split [0]))
					continue;

				// We now create an account with an empty password and the specified roles.
				// Note that it is not possible to log into an account with an empty password
				// using the normal login procedure.

				DBPerson open_person = null;

				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"SELECT * FROM Person WHERE login = @login;";
					DB.CreateParameter (cmd, "login", email);
					using (var reader = cmd.ExecuteReader ()) {
						if (reader.Read ())
							open_person = new DBPerson (reader);
					}
				}

				if (open_person == null) {
					open_person = new DBPerson ();
					open_person.login = email;
					open_person.roles = split [1];
					open_person.Save (db);
				} else {
					// only save if something has changed
					if (open_person.roles != split [1]) {
						open_person.roles = split [1];
						open_person.Save (db);
					}
				}

				var result = new DBLogin ();
				result.person_id = open_person.id;
				result.ip4 = ip4;
				result.cookie = CreateCookie ();
				result.expires = DateTime.Now.AddDays (1);
				result.Save (db);
				
				response.User = email;
				response.UserName = email;
				response.UserRoles = open_person.Roles;
				response.Cookie = result.cookie;

				return;
			}

			throw new Exception ("The provided email address is not allowed to log in");
		}

		public static string CreateCookie ()
		{
			byte [] data = new byte [32];
			StringBuilder builder = new StringBuilder (data.Length);
			random.GetBytes (data);

			for (int i = 0; i < data.Length; i++)
				builder.Append (string.Format ("{0:x}", data [i]));
			builder.Append (DateTime.Now.Ticks);

			return builder.ToString ();
		}

		public static void Logout (DB db, string cookie)
		{
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "DELETE FROM Login WHERE cookie = @cookie;";
				DB.CreateParameter (cmd, "cookie", cookie);
				cmd.ExecuteNonQuery ();
			}
		}
	}
}

