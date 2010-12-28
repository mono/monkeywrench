/*
 * FrontPageResponse.cs
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
	public class FrontPageResponse : WebServiceResponse
	{
		public DBLane Lane;
		/// <summary>
		/// A list of the lanes to show.
		/// </summary>
		public List<DBLane> SelectedLanes;
		/// <summary>
		/// All Lanes
		/// </summary>
		public List<DBLane> Lanes;
		/// <summary>
		/// All Hosts
		/// </summary>
		public List<DBHost> Hosts;
		public List<DBHostLane> HostLanes;

		public List<int> RevisionWorkHostLaneRelation;
		public List<List<DBRevisionWorkView2>> RevisionWorkViews;
	}
}
