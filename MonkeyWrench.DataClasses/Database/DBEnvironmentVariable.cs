/*
 * DBEnvironmentVariable.cs
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
using System.Collections.Specialized;
using System.Text;
using System.Data;
using System.Data.Common;

namespace MonkeyWrench.DataClasses
{
	public partial class DBEnvironmentVariable : DBRecord
	{
		public const string TableName = "EnvironmentVariable";

		public DBEnvironmentVariable ()
		{
		}

		public DBEnvironmentVariable (IDataReader reader)
			: base (reader)
		{
		}

		public void Evaluate (StringDictionary vars)
		{
			vars [name] = Evaluate (vars, value);
		}

		public static string Evaluate (StringDictionary vars, string var)
		{
			StringBuilder result;
			int start, end;

			if (string.IsNullOrEmpty (var))
				return var;

			start = var.IndexOf ("${");

			if (start == -1)
				return var;

			result = new StringBuilder ();

			while (start != -1) {
				end = var.IndexOf ('}', start + 2);

				if (end == -1) {
					result.Append (var);
					break;
				}

				result.Append (var.Substring (0, start));
				string n = var.Substring (start + 2, end - start - 2);
				result.Append (vars [n]);
				var = var.Substring (end + 1);

				start = var.IndexOf ("${");
			}

			result.Append (var);

			return result.ToString ();
		}
	}
}

