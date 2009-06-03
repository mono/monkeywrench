/*
 * LoginResponse.cs
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
	public class LoginResponse : WebServiceResponse
	{
		/// <summary>
		/// The name of the logged in user
		/// </summary>
		public string User;

		/// <summary>
		/// The cookie of the logged in user
		/// </summary>
		public string Cookie;

		/// <summary>
		/// The fullname of the logged in user
		/// </summary>
		public string FullName;

		/// <summary>
		/// The ID of the logged in user
		/// </summary>
		public int ID;
	}
}
