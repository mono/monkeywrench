/*
 * WebServiceLogin.cs
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
	public class WebServiceLogin
	{
		/// <summary>
		/// The user
		/// </summary>
		public string User;
		/// <summary>
		/// The password (not required if Cookie is specified)
		/// </summary>
		public string Password;
		/// <summary>
		/// The cookie of an already logged in user (not required if Password is specified)
		/// </summary>
		public string Cookie;
		/// <summary>
		/// If the calling ip is impersonating somebody else (web ui does this), it must
		/// provide the ip for the impersonated login (this applies to when either
		/// Password or Cookie is specified)
		/// </summary>
		public string Ip4;
	}
}
