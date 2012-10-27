/*
 * GetViewTableDataResponse.cs
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
	public class GetViewTableDataResponse : WebServiceResponse
	{
		public int Page;
		public int PageSize;
		public int Count;
		public DBLane Lane;
		public DBHost Host;
		public List<DBRevisionWorkView> RevisionWorkViews;
		public bool Enabled;
	}
}

