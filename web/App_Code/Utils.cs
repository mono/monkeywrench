using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/// <summary>
/// Summary description for Utils
/// </summary>
public static class Utils
{
	public static TableHeaderRow CreateTableHeaderRow(params string[] cells)
	{
		TableHeaderRow result = new TableHeaderRow();
		for (int i = 0; i < cells.Length; i++)
			result.Cells.Add(CreateTableHeaderCell(cells[i]));
		return result;
	}

	public static TableRow CreateTableRow (params object [] cells)
	{
		TableRow result = new TableRow ();
		for (int i = 0; i < cells.Length; i++)
			result.Cells.Add (CreateTableCell (cells [i]));
		return result;
	}

	public static TableRow CreateTableRow(params string[] cells)
	{
		TableRow result = new TableRow();
		for (int i = 0; i < cells.Length; i++)
			result.Cells.Add(CreateTableCell(cells[i]));
		return result;
	}
	public static TableHeaderCell CreateTableHeaderCell(string text)
	{
		TableHeaderCell result = new TableHeaderCell();
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
	public static TableCell CreateTableCell(string text)
	{
		TableCell result = new TableCell();
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
}
