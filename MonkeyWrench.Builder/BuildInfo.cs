/*
 * BuildInfo.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System.Collections.Generic;

using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Builder
{
	internal class BuildInfo
	{
		public int number;
		public DBLane lane;
		public DBHostLane hostlane;
		public DBHost host;
		public DBWork work;
		public DBCommand command;
		public DBRevision revision;
		public List<DBEnvironmentVariable> environment_variables;
		public string BUILDER_DATA_LOG_DIR;
		public string BUILDER_DATA_INSTALL_DIR;
		public string BUILDER_DATA_SOURCE_DIR;
		public string temp_dir;
	}
}
