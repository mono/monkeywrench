/*
 * DBFileDeletionDirective_Extensions.cs
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
	public static class DBFileDeletionDirective_Extensions
	{

		public static List<DBFileDeletionDirective> GetAll (DB db)
		{
			List<DBFileDeletionDirective> result = new List<DBFileDeletionDirective> ();

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM FileDeletionDirective";
				using (IDataReader reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						result.Add (new DBFileDeletionDirective (reader));
					}
				}
			}

			return result;
		}
	}
}
