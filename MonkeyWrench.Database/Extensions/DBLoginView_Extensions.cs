/*
 * DBLoginView_Extensions.cs
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
	public static class DBLoginView_Extensions
	{
		public static DBLoginView VerifyLogin (DB db, string person, string cookie, string ip4)
		{
			DBLoginView result;

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM LoginView WHERE cookie = @cookie AND login = @person AND ip4 = @ip4 AND ip4 <> '';";
				DB.CreateParameter (cmd, "cookie", cookie);
				DB.CreateParameter (cmd, "person", person);
				DB.CreateParameter (cmd, "ip4", ip4);
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						return null;

					result = new DBLoginView (reader);

					if (reader.Read ())
						return null;

					return result;
				}
			}
		}
	}
}
