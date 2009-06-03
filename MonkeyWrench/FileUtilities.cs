/*
 * FileUtilities.cs
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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MonkeyWrench
{
	public static class FileUtilities
	{
		public static void GZUncompress (string filename)
		{
			if (!filename.EndsWith (".gz")) {
				File.Move (filename, filename + ".gz");
				filename = filename + ".gz";
			}

			// Uncompress it
			using (Process p = new Process ()) {
				p.StartInfo.FileName = "gunzip";
				p.StartInfo.Arguments = "--force " + filename; // --force is needed since Path.GetTempFileName creates the file
				p.Start ();
				if (!p.WaitForExit (1000 * 60 /* 1 minute */ )) {
					Logger.Log ("GZUncompress: gunzip didn't finish in one minute, killing it.");
					p.Kill ();
				}
			}
		}

	}
}
