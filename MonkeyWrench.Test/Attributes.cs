/*
 * Attributes.cs
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
using System.Linq;
using System.Text;

namespace MonkeyWrench.Test
{
	public class TestAttribute : System.Attribute {	}
	public class TestSetupAttribute : System.Attribute {}
	public class TestCleanupAttribute : System.Attribute { }
	public class TestFixtureAttribute : System.Attribute { }
	public class AssertException : System.Exception {
		public AssertException (string msg, params object [] args)
			: base (string.Format (msg, args))
		{

		}
	}

	/// <summary>
	/// This class differs from the common unit testing class 'Assert' in that it allows a test to continue (it does not throw an exception) if a check fails.
	/// </summary>
	public static class Check
	{
		public static void AreEqual (int expected, int actual, string message)
		{
			if (expected != actual)
				Runner.Assertions.Add (new AssertException ("Expected '{0}', got '{1}': {2}", expected, actual, message));
		}

		public static void AreEqual (bool expected, bool actual, string message)
		{
			if (expected != actual)
				Runner.Assertions.Add (new AssertException ("Expected '{0}', got '{1}': {2}", expected, actual, message));
		}

		public static void AreEqual (DateTime expected, DateTime actual, string message)
		{
			if (expected != actual)
				Runner.Assertions.Add (new AssertException ("Expected '{0}', got '{1}': {2}", expected, actual, message));
		}

		public static void AreEqual (string expected, string actual, string message)
		{
			if (expected != actual) {
				if (expected.IndexOfAny (new char [] { '\n', '\r' }) >= 0 || actual.IndexOfAny (new char [] { '\n', '\r' }) >= 0) {
					int a_len = actual.Length;
					int e_len = expected.Length;
					int diff_idx = -1;
					for (int i = 0; i < a_len && i < e_len; i++) {
						if (actual [i] != expected [i]) {
							diff_idx = i;
							break;
						}
					}
					if (diff_idx == 1)
						diff_idx = Math.Min (a_len, e_len);

					Runner.Assertions.Add (new AssertException ("Expected {0}'{1}'{0} (length {3}), got {0}'{2}'{0} (length {4}). Diff index: {5} (Expected: '{6}', got: '{7}')", 
						Environment.NewLine, expected, actual, e_len, a_len, diff_idx, e_len >= diff_idx ? ((int) expected [diff_idx]).ToString () : "N/A", a_len >= diff_idx ? ((int) actual [diff_idx]).ToString () : "N/A"));
				} else {
					Runner.Assertions.Add (new AssertException ("Expected '{0}', got '{1}': {2}", expected, actual, message));
				}
			}
		}

		public static void AreNotEqual (DateTime a, DateTime b, string message)
		{
			if (a == b)
				Runner.Assertions.Add (new AssertException ("Got equal values, expected different values ('{0}' vs '{1}', or {3} ticks vs {4} ticks): {2}", a, b, message, a.Ticks, b.Ticks));
		}

		public static void IsNullOrEmpty (string actual, string message)
		{
			if (!string.IsNullOrEmpty (actual))
				Runner.Assertions.Add (new AssertException ("Expected a null or empty string value, got: '{0}'", message));
		}

		public static void IsNull (object obj, string message)
		{
			if (obj != null)
				Runner.Assertions.Add (new AssertException ("Expected null, got '{0}': {1}", obj, message));
		}

		public static void IsDBNull (object obj, string message)
		{
			if (!(obj is DBNull))
				Runner.Assertions.Add (new AssertException ("Expected DBNull, got '{0}': {1}", obj, message));

		}
	}
}
