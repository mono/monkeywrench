/*
 * WebServices.cs
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
using System.IO;
using System.Net;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;

namespace MonkeyWrench.Web.WebServices
{
	public partial class WebServices
	{
		public WebServiceLogin WebServiceLogin;

		private static string CreatePage (string page)
		{
			if (Configuration.WebServiceUrl.EndsWith ("/")) {
				return Configuration.WebServiceUrl + page;
			} else {
				return Configuration.WebServiceUrl + "/" + page;
			}
		}

		public void CreateLogin (string user, string password)
		{
			WebServiceLogin = new WebServiceLogin ();
			WebServiceLogin.User = user;
			WebServiceLogin.Password = password;
		}

		public static WebServices Create ()
		{
			WebServices result = new WebServices ();
			result.Url = CreatePage ("WebServices.asmx");
			return result;
		}

		public string CreateWebServiceDownloadUrl (int workfile_id)
		{
			string uri = CreatePage ("Download.aspx");
			uri += "?";
			uri += "cookie=" + WebServiceLogin.Cookie;
			uri += "&ip4=" + WebServiceLogin.Ip4;
			uri += "&user=" + WebServiceLogin.User;
			uri += "&workfile_id=" + workfile_id.ToString ();
			return uri;

		}

		public void DownloadFile (DBWorkFile file, string directory)
		{
			string filename = Path.Combine (directory, file.filename);

			using (WebClient web = new WebClient ()) {
				web.DownloadFile (CreateWebServiceDownloadUrl (file.id), filename);

				if (web.ResponseHeaders ["Content-Encoding"] == "gzip")
					FileUtilities.GZUncompress (filename);
			}
		}
	}
}
