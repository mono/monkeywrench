using DotNetOpenAuth.AspNet;
using DotNetOpenAuth.AspNet.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DotNetOpenAuth.GoogleOAuth2;
using MonkeyWrench.DataClasses.Logic;

namespace MonkeyWrench.Web.UI
{
	public static class AuthenticationHelper
	{
		private const string _email = "xamarin.com";

		public static bool IsAuthenticated()
		{
			var auth = Global.ReadFromSession<AuthenticationResult>("Auth");
			if (auth != null && auth.IsSuccessful)
			{
				string baseEmail = "";

				// And by check, I mean check in the lamest way possible... :(
				if (auth.ExtraData.TryGetValue ("hd", out baseEmail)) {
					if (!baseEmail.Equals (_email)) {
						return false;
					}
				} else {
					return false;
				}

				return true;                                          
			}
			return false;            
		}

		public static string GetEmail()
		{
			var auth = Global.ReadFromSession<AuthenticationResult>("Auth");
			if (auth != null)            
			{
				string baseEmail = "";
				if (auth.ExtraData.TryGetValue ("email", out baseEmail)) {
					return baseEmail;
				}
			}
			return String.Empty;
		}

		public static AuthenticationResult VerifyAuthentication()
		{
			var ms = new GoogleOAuth2Client(Configuration.OauthClientId, Configuration.OauthClientSecret);
			var manager = new OpenAuthSecurityManager(new HttpContextWrapper(HttpContext.Current), 
				ms, OAuthDataProvider.Instance);
			GoogleOAuth2Client.RewriteRequest();
			var result = manager.VerifyAuthentication(Configuration.OauthRedirect);

			if (result != null)
			{
				Global.SaveInSession("Auth", result);
			}

			return result;
		}

		public static void Authenticate()
		{
			string[] scopes = {
				"https://www.googleapis.com/auth/userinfo.email",
				"https://www.googleapis.com/auth/userinfo.profile",
			};
			var ms = new GoogleOAuth2Client(Configuration.OauthClientId, Configuration.OauthClientSecret, scopes);
			new OpenAuthSecurityManager(new HttpContextWrapper(HttpContext.Current),
				ms, OAuthDataProvider.Instance).RequestAuthentication(Configuration.OauthRedirect);
		}

		public static void Unauthenticate()
		{    
			Global.ClearFromSession("Auth");            
		}
	}
}

