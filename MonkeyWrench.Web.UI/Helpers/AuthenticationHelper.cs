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
		private const string _id = "515904261941-okror0rpf0f79ljdubj8lckaakae4f8a.apps.googleusercontent.com";
		private const string _clientKey = "p8is5rKSOs803d6gFqUVF_c3";
		private const string _redirect = "http://localhost:8123/Login.aspx";
		private const string _email = "xamarin.com";

		public static bool IsAuthenticated()
		{
			var auth = Global.ReadFromSession<AuthenticationResult>("Auth");
			if (auth != null && auth.IsSuccessful)
			{
				// Check if they are actually a Xamarin Employee...
				foreach (var thing in auth.ExtraData) {
					Console.WriteLine (thing.Key + ":" + thing.Value);
				}
				string foo = "";

				// And by check, I mean check in the lamest way possible... :(
				if (auth.ExtraData.TryGetValue ("hd", out foo)) {
					if (!foo.Equals (_email)) {
						return false;
					}
				} else {
					return false;
				}
				Console.WriteLine ("Ahhh, A xamarin employee!");


				return true;                                          
			}
			return false;            
		}

		public static string GetEmail()
		{
			var auth = Global.ReadFromSession<AuthenticationResult>("Auth");
			if (auth != null)            
			{
				string foo = "";

				// And by check, I mean check in the lamest way possible... :(
				if (auth.ExtraData.TryGetValue ("email", out foo)) {
					return foo;
				}
			}
			return String.Empty;
		}

		public static AuthenticationResult VerifyAuthentication()
		{
			var ms = new GoogleOAuth2Client(_id, _clientKey);
			var manager = new OpenAuthSecurityManager(new HttpContextWrapper(HttpContext.Current), 
				ms, OAuthDataProvider.Instance);
			GoogleOAuth2Client.RewriteRequest();
			var result = manager.VerifyAuthentication(_redirect);

			if (result != null)
			{
				Console.WriteLine ("We're authed! weee.");
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
			var ms = new GoogleOAuth2Client(_id, _clientKey, scopes);
			new OpenAuthSecurityManager(new HttpContextWrapper(HttpContext.Current),
				ms, OAuthDataProvider.Instance).RequestAuthentication(_redirect);
		}

		public static void Unauthenticate()
		{    
			Global.ClearFromSession("Auth");            
		}
	}
}

