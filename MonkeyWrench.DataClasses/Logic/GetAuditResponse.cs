/*
 * GetAuditResponse.cs
 *
 * Authors:
 *   Matt Sylvia (matthew.sylvia@xamarin.com)
 *   
 * Copyright 2016 Xamarin, Inc. (http://www.xamarin.com)
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
	public class GetAuditResponse : WebServiceResponse
	{
		public int Count;
		public List<DBAudit> AuditEntries;
	}
}

