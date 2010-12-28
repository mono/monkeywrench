/*
 * DBPerson_Extensions.cs
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
using System.Text;

using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Database
{
	public static class DBPerson_Extensions
	{
		public static DBPerson Create (DB db, int id)
		{
			return DBRecord_Extensions.Create (db, new DBPerson (), DBPerson.TableName, id);
		}

		public static List<DBPerson> GetAll (DB db)
		{
			List<DBPerson> result = new List<DBPerson> ();

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM Person";
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (new DBPerson (reader));
					}
				}
			}

			return result;
		}

		public static List<string> GetEmails (this DBPerson person, DB db)
		{
			List<string> result = new List<string> ();

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT email FROM UserEmail WHERE person_id = @person_id;";
				DB.CreateParameter (cmd, "person_id", person.id);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (reader.GetString (0));
					}
				}
			}

			return result;
		}

		public static void AddEmail (this DBPerson person, DB db, string email)
		{
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "INSERT INTO UserEmail (person_id, email) VALUES (@person_id, @email);";
				DB.CreateParameter (cmd, "person_id", person.id);
				DB.CreateParameter (cmd, "email", email);
				cmd.ExecuteNonQuery ();
			}
		}

		public static void RemoveEmail (this DBPerson person, DB db, string email)
		{
			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "DELETE FROM UserEmail WHERE person_id = @person_id AND email = @email;";
				DB.CreateParameter (cmd, "person_id", person.id);
				DB.CreateParameter (cmd, "email", email);
				cmd.ExecuteNonQuery ();
			}
		}
	}
}
