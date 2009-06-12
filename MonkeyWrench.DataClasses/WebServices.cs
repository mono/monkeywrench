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
			return CreateWebServiceDownloadUrl (workfile_id, WebServiceLogin);
		}

		public string CreateWebServiceDownloadRevisionUrl (int revision_id, bool diff)
		{
			return CreateWebServiceDownloadRevisionUrl (revision_id, diff, WebServiceLogin);
		}

		public static string CreateWebServiceDownloadUrl (int workfile_id, WebServiceLogin login)
		{
			string uri = CreatePage ("Download.aspx");
			uri += "?";
			uri += "cookie=" + login.Cookie;
			uri += "&ip4=" + login.Ip4;
			uri += "&user=" + login.User;
			uri += "&workfile_id=" + workfile_id.ToString ();
			return uri;

		}

		public static string CreateWebServiceDownloadRevisionUrl (int revision_id, bool diff, WebServiceLogin login)
		{
			string uri = CreatePage ("Download.aspx");
			uri += "?";
			uri += "cookie=" + login.Cookie;
			uri += "&ip4=" + login.Ip4;
			uri += "&user=" + login.User;
			uri += "&revision_id=" + revision_id.ToString ();
			uri += "&diff=" + (diff ? "true" : "false");
			return uri;
		}

		/// <summary>
		/// This method will uncompress the data too (if required)
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static string DownloadString (string url)
		{
			string tmp = null;
			try {
				tmp = Path.GetTempFileName ();
				using (WebClient wc = new WebClient ()) {
					wc.DownloadFile (url, tmp);

					if (wc.ResponseHeaders ["Content-Encoding"] == "gzip") {
						FileUtilities.GZUncompress (tmp);
					}
					return File.ReadAllText (tmp);
				}
			} finally {
				try {
					File.Delete (tmp);
				} catch {
				}
			}
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
