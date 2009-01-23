/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */


using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;

namespace Builder
{
	public partial class DBLogin : DBRecord
	{
		public const string TableName = "Login";

		public DBLogin ()
		{
		}
	
		public DBLogin (DB db, int id)
			: base (db, id)
		{
		}
	
		public DBLogin (IDataReader reader) 
			: base (reader)
		{
		}

		/// <summary>
		/// Returns null if login failed.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="user"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public static DBLogin Login (DB db, string login, string password, string ip4)
		{
			DBLogin result;
			int id;

			Console.WriteLine ("DBLogin.Login ('{0}', '{1}', '{2}')", login, password, ip4);

			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				// TODO: Encrypt passwords somehow, not store as plaintext.
				cmd.CommandText = "SELECT id FROM Person WHERE login = @login AND password = @password;";
				DB.CreateParameter (cmd, "login", login);
				DB.CreateParameter (cmd, "password", password);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						return null;

					id = reader.GetInt32 (0);

					if (reader.Read ())
						return null;
				}
			}
			
			Random random = new Random ();
			byte [] data=  new byte [32];
			StringBuilder builder = new StringBuilder (data.Length);
			random.NextBytes (data);

			for (int i = 0; i < data.Length; i++)
				builder.Append (string.Format ("{0:x}", data [i]));

			result = new DBLogin ();
			result.expires = DateTime.Now.AddDays (1);
			result.person_id = id;
			result.ip4 = ip4;
			result.cookie = builder.ToString ();

			result.Save (db);

			return result;
		}

		public static void Logout (DB db, string cookie)
		{
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "DELETE FROM Login WHERE cookie = @cookie;";
				DB.CreateParameter (cmd, "cookie", cookie);
				cmd.ExecuteNonQuery ();
			}
		}
	}
}

