﻿/*
 * DBWork.cs
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
using System.Data.Common;
using System.IO;

namespace MonkeyWrench.DataClasses
{
	public partial class DBWork : DBRecord
	{
		public const string TableName = "Work";

		public DBWork ()
		{
		}

		public DBWork (IDataReader reader)
			: base (reader)
		{
		}

		public DBState State
		{
			get { return (DBState) state; }
			set { state = (int) value; }
		}

		public void CalculateSummary (TextReader reader)
		{
			string line;
			string id, file;
			char [] tr = new char [] { '(', ')', ' ' };
			List<string> failures = new List<string> ();
			List<string> test_runs = new List<string> ();

			try {
				while ((line = reader.ReadLine ()) != null) {
					if (line.StartsWith ("Failed:", StringComparison.Ordinal)) {
						line = line.Replace ("Failed:", "");
						int end = line.IndexOf ("--", StringComparison.Ordinal);
						if (end >= 0) {
							line = line.Substring (0, end).Trim ();
						} else {
							line = line.Trim ();
						}
						int space = line.IndexOf (' ');
						if (space > 0) {
							id = line.Substring (space).Trim (tr);
							line = line.Substring (0, space).Trim ();
							file = Path.GetFileName (line);
						} else {
							id = "-1";
							file = "<unknown>";
						}
						failures.Add (file + " " + id);
					} else if (line.StartsWith ("Tests run:", StringComparison.Ordinal)) {
						var test_run = line;
						if (failures.Count > 0) {
							test_run += " (Failures: ";
							for (int i = 0; i < failures.Count; i++) {
								test_run += failures [i];
								if (i < failures.Count - 1)
									test_run += ", ";
							}
							test_run += ")";
							failures.Clear ();
						}
						test_runs.Add (test_run);
					} else if (line.StartsWith ("  Test Count:", StringComparison.Ordinal)) {
						test_runs.Add (line.TrimStart ());
					}
				}
				if (test_runs.Count == 0) {
					summary = "-";
				} else {
					summary = string.Join ("<br/>", test_runs.ToArray ());
				}
			} catch (Exception ex) {
				summary = ex.Message;
			}
		}
	}
}