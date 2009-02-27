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
using System.IO;

namespace Builder
{
	class SqlToCSharp
	{
		static Dictionary<string, string> col_type_mapping = new Dictionary<string, string> ();

		public static void Main (string [] args)
		{
			Parse (new StreamReader (args [0]));
		}

		static void WriteHeader (StreamWriter writer)
		{
			writer.WriteLine (
@"/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

/*
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
					ignored = true;
					continue;
				} else if (line.Trim () == "-- unignore generator --") {
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

		static void ParseView (StreamReader reader, string view)
		{
			string line, name, column, mtype;
			string word;
			string tmp;
			StringBuilder builder = new StringBuilder ();
			List<string> columns = new List<string> ();

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

			if ((word = ReadWord (ref view)) != "SELECT") {
				Console.WriteLine ("Invalid view, expected 'SELECT', got '{0}'", word);
				return;
			}

			do {
				string type;

				column = ReadWord (ref view);
				type = column;

				if (string.IsNullOrEmpty (column) || column == "FROM")
					break;

				tmp = ReadWord (ref view);
				if (!string.IsNullOrEmpty (tmp) && tmp.ToLowerInvariant () == "as") {
					column = ReadWord (ref view);
					tmp = ReadWord (ref view);
				}

				Console.WriteLine ("Found column: {0}, tmp: {1}", column, tmp);
				columns.Add (column + ":" + type);
			} while (tmp == ",");

			using (StreamWriter writer = new StreamWriter (string.Format ("DB{0}.generated.cs", name))) {
				WriteHeader (writer);
				writer.WriteLine (@"
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;

#pragma warning disable 649

namespace Builder
{{
	public partial class DB{0} : DBView
	{{", name);
				for (int c = 0; c < columns.Count; c++) {
					string type;
					string [] args = columns [c].Split (':');

					type = args [1];
					column = args [0];

					if (column.EndsWith (".id")) {
						columns [c] = "id";
						continue;
					} else if (!col_type_mapping.ContainsKey (type)) {
						Console.WriteLine ("Couldn't find type for column: '{0}' (type: {1})", column, type);
						columns [c] = column;
						continue;
					} else {
						mtype = col_type_mapping [type];
						Console.WriteLine ("Found mtype {0} for column {1} type {2}", mtype, column, type);
						column = column.Substring (column.IndexOf ('.') + 1);
						columns [c] = column;
					}
					writer.WriteLine ("\t\tprivate {0} _{1};", mtype, column);
					builder.AppendFormat ("\t\tpublic {0} @{1} {{ get {{ return _{2}; }} }}\n", mtype, column, column);
				}
				writer.WriteLine ("");
				writer.WriteLine (builder.ToString ());
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
        ", string.Join ("\", \"", columns.ToArray ()));

				writer.WriteLine (@"
		public DB{0} (IDataReader reader) : base (reader)
		{{
		}}
", name);
				writer.WriteLine (@"
	}
}
");
			}
		}

		static void ParseTable (StreamReader reader, string table)
		{
			Console.WriteLine ("Parsing table: '{0}'", table);
			string line, column, type, mtype;
			StringBuilder builder = new StringBuilder ();
			List<string> columns = new List<string> ();

			if (!File.Exists (string.Format ("DB{0}.cs", table))) {
				using (StreamWriter writer = new StreamWriter (string.Format ("DB{0}.cs", table))) {
					WriteHeader (writer);
					writer.WriteLine (@"
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;

#pragma warning disable 649

namespace Builder
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

			using (StreamWriter writer = new StreamWriter (string.Format ("DB{0}.generated.cs", table))) {
				WriteHeader (writer);
				writer.WriteLine (@"
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;

#pragma warning disable 649

namespace Builder
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
