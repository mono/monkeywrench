/*
 * GetHostStatusResponse.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (rolf@xamarin.com)
 *   
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonkeyWrench.DataClasses.Logic {
	public class GetHostStatusResponse : WebServiceResponse {
		public List<DBHostStatusView> HostStatus;
	}
}

