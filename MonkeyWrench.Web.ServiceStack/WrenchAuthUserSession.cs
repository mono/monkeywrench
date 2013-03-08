using System;
using System.Collections.Generic;
using System.Net.Mail;

using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Authentication.OpenId;
using ServiceStack.ServiceInterface;

namespace MonkeyWrench.Web.ServiceStack
{
	public class WrenchAuthUserSession : AuthUserSession
	{
		public override void OnAuthenticated (IServiceBase authService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo)
		{
			base.OnAuthenticated (authService, session, tokens, authInfo);
			try {
				var mail = new MailAddress (session.Email);
				// We use a very simple authentification scheme for now solely based on a @xamarin.com email address
				if (mail.Host == "xamarin.com") {
					Roles.Add (RoleNames.Admin);
					Permissions.AddRange (new [] { "CanAccess", "CanModify" });
				}
			} catch {
				// Log
			}
		}
	}
}

