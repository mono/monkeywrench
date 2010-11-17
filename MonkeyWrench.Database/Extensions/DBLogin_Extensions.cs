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

using MonkeyWrench.DataClasses;

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
				byte [] data = new byte [32];
				StringBuilder builder = new StringBuilder (data.Length);
				random.GetBytes (data);

				for (int i = 0; i < data.Length; i++)
					builder.Append (string.Format ("{0:x}", data [i]));
				builder.Append (DateTime.Now.Ticks);

				result.expires = DateTime.Now.AddDays (1);
				result.cookie = builder.ToString ();

				result.Save (db);
			}

			return result;
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
