/*
 * GetBuildInfoResponse.cs
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
	public class GetBuildInfoResponse : WebServiceResponse
	{
		/// <summary>
		/// The host executing the request.
		/// </summary>
		public DBHost Host;

		/// <summary>
		/// A list of work to do. Each item is a list of entries which can be executed simultaneously.
		/// </summary>
		public List<List<BuildInfoEntry>> Work;

		/// <summary>
		/// A list of the master hosts for this host
		/// </summary>
		public List<DBMasterHost> MasterHosts;
	}
}
