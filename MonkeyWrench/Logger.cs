/*
 * Logger.cs
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
using System.Text;
using System.IO;
using System.Diagnostics;

namespace MonkeyWrench
{
	public class Logger
	{
		private readonly static int ProcessID = Process.GetCurrentProcess ().Id;

		static string FormatLog (string format, params object [] args)
		{
			string message;
			string [] lines;
			string timestamp = DateTime.Now.ToUniversalTime ().ToString ("yyyy/MM/dd HH:mm:ss.fffff UTC");

			message = string.Format (format, args);
			lines = message.Split (new char [] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < lines.Length; i++) {
				lines [i] = string.Concat ("[", ProcessID.ToString (), " - ", timestamp, "] ", lines [i]);
			}
			message = string.Join ("\n", lines);
			return message + "\n";
		}

		public static void Log (string format, params object [] args)
		{
			string message;

			try {
				message = FormatLog (format, args);
				if (string.IsNullOrEmpty (Configuration.LogFile)) {
					Console.Write (message);
				} else {
					using (FileStream fs = new FileStream (Configuration.LogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) {
						using (StreamWriter st = new StreamWriter (fs)) {
							st.Write (message);
						}
					}
				}
			} catch (Exception ex) {
				Console.WriteLine (FormatLog ("Builder.Logger: An exception occurred while logging: {0}", ex.ToString ()));
				throw;
			}
		}
	}
}
