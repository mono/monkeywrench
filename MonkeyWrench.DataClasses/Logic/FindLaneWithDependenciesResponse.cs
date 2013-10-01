/*
 * FindLaneWithDependenciesResponse.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (rolf@xamarin.com)
 *   
 * Copyright 2013 Xamarin Inc. (http://www.xamarin.com)
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
	public class FindLaneWithDependenciesResponse : WebServiceResponse
	{
		public DBLane lane;
		public List<DBLane> dependencies;
	}
}
