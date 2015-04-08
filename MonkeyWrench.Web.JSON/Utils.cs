
using System;
using System.Data;
using System.Data.Linq;
using System.Collections.Generic;

namespace MonkeyWrench.Web.JSON
{
	public static class Utils
	{
		/**
		 * Returns true if the user with the passed name and password should be allowed access to the JSON API.
		 * 
		 * If name is null, check for anonymous accesss instead.
		 */
		public static bool isAuthorized(DB db, string name, string password) {
			if (name == null)
				return Configuration.AllowAnonymousAccess;

			using (var cmd = db.CreateCommand (@"SELECT 1 FROM person WHERE login = @username AND password = @password AND password != ''")) {
				DB.CreateParameter (cmd, "username", name);
				// If password is null, then the condition "password = @password" in SQL will always fail.
				DB.CreateParameter (cmd, "password", password);

				using (var reader = cmd.ExecuteReader ()) {
					return reader.Read ();
				}
			}
		}

		/**
		 * Returns the parsed uint, or null if parsing failed.
		 */
		public static uint? ToUInt32(this string s) {
			uint i;
			return UInt32.TryParse (s, out i) ? (uint?)i : null;
		}

		/**
		 * Gets DateTime or null
		 */
		public static DateTime? GetDateTimeOrNull(this IDataRecord reader, int i) {
			return reader.IsDBNull (i) ? null : (DateTime?)reader.GetDateTime (i);
		}
	}
}

