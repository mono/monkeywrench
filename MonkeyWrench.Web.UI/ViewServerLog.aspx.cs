/*
 * ViewServerLog.aspx.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (rolf@xamarin.com)
 *   
 * Copyright 2011 Xamarin Inc. (http://www.xamarin.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

public partial class ViewServerLog : System.Web.UI.Page
{
	private new Master Master
	{
		get { return base.Master as Master; }
	}

	protected override void OnInit (EventArgs e)
	{
		base.OnInit (e);

		try {
			LoginResponse response = Master.WebService.Login (Master.WebServiceLogin);

			if (!Authentication.IsInRole (response, Roles.Administrator)) {
				divLog.Text = "You need admin rights.";
				return;
			}

			long max_length = 32768;

			long.TryParse (Request ["maxlength"], out max_length);

			if (max_length == 0)
				max_length = 32768;

			using (FileStream fs = new FileStream (MonkeyWrench.Configuration.LogFile, FileMode.Open, FileAccess.Read)) {
				max_length = Math.Min (max_length, (long) fs.Length);
				fs.Seek (fs.Length - max_length, SeekOrigin.Begin);
				using (StreamReader reader = new StreamReader (fs)) {
					if (fs.Position > 0)
						reader.ReadLine (); // skip the first (partial)
					divLog.Text = reader.ReadToEnd ().Replace ("\n", "<br/>").Replace ("\r", "").Replace (" ", "&nbsp;");
				}
				lblLength.Text = string.Format ("Showing the last {0} bytes in the log file", max_length);
			}
		} catch (Exception ex) {
			divLog.Text = Utils.FormatException (ex, true);
		}
	}
}

