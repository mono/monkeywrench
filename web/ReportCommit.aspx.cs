using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Builder;

public partial class ReportCommit : System.Web.UI.Page
{
	protected void Page_Load (object sender, EventArgs e)
	{
		try {
			HttpPostedFile xml;
			xml = Request.Files ["xml"];

			if (xml != null) {
				string outdir = Configuration.GetSchedulerCommitsDirectory ();
				string outfile = Path.Combine (outdir, string.Format ("commit-{0}.xml", DateTime.Now.ToString ("yyyy-MM-dd-HH-mm-ss")));

				if (!Directory.Exists (outdir))
					Directory.CreateDirectory (outdir);
				
				Logger.Log ("ReportCommit.aspx: Got 'xml' with size {0} bytes, writing to '{1}'", xml.ContentLength, outfile);
	
				byte [] buffer = new byte [1024];
				int read;
				using (FileStream writer = new FileStream (outfile, FileMode.CreateNew, FileAccess.Write, FileShare.None, buffer.Length)) {
					while (0 < (read = xml.InputStream.Read (buffer, 0, buffer.Length))) {
						writer.Write (buffer, 0, read);
					}
				}
			} else {
				Logger.Log ("ReportCommit.aspx: Didn't get a file called 'xml'");
			}

			Response.Write ("OK\n");
		} catch (Exception ex) {
			Logger.Log ("ReportCommit.aspx: Got exception: {0}", ex);
		}
	}
}
