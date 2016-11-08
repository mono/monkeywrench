
namespace MonkeyWrench.Web.UI
{
	using System;
	using System.IO;
	using System.Net;
	using System.Web;
	using System.Web.UI;
	using System.Linq;
	using System.Collections.Generic;

	using MonkeyWrench.DataClasses;
	using MonkeyWrench.DataClasses.Logic;
	using MonkeyWrench.Web.WebServices;

	public partial class GetLatest : System.Web.UI.Page
	{

		private new Master Master
		{
			get { return base.Master as Master; } 
		}

		private WebServiceLogin webServiceLogin;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e); 
			webServiceLogin = Authentication.CreateLogin (Request);

			Response.AppendHeader("Access-Control-Allow-Origin", "*");
			Response.AppendHeader("Content-Type", "text/plain");
			Response.StatusCode = 404;
			Response.Write("GetLatest is deprecated, please use http://wrench.internalx.com/Wrench/GetManifest.aspx or http://wrench.internalx.com/Wrench/GetMetadata.aspx");
		}

	}
}

