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
using log4net;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;

namespace MonkeyWrench.Database
{
	public static class DBLogin_Extensions
	{
		static readonly ILog log = LogManager.GetLogger (typeof (DBLogin_Extensions));
		static RandomNumberGenerator random = RandomNumberGenerator.Create ();

		/// <summary>
		/// Returns null if login failed.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="user"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public static DBLogin LoginUser (DB db, string login, string password, string ip4, bool @readonly)
		{
			DBLogin result;
			int id;

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

		public static void Login (DB db, LoginResponse response, string email, string ip4, List<string> userOrgs, bool useGitHub = false, string gitHubLogin = "")
		{
			string [] specs;

			// email is used when using OpenID/Google Auth, 
			// and is checked against the OpenIdRoles in the Wrench Config.
			// For GitHub auth, userOrgs is used to store the users
			// GitHub organizations which are checked against the configs.

			// Setting the useGitHub flag will pick which format to auth against,
			// GitHub or OpenID/Google.

			// Note: username is NOT used for checking for authorization.
			// It is used for adding that users name as the users Wrench account name

			string username = useGitHub ? gitHubLogin : email;

			if (useGitHub) {
				specs = Configuration.GitHubOrganizationList;
			}
			else {
				specs = Configuration.OpenIdRoles;
			}

			foreach (var spec in specs) {
				// org:role1,role2
				// email:role1,role2
				string [] split = spec.Split (':');
				if (split.Length != 2) {
					log.ErrorFormat ("AuthenticateLogin: Invalid role spec: {0}", spec);
					continue;
				}

				if (string.IsNullOrEmpty (split [1])) {
					log.ErrorFormat ("AuthenticateLogin: No roles specified for {0}", split [0]);
					continue;
				}

				var roleSpecCheck = split[0];
				var roles = split[1];

				if (useGitHub) {
					// userOrgs is the current orginizations the user is in.
					// If the org in the config file is in one the user is in, we can log them in.
					// Otherwise, continue until we find one, or fail.
					if (!userOrgs.Contains (roleSpecCheck))
						continue;
				}
				else {
					if (!Regex.IsMatch (email, roleSpecCheck))
						continue;
				}

				// We now create an account with an empty password and the specified roles.
				// Note that it is not possible to log into an account with an empty password
				// using the normal login procedure.

				DBPerson open_person = null;

				using (IDbCommand cmd = db.CreateCommand ()) {
					cmd.CommandText = @"SELECT * FROM Person WHERE login = @login;";
					DB.CreateParameter (cmd, "login", username);
					using (var reader = cmd.ExecuteReader ()) {
						if (reader.Read ())
							open_person = new DBPerson (reader);
					}
				}

				if (open_person == null) {
					open_person = new DBPerson ();
					open_person.login = username;
					open_person.roles = roles;
					open_person.Save (db);
				} else {
					// only save if something has changed
					if (open_person.roles != roles) {
						open_person.roles = roles;
						open_person.Save (db);
					}
				}
				WebServiceLogin login = new WebServiceLogin ();
				login.Ip4 = ip4;
				login.User = open_person.login;
				db.Audit (login, "DBLogin_Extensions.Login (username: {0}, ip4: {1})", username, ip4);

				var result = new DBLogin ();
				result.person_id = open_person.id;
				result.ip4 = ip4;
				result.cookie = CreateCookie ();
				result.expires = DateTime.Now.AddDays (1);
				result.Save (db);
				
				response.User = username;
				response.UserName = username;
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

