using System;
using System.Web;
using DotNetOpenAuth.AspNet;
using JohnnyCode.GitHubOAuth2;

namespace MonkeyWrench.Web.UI
{
	public static class GitHubAuthenticationHelper
	{

		public static GitHubOAuth2Client ms { get; set; }

		public static string GetAccessToken(this AuthenticationResult auth)
		{
			if (auth != null)
			{
				string accessToken = "";
				if (auth.ExtraData.TryGetValue("accesstoken", out accessToken))
				{
					return accessToken;
				}
			}
			return String.Empty;
		}

		public static string GetGitHubLogin(this AuthenticationResult auth)
		{
			if (auth != null)
			{
				string login = "";
				if (auth.ExtraData.TryGetValue("login", out login))
				{
					return login;
				}
			}
			return String.Empty;
		}

		public static dynamic GetUserOrgs(string accessToken)
		{
			return ms.GetOrganizations(accessToken);
		}

		public static dynamic GetUserTeams(string accessToken)
		{
			return ms.GetUserTeams(accessToken);
		}

		public static AuthenticationResult VerifyAuthentication()
		{
			var manager = new OpenAuthSecurityManager(new HttpContextWrapper(HttpContext.Current),
				ms, OAuthDataProvider.Instance);
			return manager.VerifyAuthentication(Configuration.GitHubOauthRedirect);
		}

		public static void Authenticate()
		{
			ms = new GitHubOAuth2Client(Configuration.GitHubOauthClientId, Configuration.GitHubOauthClientSecret, "GitHubLogin", "user:email,read:org");
			new OpenAuthSecurityManager(new HttpContextWrapper(HttpContext.Current),
				ms, OAuthDataProvider.Instance).RequestAuthentication(Configuration.GitHubOauthRedirect);
		}
	}
}

