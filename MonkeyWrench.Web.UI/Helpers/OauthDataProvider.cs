using System;
﻿using DotNetOpenAuth.AspNet;

namespace MonkeyWrench.Web.UI
{
	public class OAuthDataProvider : IOpenAuthDataProvider
	{
		public readonly static IOpenAuthDataProvider Instance = new OAuthDataProvider();

		public string GetUserNameFromOpenAuth(string openAuthProvider, string openAuthId)
		{
			return openAuthId;
		}
	}
}
