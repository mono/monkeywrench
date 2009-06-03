/*
 * Utils.cs
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
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

/// <summary>
/// Summary description for Utils
/// </summary>
public static class Utils
{
	public static TableHeaderRow CreateTableHeaderRow (params string [] cells)
	{
		TableHeaderRow result = new TableHeaderRow ();
		for (int i = 0; i < cells.Length; i++)
			result.Cells.Add (CreateTableHeaderCell (cells [i]));
		return result;
	}

	public static TableRow CreateTableRow (params object [] cells)
	{
		TableRow result = new TableRow ();
		for (int i = 0; i < cells.Length; i++)
			result.Cells.Add (CreateTableCell (cells [i]));
		return result;
	}

	public static TableRow CreateTableRow (params string [] cells)
	{
		TableRow result = new TableRow ();
		for (int i = 0; i < cells.Length; i++)
			result.Cells.Add (CreateTableCell (cells [i]));
		return result;
	}
	public static TableHeaderCell CreateTableHeaderCell (string text)
	{
		TableHeaderCell result = new TableHeaderCell ();
		result.Text = text;
		return result;
	}
	public static TableCell CreateTableCell (object obj)
	{
		if (obj is string)
			return CreateTableCell ((string) obj);
		if (obj is WebControl)
			return CreateTableCell ((WebControl) obj);

		throw new ArgumentException (string.Format ("Can't create a cell of a '{0}'", obj.GetType ()));
	}
	public static TableCell CreateTableCell (WebControl control)
	{
		TableCell result = new TableCell ();
		result.Controls.Add (control);
		return result;
	}
	public static TableCell CreateTableCell (string text)
	{
		TableCell result = new TableCell ();
		result.Text = text;
		return result;
	}
	public static TableCell CreateTableCell (string text, string cssclass)
	{
		TableCell result = new TableCell ();
		result.Text = text;
		result.CssClass = cssclass;
		return result;
	}

	public static LinkButton CreateLinkButton (string id, string text, string command, CommandEventHandler handler)
	{
		return CreateLinkButton (id, text, command, string.Empty, handler);
	}

	public static LinkButton CreateLinkButton (string id, string text, string command, string argument, CommandEventHandler handler)
	{
		LinkButton result = new LinkButton ();
		result.ID = id;
		result.Text = text;
		result.CommandName = command;
		result.CommandArgument = argument;
		result.Command += handler;
		return result;
	}

	public static TextBox CreateTextBox (string id, string text)
	{
		TextBox result = new TextBox ();
		result.ID = id;
		result.Text = text;
		return result;
	}

	public static string GetCookie (HttpRequest request, string name)
	{
		HttpCookie cookie = request.Cookies [name];
		return cookie == null ? null : cookie.Value;
	}

	public static DBLane FindLane (IEnumerable<DBLane> lanes, int id)
	{
		foreach (DBLane lane in lanes)
			if (lane.id == id)
				return lane;
		return null;
	}

	public static DBHost FindHost (IEnumerable<DBHost> hosts, int id)
	{
		foreach (DBHost host in hosts)
			if (host.id == id)
				return host;
		return null;
	}

	public static DBLanefile FindFile (IEnumerable<DBLanefile> files, Predicate<DBLanefile> pred)
	{
		foreach (DBLanefile file in files)
			if (pred (file))
				return file;
		return null;
	}

	/// <summary>
	/// Checks if 'child' is a descendent of 'parent'.
	/// </summary>
	/// <param name="lanes"></param>
	/// <param name="parent"></param>
	/// <param name="child"></param>
	/// <param name="iteration">Pass in 0</param>
	/// <returns></returns>
	public static bool IsDescendentLaneOf (IEnumerable<DBLane> lanes, DBLane parent, DBLane child, int iteration)
	{
		if (parent.id == child.id)
			return false;

		if (child.parent_lane_id == parent.id)
			return true;

		if (child.parent_lane_id == null)
			return false;

		if (iteration >= 100)
			return true; // catch infinite recursion

		DBLane p = FindLane (lanes, child.parent_lane_id.Value);

		return IsDescendentLaneOf (lanes, parent, p, iteration + 1);
	}

	public static string GetExternalIP (HttpRequest request)
	{
		if (request.IsLocal)
			return System.Net.Dns.GetHostEntry (System.Net.Dns.GetHostName ()).AddressList [0].ToString ();
		else
			return request.UserHostAddress;
	}

	private static WebServices web_service;

	public static WebServices WebService
	{
		get
		{
			if (web_service == null) 
				web_service = WebServices.Create ();
			return web_service;
		}
	}

	public static WebServiceLogin CreateWebServiceLogin (HttpRequest Request)
	{
		WebServiceLogin web_service_login;
		web_service_login = new WebServiceLogin ();
		web_service_login.Cookie = Utils.GetCookie (Request, "cookie");
		if (HttpContext.Current.User != null)
			web_service_login.User = HttpContext.Current.User.Identity.Name;
		web_service_login.Ip4 = Utils.GetExternalIP (Request);
		
		// Console.WriteLine ("Master, Cookie: {0}, User: {1}", web_service_login.Cookie, web_service_login.User);

		return web_service_login;
	}

	public static string CreateWebServiceDownloadUrl (HttpRequest Request, int workfile_id)
	{
		return WebServices.CreateWebServiceDownloadUrl (workfile_id, Utils.CreateWebServiceLogin (Request));
	}

	public static bool IsInRole (string role)
	{
		return HttpContext.Current.User.IsInRole (role);
	}

	public static int? TryParseInt32 (string input)
	{
		int i;
		if (int.TryParse (input, out i))
			return i;
		return null;
	}
}
