/*
 * Login.aspx.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (rolf@xamarin.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 * Copyright 2012 Xamarin Inc. (http://www.xamarin.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.RelyingParty;
using DotNetOpenAuth.OpenId.Extensions.AttributeExchange;
using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Web;
using System.Threading;
using MonkeyWrench.Web.UI;
using DotNetOpenAuth.AspNet;
using MonkeyWrench.Database;

public partial class Login : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		string action = Request ["action"];
		string referrer = Request ["referrer"] ?? (string) Session ["login_referrer"];
		Session.Remove ("login_referrer");
		bool noOpenIdResponse = false;

		if (!string.IsNullOrEmpty (referrer))
			txtReferrer.Value = referrer;

		if (!this.IsPostBack) {
			if (Request.UrlReferrer != null && string.IsNullOrEmpty (txtReferrer.Value))
				txtReferrer.Value = Request.UrlReferrer.AbsoluteUri;
		}

		// can't refer back to itself
		if (txtReferrer.Value.Contains ("Login.aspx"))
			txtReferrer.Value = "index.aspx";

		cmdLoginOpenId.Visible = !string.IsNullOrEmpty (Configuration.OpenIdProvider);
		cmdLoginOauth.Visible = !string.IsNullOrEmpty (Configuration.OauthClientId);

		if (!Configuration.AllowPasswordLogin) {
			cmdLogin.Visible = Configuration.AllowPasswordLogin;
			txtPassword.Visible = Configuration.AllowPasswordLogin;
			txtUser.Visible = Configuration.AllowPasswordLogin;
			lblUser.Visible = Configuration.AllowPasswordLogin;
			lblPassword.Visible = Configuration.AllowPasswordLogin;
		}

		if (cmdLoginOauth.Visible && Request.QueryString.GetValues ("state") != null) {
			var authResult = AuthenticationHelper.VerifyAuthentication ();
			if (!authResult.IsSuccessful) {
				lblMessageOpenId.Text = "Failed to get user authenication from Google";
				return;
			}

			LoginResponse loginResponse = new LoginResponse ();
			using (DB db = new DB ()) {
				try {
					DBLogin_Extensions.LoginOpenId (db, loginResponse, authResult.GetEmail (), Utilities.GetExternalIP (Request));
				} catch (Exception ex) {
					loginResponse.Exception = new WebServiceException (ex);
				}
			}
			if (loginResponse.Exception != null) {
				lblMessageOpenId.Text = loginResponse.Exception.Message;
			} else {
				Authentication.SetCookies (Response, loginResponse);
				Response.Redirect (txtReferrer.Value, false);
			}
			return;
		}

		if (cmdLoginOpenId.Visible) {
			OpenIdRelyingParty openid = new OpenIdRelyingParty ();
			var oidresponse = openid.GetResponse ();
			if (oidresponse != null) {
				switch (oidresponse.Status) {
				case AuthenticationStatus.Authenticated:
					// This is where you would look for any OpenID extension responses included
					// in the authentication assertion.
					var fetch = oidresponse.GetExtension<FetchResponse> ();
					string email;

					email = fetch.Attributes [WellKnownAttributes.Contact.Email].Values [0];

					WebServiceLogin login = new WebServiceLogin ();
					login.Password = Configuration.WebServicePassword;
					login.User = Configuration.Host;
					var response = Utils.LocalWebService.LoginOpenId (login, email, Utilities.GetExternalIP (Request));
					if (response.Exception != null) {
						lblMessageOpenId.Text = response.Exception.Message;
					} else {
						Authentication.SetCookies (Response, response);
						Response.Redirect (txtReferrer.Value, false);
						return;
					}
					break;
				default:
					lblMessageOpenId.Text = "Could not login using OpenId: " + oidresponse.Status.ToString ();
					break;
				}
			} else {
				noOpenIdResponse = true;
			}
		}

		if (!string.IsNullOrEmpty (action) && action == "logout") {
			if (Request.Cookies ["cookie"] != null) {
				Utils.LocalWebService.Logout (Master.WebServiceLogin);
				Response.Cookies.Add(new HttpCookie ("cookie", ""));
				Response.Cookies ["cookie"].Expires = DateTime.Now.AddYears (-20);
				Response.Cookies.Add (new HttpCookie ("user", ""));
				Response.Cookies ["user"].Expires = DateTime.Now.AddYears (-20);
				Response.Cookies.Add (new HttpCookie ("roles", ""));
				Response.Cookies ["roles"].Expires = DateTime.Now.AddYears (-20);
			}
			Response.Redirect (txtReferrer.Value, false);
			return;
		}
		
		var auto_openid_redirect = false;
		var auto = Request ["auto-redirect-openid"];
		if (!string.IsNullOrEmpty (auto) && auto.ToUpperInvariant () == "TRUE")
			auto_openid_redirect = true;

		if (!Configuration.AllowPasswordLogin && string.IsNullOrEmpty (action) && Configuration.AllowAnonymousAccess && noOpenIdResponse)
			auto_openid_redirect = true;

		if (auto_openid_redirect)
			cmdLoginOpenId_Click (null, null);
	}

	protected void cmdLogin_Click (object sender, EventArgs e)
	{
		if (!Configuration.AllowPasswordLogin) {
			lblMessage.Text = "Password login disabled.";
			txtPassword.Text = "";
			return;
		}

		Master.ClearLogin ();

		if (!Authentication.Login (txtUser.Text, txtPassword.Text, Request, Response)) {
			lblMessage.Text = "Could not log in";
			txtPassword.Text = "";
		} else {
			Response.Redirect (txtReferrer.Value, false);
		}
	}

	protected void cmdLoginOauth_Click (object sender, EventArgs e)
	{
		Session ["login_referrer"] = Request.QueryString ["referrer"];
		AuthenticationHelper.Authenticate ();
	}

	protected void cmdLoginOpenId_Click (object sender, EventArgs e)
	{
		try {
			using (OpenIdRelyingParty openid = new OpenIdRelyingParty ()) {
				var realm = Request.Url.GetComponents (UriComponents.SchemeAndServer, UriFormat.UriEscaped) + "/";
				var return_to = Request.Url.StripQueryArgumentsWithPrefix ("auto-redirect-openid");

				if (!string.IsNullOrEmpty (txtReferrer.Value) && !return_to.Query.Contains ("referrer"))
					return_to = new Uri (return_to.ToString () + (string.IsNullOrEmpty (return_to.Query) ? "?" : "&") + "referrer=" + HttpUtility.UrlEncode (txtReferrer.Value));

				IAuthenticationRequest request = openid.CreateRequest (Configuration.OpenIdProvider, realm, return_to);

				var fetch = new FetchRequest ();
				fetch.Attributes.Add (new AttributeRequest (WellKnownAttributes.Contact.Email, true));
				request.AddExtension (fetch);

				// Send your visitor to their Provider for authentication.
				request.RedirectToProvider ();
			}
		} catch (ProtocolException ex) {
			// The user probably entered an Identifier that
			// was not a valid OpenID endpoint.
			lblMessage.Text = Utils.FormatException (ex);
		}
	}
}

