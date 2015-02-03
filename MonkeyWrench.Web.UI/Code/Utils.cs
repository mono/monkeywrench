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
		TableCell cell;
		for (int i = 0; i < cells.Length; i++) {
			cell = cells [i] as TableCell;
			if (cell == null)
				cell = CreateTableCell (cells [i]);
			result.Cells.Add (cell);
		}
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

	public static MonkeyWrench.WebServices.WebServices local_web_service;

	public static MonkeyWrench.WebServices.WebServices LocalWebService
	{
		get
		{
			if (local_web_service == null)
				local_web_service = new MonkeyWrench.WebServices.WebServices (false);
			return local_web_service;
		}
	}

	public static int? TryParseInt32 (string input)
	{
		int i;
		if (int.TryParse (input, out i))
			return i;
		return null;
	}

	public static string FormatException (Exception ex)
	{
		return FormatException (ex, false);
	}

	public static string FormatException (Exception ex, bool include_stack_trace)
	{
		return FormatException (include_stack_trace ? ex.ToString () : ex.Message);
	}

	public static string FormatException (string str)
	{
		return str.Replace ("\n", "<br/>\n").Replace (" ", "&nbsp;").Replace ("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");
	}
}
