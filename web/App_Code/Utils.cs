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
}
