/*
 * EnvironmentVariablesEditor.ascx.cs
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
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.Web.WebServices;

namespace MonkeyWrench.Web.UI
{
	public partial class EnvironmentVariablesEditor : System.Web.UI.UserControl
	{
		public List<DBEnvironmentVariable> Variables;
		public DBHost Host;
		public DBLane Lane;
		public Master Master;

		protected void Page_Load (object sender, EventArgs e)
		{
			if (Variables == null)
				return;

			foreach (DBEnvironmentVariable variable in Variables) {
				tblVariables.Rows.AddAt (
					tblVariables.Rows.Count - 1,
					Utils.CreateTableRow (
						variable.name,
						string.Format ("<a href=\"javascript:editEnvironmentVariable ({0}, {1}, '{2}', {3}, '{4}')\">{5}</a>", Lane == null ? 0 : Lane.id, Host == null ? 0 : Host.id, variable.value.Replace ("\'", "\\\'"), variable.id, variable.name, variable.value),
						Utils.CreateLinkButton (variable.id.ToString (), "Delete", "delete", variable.id.ToString (), EnvironmentVariable_OnCommand)
						)
				);
			}
		}

		public void EnvironmentVariable_OnCommand (object sender, CommandEventArgs e)
		{
			switch (e.CommandName) {
			case "delete":
				Master.WebService.DeleteEnvironmentVariable (Master.WebServiceLogin, int.Parse ((string) e.CommandArgument));
				Response.Redirect (Request.Url.ToString (), false);
				break;
			case "add":
				Master.WebService.AddEnvironmentVariable (Master.WebServiceLogin, Lane == null ? (int?) null : Lane.id, Host  == null ? (int?) null : Host.id, txtVariableName.Text, txtVariableValue.Text);
				Response.Redirect (Request.Url.ToString (), false);
				break;
			}
		}
	}
}