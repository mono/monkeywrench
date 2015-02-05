/*
 * DBRecord.cs
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
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;

namespace MonkeyWrench.DataClasses
{
	public abstract class DBRecord
	{
		public int id;
		/// <summary>
		/// Setting a datetime field to this value will cause any insert/update to use the
		/// now() function to get the value for the field instead of this value.
		/// </summary>
		public static readonly DateTime DatabaseNow = DateTime.MinValue.AddMinutes (123);
		public virtual string [] Fields { get { return null; } }
		public abstract string Table { get; }

		private static readonly ConcurrentDictionary<Type, Dictionary<string, FieldInfo>> fieldsCache =
			new ConcurrentDictionary<Type, Dictionary<string, FieldInfo>> ();

		public DBRecord ()
		{
		}

		public DBRecord (IDataReader reader)
		{
			Load (reader);
		}

		public virtual void Load (IDataReader reader)
		{
			LoadInternal (reader);
		}

		public virtual void Save (IDB db)
		{
			SaveInternal (db);
		}

		public virtual void Delete (IDB db)
		{
			DeleteInternal (db);
		}

		public void Reload (IDB db)
		{
			if (id <= 0)
				throw new ArgumentException ("There's no id to reload from.");

			using (IDbCommand cmd = db.CreateCommand ()) {
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
			id = cmd.GetInt32 (0);

			var fields = DBRecord.getReflectionInfo(this.GetType());

			foreach (var field in fields) {
				int ordinal = cmd.GetOrdinal (field.Key);
				var value = cmd.GetValue (ordinal);
				if (value == DBNull.Value)
					value = null;
				field.Value.SetValue (this, value);
			}
		}

		/// <summary>
		/// A default Save implementation using reflection
		/// </summary>
		/// <param name="connection"></param>
		protected void SaveInternal (IDB db)
		{
			string sql;
			string [] fields = Fields;

			using (IDbCommand cmd = db.CreateCommand ()) {
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

				CreateParameter (cmd, "id", id);
				foreach (string field in fields) {
					FieldInfo fieldinfo = GetField (GetType (), field);
					object value = fieldinfo.GetValue (this);
					if (value == null && fieldinfo.FieldType == typeof (string)) {
						value = string.Empty;
					} else if (value is DateTime && ((DateTime) value) == DatabaseNow) {
						sql = sql.Replace ("@" + field, "now () AT TIME ZONE 'UTC'");
						continue;
					}
					CreateParameter (cmd, field, value);
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

		private static FieldInfo GetField (Type t, string field)
		{
			FieldInfo result;

			result = t.GetField (field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (result == null)
				result = t.GetField ("_" + field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			return result;
		}

		/// <summary>
		/// A default Delete implementation
		/// </summary>
		/// <param name="connection"></param>
		public static void DeleteInternal (IDB db, int id, string Table)
		{
			if (id <= 0)
				throw new Exception (Table + " doesn't have an id.");

			using (IDbCommand cmd = db.CreateCommand ()) {
				cmd.CommandText = "DELETE FROM " + Table + " WHERE id = " + id.ToString ();
				cmd.ExecuteNonQuery ();
			}
		}

		public void DeleteInternal (IDB db)
		{
			DeleteInternal (db, id, Table);
		}

		public static void CreateParameter (IDbCommand cmd, string name, object value)
		{
			IDbDataParameter result = cmd.CreateParameter ();
			result.ParameterName = name;
			result.Value = value;
			cmd.Parameters.Add (result);
		}

		private static Dictionary<string, FieldInfo> getReflectionInfo(Type typ) {
			return fieldsCache.GetOrAdd (typ, delegate(Type t) {
				var fields = new Dictionary<string, FieldInfo> ();

				// The Fields property is an instance property; create a dummy object to retrieve it from.
				var dummy = t.GetConstructor (Type.EmptyTypes).Invoke (new object[] { });
				var fieldNames = t.GetProperty ("Fields").GetValue (dummy, new object[] { }) as string[];

				foreach (var fieldName in fieldNames)
					fields [fieldName] = GetField (t, fieldName);

				return fields;
			});
		}

		public static List<T> LoadMany<T>(IDataReader reader) where T : DBRecord, new() {
			var t = typeof(T);

			// Get FieldInfos from cache (or generate them. cache because reflection is slow.)
			Dictionary<string, FieldInfo> fields = getReflectionInfo(t);

			// Get ordinals for fields
			var ordinals = new Dictionary<string, int> ();
			foreach (var field in fields)
				ordinals [field.Key] = reader.GetOrdinal (field.Key);

			// Read data
			var results = new List<T> ();
			while (reader.Read ()) {
				T obj = new T ();
				obj.id = reader.GetInt32 (0);
				foreach (var field in fields) {
					var value = reader.GetValue (ordinals [field.Key]);
					if (value == DBNull.Value)
						value = null;
					field.Value.SetValue (obj, value);
				}
				results.Add (obj);
			}
			return results;
		}
	}
}