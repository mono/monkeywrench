/*
 * BuildBotStatus.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2010 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MonkeyWrench.DataClasses.Logic
{
	public class BuildBotStatus
	{
		public string Host;
		public string AssemblyVersion;
		public string AssemblyDescription;

		public void FillInAssemblyAttributes ()
		{
			AssemblyDescriptionAttribute ad;
			object [] attribs;

			attribs = typeof (Configuration).Assembly.GetCustomAttributes (typeof (AssemblyDescriptionAttribute), false);
			if (attribs != null && attribs.Length > 0) {
				ad = (AssemblyDescriptionAttribute) attribs [0];
				AssemblyDescription = ad.Description;
			}
			AssemblyVersion = typeof (Configuration).Assembly.GetName ().Version.ToString ();
		}
	}
}
