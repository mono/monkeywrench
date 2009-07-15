/*
 * BuildInfoEntry.cs
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

namespace MonkeyWrench.DataClasses.Logic
{
	public class BuildInfoEntry
	{
		public DBLane Lane;
		public DBHostLane HostLane;
		public DBRevision Revision;
		public List<DBWorkFile> FilesToDownload;
		public List<DBLane> DependentLaneOfFiles; // same # of entries as FilesToDownload, 1-1 correspondence.
		public List<DBLanefile> LaneFiles;
		public DBWork Work;
		public DBCommand Command;
		public List<DBEnvironmentVariable> EnvironmentVariables;
	}
}
