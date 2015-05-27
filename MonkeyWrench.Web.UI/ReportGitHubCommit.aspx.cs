/*
 * ReportCommit.aspx.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

#pragma warning disable 649 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Script.Serialization;
using log4net;

using MonkeyWrench;
using MonkeyWrench.DataClasses;
using MonkeyWrench.Web.WebServices;

public partial class ReportGitHubCommit : System.Web.UI.Page
{
	private static readonly ILog log = LogManager.GetLogger (typeof (ReportGitHubCommit));

	protected void Page_Load (object sender, EventArgs e)
	{
		string ip = Request.UserHostAddress;
		bool ip_accepted = false;
		string payload;

		log.InfoFormat ("Received GitHub post with {2} files from: {0} allowed ips: {1}", ip, Configuration.AllowedCommitReporterIPs, Request.Files.Count);

		if (log.IsDebugEnabled) {
			foreach (HttpPostedFile f in Request.Files) {
				log.DebugFormat ("ReportGitHubCommit.aspx: got file: {0}", f.FileName);
			}
			foreach (string f in Request.Form.AllKeys) {
				log.DebugFormat ("ReportGitHubCommit.aspx: {0}={1}", f, Request.Form [f]);
			}
		}

		foreach (string allowed_ip in Configuration.AllowedCommitReporterIPs.Split ('.')) {
			if (string.IsNullOrEmpty (ip))
				continue;
			if (Regex.IsMatch (ip, FileUtilities.GlobToRegExp (allowed_ip))) {
				ip_accepted = true;
				break;
			}
		}

		if (!ip_accepted) {
			log.WarnFormat ("{0} tried to send a file, ignored. Allowed IPs: {1}", ip, Configuration.AllowedCommitReporterIPs);
			Response.StatusCode = 403;
			return;
		}

		payload = Request ["payload"];

		if (!string.IsNullOrEmpty (payload)) {
			string outdir = Configuration.GetSchedulerCommitsDirectory ();
			string outfile = Path.Combine (outdir, string.Format ("commit-{0}.xml", DateTime.Now.ToString ("yyyy-MM-dd-HH-mm-ss")));

			if (!Directory.Exists (outdir))
				Directory.CreateDirectory (outdir);

			log.InfoFormat ("Got 'payload' from {2} with size {0} bytes, writing to '{1}'", payload.Length, outfile, ip);

			JavaScriptSerializer json = new JavaScriptSerializer ();
			GitHub.Payload pl = json.Deserialize<GitHub.Payload> (payload);

			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.CloseOutput = true;
			settings.Indent = true;
			settings.IndentChars = "\t";
			XmlWriter writer = XmlWriter.Create (outfile, settings);
			/*
# <monkeywrench version="1">
#   <changeset sourcecountrol="svn|git" root="<repository root>" revision="<revision>">
#     <directories>
#        <directory>/dir1</directory>
#        <directory>/dir2</directory>
#     </directories>
#   </changeset>
# </monkeywrench>
			 * */
			writer.WriteStartElement ("monkeywrench");
			writer.WriteAttributeString ("version", "1");
			foreach (GitHub.Commit commit in pl.commits) {
				writer.WriteStartElement ("changeset");
				writer.WriteAttributeString ("sourcecontrol", "git");
				writer.WriteAttributeString ("root", pl.repository.url);
				writer.WriteAttributeString ("revision", commit.id);
				writer.WriteStartElement ("directories");
				HashSet<string> dirs = new HashSet<string> ();
				foreach (string f in commit.added)
					dirs.Add (Path.GetDirectoryName (f));
				foreach (string f in commit.modified)
					dirs.Add (Path.GetDirectoryName (f));
				foreach (string f in commit.removed)
					dirs.Add (Path.GetDirectoryName (f));
				foreach (string dir in dirs) {
					writer.WriteElementString ("directory", dir);
				}
				writer.WriteEndElement ();
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
			writer.Close ();

		} else {
			log.Warn ("Didn't get a file called 'payload'");
		}

		Response.StatusCode = 204;
		WebServices.ExecuteSchedulerAsync ();
	}

	private class GitHub
	{
		public class Payload
		{
			public string before;
			public string after;
			public string @ref;
			public Commit [] commits;
			public Repository repository;
		}

		public class Commit
		{
			public string id;
			public string message;
			public string timestamp;
			public string url;
			public string [] added;
			public string [] removed;
			public string [] modified;
			public Author author;
		}

		public class Repository
		{
			public string name;
			public string url;
			public string pledgie;
			public string description;
			public string homepage;
			public string watchers;
			public string forks;
			public string @private;
			public Owner owner;
		}

		public class Owner
		{
			public string name;
			public string email;
		}

		public class Author
		{
			public string name;
			public string email;
		}
	}
}
