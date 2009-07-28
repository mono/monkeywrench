/*
 * GetFile.aspx.cs
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
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Net;

using MonkeyWrench;
using MonkeyWrench.Web.WebServices;

public partial class GetFile : System.Web.UI.Page
{
	protected void Page_Load (object sender, EventArgs e)
	{
		int id;

		if (!int.TryParse (Request ["id"], out id))
			throw new HttpException ("Invalid id");

		Response.Redirect (Utils.CreateWebServiceDownloadUrl (Request, id, true));
	}
}
