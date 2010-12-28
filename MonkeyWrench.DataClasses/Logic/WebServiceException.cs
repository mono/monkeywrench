/*
 * WebServiceException.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2010 Novell, Inc. (http://www.novell.com)
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
	public class WebServiceException
	{
		public string Message;
		public string StackTrace;
		public string Type;
		public string AsString;

		public WebServiceException ()
		{
		}

		public WebServiceException (string Message)
		{
			this.Message = Message;
		}

		public WebServiceException (Exception ex)
		{
			Message = ex.Message;
			StackTrace = ex.StackTrace;
			Type = ex.GetType ().FullName;
			AsString = ex.ToString ();
		}
	}
}
