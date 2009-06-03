/*
 * GetViewLaneDataResponse.cs
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
	public class GetViewLaneDataResponse : WebServiceResponse
	{
		public DateTime Now;
		public DBLane Lane;
		public DBHost Host;
		public DBHost WorkHost;
		public DBRevision Revision;
		public DBRevisionWork RevisionWork;
		public List<DBWorkView2> WorkViews;
		public List<List<DBWorkFileView>> WorkFileViews;
	}
}
