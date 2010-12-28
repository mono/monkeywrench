/*
 * WebServiceResponse.cs
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
	public class WebServiceResponse
	{
		/// <summary>
		/// The role the logged in user has.
		/// May be null if the user has no roles.
		/// </summary>
		public string [] UserRoles;

		/// <summary>
		/// The name of the logged in user.
		/// Will be null if there is no logged in user.
		/// </summary>
		public string UserName;

		/// <summary>
		/// Any exceptions thrown
		/// </summary>
		public WebServiceException Exception;

		/// <summary>
		/// Checks if a user in in a specific role
		/// </summary>
		/// <param name="role"></param>
		/// <returns></returns>
		public bool IsInRole (string role)
		{
			if (UserRoles == null)
				return false;

			return Array.IndexOf<string> (UserRoles, role) >= 0;
		}
	}
}
