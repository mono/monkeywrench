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

public partial class Login : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		string action = Request ["action"];
		string referrer = Request ["referrer"];


		if (!string.IsNullOrEmpty (referrer))
			txtReferrer.Value = referrer;

		if (!this.IsPostBack) {
			if (Request.UrlReferrer != null && string.IsNullOrEmpty (txtReferrer.Value))
				txtReferrer.Value = Request.UrlReferrer.AbsoluteUri;
		}

		if (string.IsNullOrEmpty (Configuration.OpenIdProvider)) {
			cmdLoginOpenId.Visible = false;
		} else {
			cmdLoginOpenId.Visible = true;
			
			OpenIdRelyingParty openid = new OpenIdRelyingParty ();
			var oidresponse = openid.GetResponse ();
			if (oidresponse != null) {
				switch (oidresponse.Status) {
				case AuthenticationStatus.Authenticated:
					// This is where you would look for any OpenID extension responses included
					// in the authentication assertion.
					var fetch = oidresponse.GetExtension<FetchResponse> ();
					string email;

					try {
						email = fetch.Attributes [WellKnownAttributes.Contact.Email].Values [0];

						WebServiceLogin login = new WebServiceLogin ();
						login.Password = Configuration.WebServicePassword;
						login.User = Configuration.Host;
						var response = Master.WebService.LoginOpenId (login, email, Utilities.GetExternalIP (Request));
						if (response.Exception != null) {
							lblMessageOpenId.Text = response.Exception.Message;
						} else {
							Authentication.SetCookies (Response, response);
							Response.Redirect (txtReferrer.Value, false);
							return;
						}
					} catch (Exception ex) {
						lblMessageOpenId.Text = Utils.FormatException (ex);
					}
					break;
				default:
					lblMessageOpenId.Text = "Could not login using OpenId: " + oidresponse.Status.ToString ();
					break;
				}
			}
		}

		// can't refer back to itself
		if (txtReferrer.Value.Contains ("Login.aspx"))
			txtReferrer.Value = "index.aspx";

		if (!string.IsNullOrEmpty (action) && action == "logout") {
			if (Request.Cookies ["cookie"] != null) {
				Master.WebService.Logout (Master.WebServiceLogin);
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
	}

	protected void cmdLogin_Click (object sender, EventArgs e)
	{
		Master.ClearLogin ();

		try {
			if (!Authentication.Login (txtUser.Text, txtPassword.Text, Request, Response)) {
				lblMessage.Text = "Could not log in";
				txtPassword.Text = "";
			} else {
				Response.Redirect (txtReferrer.Value, false);
			}
		} catch (Exception) {
			lblMessage.Text = "Invalid user/password.";
			txtPassword.Text = "";
		}
	}

	protected void cmdLoginOpenId_Click (object sender, EventArgs e)
	{
		try {
			using (OpenIdRelyingParty openid = new OpenIdRelyingParty ()) {
				var realm = Request.Url.GetComponents (UriComponents.SchemeAndServer, UriFormat.UriEscaped) + "/";
				IAuthenticationRequest request = openid.CreateRequest (Configuration.OpenIdProvider, realm, Request.Url);

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

