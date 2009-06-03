/*
 * GetHostsForEditResponse.cs
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
	public class GetHostForEditResponse: WebServiceResponse
	{
		public DBHost Host;
		public List<DBHostLaneView> HostLaneViews;
		public List<DBLane> Lanes;
		public List<DBEnvironmentVariable> Variables;
		public List<DBHost> MasterHosts;
		public List<DBHost> SlaveHosts;
		public List<DBHost> Hosts;
	}
}
