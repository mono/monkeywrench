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
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;
using Npgsql;

namespace Builder
{
	public abstract class DBRecord
	{
		public int id;
		/// <summary>
		/// Setting a datetime field to this value will cause any insert/update to use the
		/// now() function to get the value for the field instead of this value.
		/// </summary>
		public static readonly DateTime DatabaseNow = DateTime.MinValue.AddMinutes (123);

		public DBRecord ()
		{
		}

		public DBRecord (DB db, int id)
		{
			using (IDbCommand cmd = db.Connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM " + Table + " WHERE id = " + id.ToString ();
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						throw new Exception (string.Format ("Could not find the id {0} in the table {1}", id, Table));
					this.Load (reader);
					if (reader.Read ())
						throw new Exception (string.Format ("Found more than one record with the id {0} in the table {1}", id, Table));
				}
			}
		}

		public DBRecord (IDataReader reader)
		{
			Load (reader);
		}

		public abstract string Table { get; }

		public virtual void Load (IDataReader reader)
		{
			LoadInternal (reader);
		}

		public virtual void Save (IDbConnection connection)
		{
			SaveInternal (connection);
		}

		public void Save (DB db)
		{
			Save (db.Connection);
		}

		public virtual void Delete (IDbConnection connection)
		{
			DeleteInternal (connection);
		}

		public void Delete (DB db)
		{
			Delete (db.Connection);
		}

		public static void Delete (DB db, int id, string Table)
		{
			DeleteInternal (db.Connection, id, Table);
		}

		public virtual string [] Fields { get { return null; } }

		public void Reload (DB db)
		{
			Reload (db.Connection);
		}

		public void Reload (IDbConnection connection)
		{
			if (id <= 0)
				throw new ArgumentException ("There's no id to reload from.");

			using (IDbCommand cmd = connection.CreateCommand ()) {
				cmd.CommandText = "SELECT * FROM " + Table + " WHERE id = " + id.ToString ();
				using (IDataReader reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						throw new Exception ("No records found");

					Load (reader);

					if (reader.Read ())
						throw new Exception ("More than one record found.");
				}
			}
		}

		/// <summary>
		/// A default Load implementation using reflection
		/// </summary>
		/// <param name="cmd"></param>
		protected void LoadInternal (IDataReader cmd)
		{
			FieldInfo fi;
			object value;
			int ordinal;
			id = cmd.GetInt32 (0);

			foreach (string field in Fields) {
				fi = GetField (field);
				ordinal = cmd.GetOrdinal (field);
				if (ordinal >= 0 && ordinal < cmd.FieldCount) {
					value = cmd.GetValue (cmd.GetOrdinal (field));
					if (value == DBNull.Value)
						value = null;
					if (fi != null)
						fi.SetValue (this, value);
					else
						Logger.Log ("{0} Could not find the field '{1}'", GetType ().Name, field);
				} else {
					Logger.Log ("{0}.LoadInternal: Could not find the field '{1}' in the reader.", GetType ().Name, field);
				}
			}
		}

		/// <summary>
		/// A default Save implementation using reflection
		/// </summary>
		/// <param name="connection"></param>
		protected void SaveInternal (IDbConnection connection)
		{
			string sql;
			string [] fields = Fields;

			using (IDbCommand cmd = connection.CreateCommand ()) {
				if (id == 0) {
					sql = "INSERT INTO " + Table + "(";
					sql += string.Join (", ", fields);
					sql += ") VALUES (";
					sql += "@" + string.Join (", @", fields);
					sql += ");\n";
					sql += "SELECT currval(pg_get_serial_sequence('" + Table + "', 'id'));";
					id = -1; // Don't know how to get the id from the db once the record has been saved, disable multiple save until then
				} else if (id > 0) {
					sql = "UPDATE " + Table + " SET ";
					for (int i = 0; i < fields.Length; i++) {
						sql += fields [i] + " = @" + fields [i];
						if (i != fields.Length - 1)
							sql += ", ";
					}
					sql += " WHERE id = " + id.ToString ();
				} else { //if (id == -1) {
					throw new Exception ("Can't save more than once unless you loaded the revision from db.");
				}

				DB.CreateParameter (cmd, "id", id);
				foreach (string field in fields) {
					FieldInfo fieldinfo = GetField (field);
					object value = fieldinfo.GetValue (this);
					if (value == null && fieldinfo.FieldType == typeof (string)) {
						value = string.Empty;
					}  else if (value is DateTime && ((DateTime) value) == DatabaseNow) {
						sql = sql.Replace ("@" + field, "now () AT TIME ZONE 'UTC'");
						continue;
					}
					DB.CreateParameter (cmd, field, value);
				}

				cmd.CommandText = sql;

				if (id == -1) {
					object o = cmd.ExecuteScalar ();
					id = (int) (long) o;
				} else {
					cmd.ExecuteNonQuery ();
				}
			}
		}

		private FieldInfo GetField (string field)
		{
			FieldInfo result;

			result = GetType ().GetField (field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (result == null)
				result = GetType ().GetField ("_" + field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			return result;
		}

		/// <summary>
		/// A default Delete implementation
		/// </summary>
		/// <param name="connection"></param>
		protected static void DeleteInternal (IDbConnection connection, int id, string Table)
		{
			if (id <= 0)
				throw new Exception (Table + " doesn't have an id.");

			using (IDbCommand cmd = connection.CreateCommand ()) {
				cmd.CommandText = "DELETE FROM " + Table + " WHERE id = " + id.ToString ();
				cmd.ExecuteNonQuery ();
			}
		}

		protected void DeleteInternal (IDbConnection connection)
		{
			DeleteInternal (connection, id, Table);
		}
	}
}