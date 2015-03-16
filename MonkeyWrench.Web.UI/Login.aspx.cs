/*
 * Login.aspx.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (rolf@xamarin.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 * Copyright 2012, 2015 Xamarin Inc. (http://www.xamarin.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class Login : System.Web.UI.Page
{
	static string GoogleOAuth2AuthorizationEndpoint;
	static string GoogleOAuth2TokenEndpoint;

	string GoogleOAuth2RedirectUrl {
		get {
			return Configuration.WebSiteUrl + "Login.aspx?action=google-auth-callback";
		}
	}

	private new Master Master
	{
		get { return base.Master as Master; }
	}

	class DiscoveryDocument {
		public string issuer;
		public string authorization_endpoint;
		public string token_endpoint;
	}

	class TokenExchangeResponse {
		public string access_token;
		public string token_type;
		public int expires_in;
		public string id_token;
	}

	class IdTokenResponse {
		public string iss;
		public string sub;
		public string azp;
		public string email;
		public string at_hash;
		public bool email_verified;
		public string aud;
		public string hd;
		public string iat;
		public string exp;
	}

	static void LoadGoogleOAuth2DiscoveryDocument ()
	{
		if (!string.IsNullOrEmpty (Configuration.GoogleOAuth2ClientId)) {
			// https://accounts.google.com/.well-known/openid-configuration
			try {
				var wc = new WebClient ();
				var doc = wc.DownloadString ("https://accounts.google.com/.well-known/openid-configuration");
				var discoveryDoc = Newtonsoft.Json.JsonConvert.DeserializeObject<DiscoveryDocument> (doc);

				if (discoveryDoc.issuer != "accounts.google.com") {
					Logger.Log ("Invalid discovery doc issuer: {0}", discoveryDoc.issuer);
					return;
				}
				GoogleOAuth2AuthorizationEndpoint = discoveryDoc.authorization_endpoint;
				GoogleOAuth2TokenEndpoint = discoveryDoc.token_endpoint;
				Logger.Log ("Loaded Google OAuth2 Discovery Document. Issuer: {0} Authorization endpoint: {1} Token endpoint: {2}", discoveryDoc.issuer, discoveryDoc.authorization_endpoint, discoveryDoc.token_endpoint);
			} catch (Exception ex) {
				Logger.Log ("Failed to load Google OAuth2 Discovery Document: {0}", ex);
				GoogleOAuth2AuthorizationEndpoint = string.Empty;
				GoogleOAuth2TokenEndpoint = string.Empty;
			}
		}
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		LoadGoogleOAuth2DiscoveryDocument ();

		string action = Request ["action"];
		string referrer = Request ["referrer"];

		if (!string.IsNullOrEmpty (referrer))
			txtReferrer.Value = referrer;

		if (!this.IsPostBack) {
			if (Request.UrlReferrer != null && string.IsNullOrEmpty (txtReferrer.Value))
				txtReferrer.Value = Request.UrlReferrer.AbsoluteUri;
		}
		if (!Configuration.AllowPasswordLogin) {
			cmdLogin.Visible = Configuration.AllowPasswordLogin;
			txtPassword.Visible = Configuration.AllowPasswordLogin;
			txtUser.Visible = Configuration.AllowPasswordLogin;
			lblUser.Visible = Configuration.AllowPasswordLogin;
			lblPassword.Visible = Configuration.AllowPasswordLogin;
		}

		// can't refer back to itself
		if (txtReferrer.Value.Contains ("Login.aspx"))
			txtReferrer.Value = "index.aspx";

		if (action == "logout") {
			if (Request.Cookies ["cookie"] != null) {
				Master.WebService.Logout (Master.WebServiceLogin);
				Response.Cookies.Add (new HttpCookie ("cookie", ""));
				Response.Cookies ["cookie"].Expires = DateTime.Now.AddYears (-20);
				Response.Cookies.Add (new HttpCookie ("user", ""));
				Response.Cookies ["user"].Expires = DateTime.Now.AddYears (-20);
				Response.Cookies.Add (new HttpCookie ("roles", ""));
				Response.Cookies ["roles"].Expires = DateTime.Now.AddYears (-20);
			}
			Response.Redirect (txtReferrer.Value, false);
			return;
		} else if (action == "google-auth-callback") {
			// verify state
			var user_state = Request ["state"];
			var session_state = Session ["google-auth-state"] as string;
			Console.WriteLine ("user state: {0} session state: {1} match: {2}", user_state, session_state, user_state == session_state);
			if (user_state != session_state) {
				Logger.Log ("User state {0} does not match session state {1}", user_state, session_state);
				lblMessageOpenId.Text = "Authentication error";
				return;
			}

			// get the access token 
			var str = string.Format ("code={0}&client_id={1}&client_secret={2}&redirect_uri={3}&grant_type=authorization_code",
				HttpUtility.UrlEncode (Request ["code"]),
				HttpUtility.UrlEncode (Configuration.GoogleOAuth2ClientId),
				HttpUtility.UrlEncode (Configuration.GoogleOAuth2ClientSecret),
				HttpUtility.UrlEncode (GoogleOAuth2RedirectUrl));
			var byteArray = Encoding.UTF8.GetBytes (str);
			var webRequest = (HttpWebRequest) WebRequest.Create (GoogleOAuth2TokenEndpoint);
			webRequest.Method = "POST";
			webRequest.ContentType = "application/x-www-form-urlencoded";
			webRequest.ContentLength = byteArray.Length;
			using (var postStream = webRequest.GetRequestStream ()) {
				postStream.Write (byteArray, 0, byteArray.Length);
			}

			var response = webRequest.GetResponse ();
			using (var postStream = response.GetResponseStream ()) {
				using (var reader = new StreamReader (postStream)) {
					string responseFromServer = reader.ReadToEnd ();
					Console.WriteLine (responseFromServer);
					var rsp = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenExchangeResponse> (responseFromServer);
					ValidateGoogleOAuthLogin (Decode (rsp.id_token));
				}
			}
			return;
		}
		
		var auto_google_redirect = false;
		var auto = Request ["auto-redirect-openid"];
		if (!string.IsNullOrEmpty (auto) && auto.ToUpperInvariant () == "TRUE")
			auto_google_redirect = true;

		if (!Configuration.AllowPasswordLogin && string.IsNullOrEmpty (action) && Configuration.AllowAnonymousAccess)
			auto_google_redirect = true;

		if (auto_google_redirect)
			Response.Redirect (GetGoogleOAuthLoginUrl (), false);
	}


	static IdTokenResponse Decode (string token, string key = null, bool verify = false)
	{
		var parts = token.Split ('.');
		var payload = parts [1];

		var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode (payload));
		Console.WriteLine ("Payload: {0}", payloadJson);
		return Newtonsoft.Json.JsonConvert.DeserializeObject<IdTokenResponse> (payloadJson);
	}

	// from JWT spec
	static byte[] Base64UrlDecode(string input)
	{
		var output = input;
		output = output.Replace('-', '+'); // 62nd char of encoding
		output = output.Replace('_', '/'); // 63rd char of encoding
		switch (output.Length % 4) // Pad with trailing '='s
		{
		case 0: break; // No pad chars in this case
		case 2: output += "=="; break; // Two pad chars
		case 3: output += "="; break; // One pad char
		default: throw new System.Exception("Illegal base64url string!");
		}
		var converted = Convert.FromBase64String(output); // Standard base64 decoder
		return converted;
	}

	protected void cmdLogin_Click (object sender, EventArgs e)
	{
		Master.ClearLogin ();

		if (!Authentication.Login (txtUser.Text, txtPassword.Text, Request, Response)) {
			lblMessage.Text = "Could not log in";
			txtPassword.Text = "";
		} else {
			Response.Redirect (txtReferrer.Value, false);
		}
	}

	void ValidateGoogleOAuthLogin (IdTokenResponse id_token)
	{
		Logger.Log ("Login.ValidateGoogleOAuthLogin email: {0} email_verified: {1} hd: {2}", id_token.email, id_token.email_verified, id_token.hd);
		if (id_token.hd != Configuration.GoogleOAuth2HostedDomain) {
			Logger.Log ("Invalid hd");
			lblMessageOpenId.Text = "This email is not authorized.";
			return;
		} else if (!id_token.email_verified) {
			Logger.Log ("Email not verified");
			lblMessageOpenId.Text = "This email is not verified.";
			return;
		}
		var login = new WebServiceLogin ();
		login.Password = Configuration.WebServicePassword;
		login.User = Configuration.Host;
		var response = Utils.LocalWebService.LoginOpenId (login, id_token.email, Utilities.GetExternalIP (Request));
		if (response.Exception != null) {
			lblMessageOpenId.Text = response.Exception.Message;
		} else {
			Authentication.SetCookies (Response, response);
			Console.WriteLine ("Redirecting to: {0}", txtReferrer.Value);
			Response.Redirect (txtReferrer.Value, false);
			return;
		}
	}

	public string GetGoogleOAuthLoginUrl ()
	{
		LoadGoogleOAuth2DiscoveryDocument ();

		string URL = GoogleOAuth2AuthorizationEndpoint +
			"?response_type=code" +
			"&scope=openid%20email" +
			"";

		URL += "&client_id=" + HttpUtility.UrlEncode (Configuration.GoogleOAuth2ClientId);
		URL += "&redirect_uri=" + HttpUtility.UrlEncode (GoogleOAuth2RedirectUrl);

		if (!string.IsNullOrEmpty (Configuration.GoogleOAuth2HostedDomain))
			URL += "&hd=" + HttpUtility.UrlEncode (Configuration.GoogleOAuth2HostedDomain);

		if (!string.IsNullOrEmpty (Request ["user"]))
			URL += "&login_hint=" + HttpUtility.UrlEncode (Request ["user"]);

		var state = CreateState ();
		URL += "&state=" + state;

		Logger.Log ("Request google oauth2 login: {0}", URL);
		return URL;
	}

	string CreateState ()
	{
		var rnd = new System.Security.Cryptography.RNGCryptoServiceProvider ();
		var bytes = new byte [48];
		rnd.GetBytes (bytes);
		var sb = new StringBuilder ();
		for (int i = 0; i < bytes.Length; i++)
			sb.Append (bytes [i].ToString ("X"));
		Context.Session.Add ("google-auth-state", sb.ToString ());
		return sb.ToString ();
	}
}

