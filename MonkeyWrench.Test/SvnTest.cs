using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MonkeyWrench.Test
{
	[Test]
	public class SvnTest : VCTest
	{
		public string GetTestCheckoutPath
		{
			get
			{
				return Path.Combine (Runner.TemporaryTestDirectory, Path.Combine (GetType ().Name, "Checkout"));
			}
		}
		
		protected override void Commit (string filename, string contents, string commit_msg, string author, string email)
		{
			if (!Directory.Exists (GetTestCheckoutPath))
				Directory.CreateDirectory (GetTestCheckoutPath);
			Execute (GetTestCheckoutPath, "svn", "co", "file://" + GetTestRepositoryPath, GetTestCheckoutPath);
			File.WriteAllText (Path.Combine (GetTestCheckoutPath, filename), contents);
			Execute (GetTestCheckoutPath, "svn", "add", filename);
			CommitInternal (filename, commit_msg, author, email);

			// yay for platform independence, Directory.Delete throws an UnauthorizedException because some directories are read-only
			Execute (Runner.TemporaryTestDirectory, "rm", "-Rf", GetTestCheckoutPath);
			//Directory.Delete (GetTestCheckoutPath, true);
		}

		protected override void CommitInternalWithMessageFile (string filename, string commit_msg_filename, string author, string email)
		{
			Execute (GetTestCheckoutPath, "svn", "commit", "-F", commit_msg_filename, "--username", author);
		}

		protected override string GetVCType
		{
			get { return "svn"; }
		}

		protected override void InitializeTestRepository (string path)
		{
			Execute (path, "svnadmin", "create", path);
		}
	}
}
