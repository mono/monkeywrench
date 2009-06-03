/*
 * GetLaneForEditResponse.cs
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

using MonkeyWrench;

namespace MonkeyWrench.DataClasses.Logic
{
	public class GetLaneForEditResponse : WebServiceResponse
	{
		public DBLane Lane;
		public List<DBLanefile> Files;
		public List<DBLanefile> ExistingFiles;
		public List<DBCommand> Commands;
		public List<DBHostLaneView> HostLaneViews;
		public List<DBHost> Hosts;
		public List<DBLane> Lanes;
		public List<DBLaneDependency> Dependencies;
		public List<DBLaneDeletionDirectiveView> LaneDeletionDirectives;
		public List<DBFileDeletionDirective> FileDeletionDirectives;
		public List<DBEnvironmentVariable> Variables;
	}
}
