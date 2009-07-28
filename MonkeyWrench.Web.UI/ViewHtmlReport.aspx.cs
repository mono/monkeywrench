/*
 * ViewHtmlReport.aspx.cs
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
using System.Net;
using System.Web;
using System.Web.UI;

using MonkeyWrench;
using MonkeyWrench.Web.WebServices;

public partial class ViewHtmlReport : System.Web.UI.Page
{
	private static void FixupHtml (string filename, int workfile_id, string md5)
	{
		string [] find;
		string [] replace;
		string line;
		string name = !string.IsNullOrEmpty (md5) ? "md5" : "workfile_id";
		string value = !string.IsNullOrEmpty (md5) ? md5 : workfile_id.ToString ();

		find = new string [] { 
				"img src=\"", 
				"img src='", 
				"a href=\"", 
				"a href='" };

		replace = new string []{
				string.Format ("img src=\"ViewHtmlReport.aspx?{1}={0}&amp;filename=", value, name),
				string.Format ("img src='ViewHtmlReport.aspx?{1}={0}&amp;filename=", value, name),
				string.Format ("a href=\"ViewHtmlReport.aspx?{1}={0}&amp;filename=", value, name),
				string.Format ("a href='ViewHtmlReport.aspx?{1}={0}&amp;filename=", value, name)};

		using (FileStream fs_writer = new FileStream (filename + ".out", FileMode.Create, FileAccess.Write, FileShare.Read)) {
			using (StreamWriter writer = new StreamWriter (fs_writer)) {
				using (FileStream fs_reader = new FileStream (filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					using (StreamReader reader = new StreamReader (fs_reader)) {
						while (null != (line = reader.ReadLine ())) {
							for (int i = 0; i < find.Length; i++)
								line = line.Replace (find [i], replace [i]);

							// undo any changes for relative links
							line = line.Replace (string.Format ("ViewHtmlReport.aspx?workfile_id={0}&amp;filename=#", workfile_id), "#");
							// undo any changes for javascript links
							line = line.Replace (string.Format ("ViewHtmlReport.aspx?workfile_id={0}&amp;filename=javascript", workfile_id), "javascript");

							writer.Write (line);
							writer.Write ('\n');
						}
					}
				}
			}
		}

		File.Delete (filename);
		File.Move (filename + ".out", filename);
	}

	protected void Page_Load (object sender, EventArgs e)
	{
		string md5 = Request ["md5"];
		string filename = Request ["filename"];

		int workfile_id;

		int.TryParse (Request ["workfile_id"], out workfile_id);

		if (string.IsNullOrEmpty (filename)) {
			// send html file
			string tmp_html_filename = null;

			Response.ContentType = "text/html";

			try {
				tmp_html_filename = Path.GetTempFileName ();

				using (WebClient web = new WebClient ()) {
					web.DownloadFile (Utils.CreateWebServiceDownloadUrl (Request, workfile_id, false), tmp_html_filename);

					if (web.ResponseHeaders ["Content-Encoding"] == "gzip")
						MonkeyWrench.FileUtilities.GZUncompress (tmp_html_filename);

					FixupHtml (tmp_html_filename, workfile_id, md5);
				}

				Response.TransmitFile (tmp_html_filename);
			} catch (HttpException ex) {
				Logger.Log ("ViewHtmlReport: Exception while download html: {0} (redirected to login page)", ex.Message);
				Response.Redirect ("Login.asxp");
			} finally {
				try {
					File.Delete (tmp_html_filename);
				} catch {
					// Ignore any exceptions.
				}
				Response.End ();
			}
			return;
		}

		string url = Utils.CreateWebServiceDownloadUrl (Request, workfile_id, true);
		if (!string.IsNullOrEmpty (md5))
			url += "&md5=" + md5;
		url += "&filename=" + HttpUtility.UrlEncode (filename);
		Response.Redirect (url);
	}
}
