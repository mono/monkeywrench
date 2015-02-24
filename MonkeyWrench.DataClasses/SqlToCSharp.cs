/*
 * SqlToCSharp.cs: Program to create database classes from sql.
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 * Copyright 2014 Xamarin Inc. (http://www.xamarin.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MonkeyWrench.DataClasses
{
	class SqlToCSharp
	{
		const string OUTPUT_DIR = "Database";
		static Dictionary<string, string> col_type_mapping = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
		static Dictionary<string, bool> col_type_isnull_mapping = new Dictionary<string, bool> (StringComparer.OrdinalIgnoreCase);

		public static void Main (string [] args)
		{
			Parse (new StreamReader (args [0]));
		}

		static void WriteHeader (StreamWriter writer, string filename)
		{
			writer.WriteLine (
@"/*
 * This file has been generated. 
 * If you modify it you'll loose your changes.
 */ 
");

		}

		static string RemoveComment (string line)
		{
			int comment = line.IndexOf ("--");
			if (comment >= 0)
				return line.Substring (0, comment);
			return line;
		}

		static void Parse (StreamReader reader)
		{
			string line;
			bool ignored = false;

			while (null != (line = reader.ReadLine ())) {
				if (line.Trim () == "-- ignore generator --") {
					Console.WriteLine ("Ignore ON");
					ignored = true;
					continue;
				} else if (line.Trim () == "-- unignore generator --") {
					Console.WriteLine ("Ignore OFF");
					ignored = false;
					continue;
				} else if (ignored) {
					continue;
				}

				line = RemoveComment (line);
				if (line.StartsWith ("CREATE TABLE")) {
					ParseTable (reader, line.Replace ("CREATE TABLE", "").Trim (new char [] { ' ', ',', '\t', '(', '"' }));
				} else if (line.StartsWith ("CREATE VIEW")) {
					ParseView (reader, line);
				}
			}
		}

		class ViewColumns
		{
			public string Name; // name as it will appear in the query
			public string Column; // name to look up field in db (Table.Column)
		}

		static void ParseView (StreamReader reader, string view)
		{
			string line, name, column, mtype;
			string word;
			string tmp;
			string sql;
			Dictionary<string, string> table_aliases = new Dictionary<string, string> ();
			StringBuilder builder = new StringBuilder ();
			List<ViewColumns> columns = new List<ViewColumns> ();
			List<string> all_columns = new List<string> ();

			while (null != (line = reader.ReadLine ())) {
				view += " " + line;
				if (line.Contains (";"))
					break;
			}

			if ((word = ReadWord (ref view)) != "CREATE") {
				Console.WriteLine ("Invalid view, expected 'CREATE', got '{0}'", word);
				return;
			}

			if ((word = ReadWord (ref view)) != "VIEW") {
				Console.WriteLine ("Invalid view, expected 'VIEW', got '{0}'", word);
				return;
			}

			name = ReadWord (ref view);

			Console.WriteLine ("Parsing view {0}: {1}", name, view);

			if ((word = ReadWord (ref view)) != "AS") {
				Console.WriteLine ("Invalid view, expected 'AS', got '{0}'", word);
				return;
			}

			sql = view;

			if ((word = ReadWord (ref view)) != "SELECT") {
				Console.WriteLine ("Invalid view, expected 'SELECT', got '{0}'", word);
				return;
			}

			do {
				ViewColumns c = new ViewColumns ();

				column = ReadWord (ref view);

				if (string.IsNullOrEmpty (column) || column == "FROM")
					break;

				c.Column = column;

				tmp = ReadWord (ref view);
				if (!string.IsNullOrEmpty (tmp) && tmp.ToLowerInvariant () == "as") {
					column = ReadWord (ref view);
					tmp = ReadWord (ref view);
					c.Name = column;
				} else {
					c.Name = column.IndexOf ('.') >= 0 ? column.Substring (column.IndexOf ('.') + 1) : column;
				}

				// Console.WriteLine ("Found column: {0}, name: {1}", c.Column, c.Name);

				columns.Add (c);

				if (tmp != ",")
					column = tmp;
			} while (tmp == ",");

			if (column == "FROM") {
				ReadWord (ref view); // skip one workd
				column = ReadWord (ref view);
				while (column == "INNER" || column == "LEFT" || column == "RIGHT") {
					column = ReadWord (ref view);
					if (column != "JOIN")
						break;

					string table;
					string alias = null;

					table = ReadWord (ref view);
					column = ReadWord (ref view); // ON or AS
					if (column == "AS") {
						alias = ReadWord (ref view);
						column = ReadWord (ref view); // ON
					}
					column = ReadWord (ref view); // left part
					column = ReadWord (ref view); // =
					if (column != "=")
						break;
					column = ReadWord (ref view); // right part

					column = ReadWord (ref view); // 

					if (alias != null) {
						Console.WriteLine ("Added table alias {0}={1}", alias, table);
						table_aliases.Add (alias, table);
					}
				}
			}


			string filename = Path.Combine (OUTPUT_DIR, string.Format ("DB{0}.generated.cs", name));

			using (StreamWriter writer = new StreamWriter (filename)) {
				WriteHeader (writer, Path.GetFileName (filename));

				writer.WriteLine (@"
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;

#pragma warning disable 649

namespace MonkeyWrench.DataClasses
{{
	public partial class DB{0} : DBView
	{{", name);
				for (int c = 0; c < columns.Count; c++) {
					string type;
					bool is_null = false;

					ViewColumns col = columns [c];

					// Console.WriteLine ("Name: {0}, Column: {1},", col.Name, col.Column);

					if (col.Name == "id")
						continue;

					type = col.Column;
					if (!col_type_mapping.ContainsKey (type)) {
						string tbl;
						
						if (col.Column.IndexOf ('.') < 0) {
							Console.WriteLine ("Couldn't find type for column: '{0}' (type: {1})", column, type);
							continue;
						}
						tbl = col.Column.Substring (0, col.Column.IndexOf ('.'));
						if (table_aliases.ContainsKey (tbl)) {
							string tbl_alias = table_aliases [tbl];
							type = tbl_alias + "." + col.Column.Substring (col.Column.IndexOf ('.') + 1);
							if (!col_type_mapping.ContainsKey (type)) {
								Console.WriteLine ("Couldn't find type for column: '{0}' (type: {1} aliased table: {2}={3})", column, type, tbl, tbl_alias);
								continue;
							}
						}
						// Console.WriteLine ("Found column in aliased table.");
					}

					mtype = col_type_mapping [type];
					if (col_type_isnull_mapping.ContainsKey (type)) {
						is_null = col_type_isnull_mapping [type];
						Console.WriteLine ("{0} isnull:  {1}", type, is_null);
					}

					all_columns.Add (col.Name);

					if (is_null && !mtype.EndsWith ("?"))
						mtype += "?";

					Console.WriteLine ("Found mtype {0} for column {1} type {2}", mtype, column, type);

					writer.WriteLine ("\t\tprivate {0} _{1};", mtype, col.Name);
					builder.AppendFormat ("\t\tpublic {0} @{1} {{ get {{ return _{1}; }} set {{ _{1} = value; }} }}\n", mtype, col.Name);
				}
				writer.WriteLine ("");
				writer.WriteLine (builder.ToString ());
				writer.WriteLine ("");
				writer.WriteLine ("\t\tpublic const string SQL = \n@\"{0}\";", sql.Replace ("\"", "\\\"").Replace ("\t\t", "\t    ").Replace ("\t", "\n\t").Replace ("\t    ", "\t\t"));
				writer.WriteLine ("");
				writer.WriteLine (@"
		private static string [] _fields_ = new string [] {{ ""{0}"" }};
		public override string [] Fields
		{{
			get
			{{
				return _fields_;
			}}
		}}
        ", string.Join ("\", \"", all_columns.ToArray ()));

				writer.WriteLine (@"
		public DB{0} ()
		{{
		}}
	
		public DB{0} (IDataReader reader) 
			: base (reader)
		{{
		}}
", name);
				writer.WriteLine (@"
	}
}
");
			}

			Console.WriteLine ("View '{1}' written to {0}", filename, name);
		}

		static void ParseTable (StreamReader reader, string table)
		{
			Console.WriteLine ("Parsing table: '{0}'", table);
			string line, column, type, mtype;
			StringBuilder builder = new StringBuilder ();
			List<string> columns = new List<string> ();
			string filename = Path.Combine (OUTPUT_DIR, string.Format ("DB{0}.cs", table));

			if (!File.Exists (filename)) {
				using (StreamWriter writer = new StreamWriter (filename)) {
					WriteHeader (writer, Path.GetFileName (filename));
					writer.WriteLine (@"
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;

#pragma warning disable 649

namespace MonkeyWrench.DataClasses
{{
	public partial class DB{0} : DBRecord
	{{
		public const string TableName = ""{0}"";

		public DB{0} ()
		{{
		}}
	
		public DB{0} (DB db, int id)
			: base (db, id)
		{{
		}}
	
		public DB{0} (IDataReader reader) 
			: base (reader)
		{{
		}}
	}}
}}
", table);

				}
			}

			string generated_filename = Path.Combine (OUTPUT_DIR, string.Format ("DB{0}.generated.cs", table));
			using (StreamWriter writer = new StreamWriter (generated_filename)) {
				WriteHeader (writer, Path.GetFileName (generated_filename));
				writer.WriteLine (@"
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;

#pragma warning disable 649

namespace MonkeyWrench.DataClasses
{{
	public partial class DB{0} : DBRecord
	{{", table);
				while (null != (line = reader.ReadLine ())) {
					line = line.Trim ();
					if (line == ");")
						break;
					if (line.StartsWith ("UNIQUE"))
						continue;
					if (line.StartsWith ("--"))
						continue;

					column = ReadWord (ref line);
					type = ReadWord (ref line);

					if (string.IsNullOrEmpty (column) || string.IsNullOrEmpty (type))
						continue;

					if (column == "id") {
						col_type_mapping.Add (table + ".id", "int");
						continue;
					}

					bool is_null = false;
					string tmp;
					do {
						tmp = ReadWord (ref line);
						if (tmp == "NULL") {
							is_null = true;
						} else if (tmp == "NOT") {
							tmp = ReadWord (ref line);
							if (tmp == "NULL")
								is_null = false;
						}
					} while (tmp != null);



					switch (type) {
					case "int":
						mtype = "int";
						break;
					case "serial":
						mtype = "int";
						break;
					case "text":
						mtype = "string";
						is_null = false;
						break;
					case "bytea":
						mtype = "byte []";
						is_null = false;
						break;
					case "timestamp":
						mtype = "DateTime";
						break;
					case "boolean":
						mtype = "bool";
						break;
					default:
						Console.WriteLine ("Table: '{0}' unknown type '{1}' for column '{2}'", table, type, column);
						continue;
					}

					Console.WriteLine ("Found field {0}.{1} is_null: {2}", table, column, is_null);

					if (is_null)
						mtype += "?";

					col_type_mapping.Add (table + "." + column, mtype);
					col_type_isnull_mapping.Add (table + "." + column, is_null);

					columns.Add (column);
					writer.WriteLine ("\t\tprivate {0} _{1};", mtype, column);
					builder.AppendFormat ("\t\tpublic {0} @{1} {{ get {{ return _{2}; }} set {{ _{2} = value; }} }}\n", mtype, column, column);
				}
				writer.WriteLine ("");
				writer.WriteLine (builder.ToString ());

				writer.WriteLine (@"
		public override string Table
		{{
			get {{ return ""{0}""; }}
		}}
        ", table);

				writer.WriteLine (@"
		public override string [] Fields
		{{
			get
			{{
				return new string [] {{ ""{0}"" }};
			}}
		}}
        ", string.Join ("\", \"", columns.ToArray ()));

				Console.WriteLine ("Parsing table: '{0}': DONE", table);

				writer.WriteLine (@"
	}
}
");
			}
		}

		static string ReadWord (ref string input)
		{
			int index;
			string result;

			if (input == null)
				return null;

			index = input.IndexOfAny (new char [] { ' ', ',' });
			if (index < 0) {
				input = null;
				return null;
			} else if (index == 0 && input [0] == ',') {
				index = 1;
			}

			result = input.Substring (0, index);
			input = input.Substring (index).TrimStart ();
			return result;
		}
	}
}
