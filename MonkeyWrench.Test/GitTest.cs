using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace MonkeyWrench.Test
{
	[Test]
	class GitTest : VCTest
	{
		protected override string GetVCType
		{
			get { return "git"; }
		}

		protected override void InitializeTestRepository (string path)
		{
			Execute (path, "git", "init");
		}

		protected override void Commit (string filename, string contents, string commit_msg, string author, string email)
		{
			File.WriteAllText (Path.Combine (GetTestRepositoryPath, filename), contents);
			Execute (GetTestRepositoryPath, "git", "add", "--", filename);
			CommitInternal (filename, commit_msg, author, email);
		}

		protected override void CommitInternalWithMessageFile (string filename, string commit_msg_filename, string author, string email)
		{
			Execute (GetTestRepositoryPath, "git", "commit", "-F", commit_msg_filename, "--author=" + author + " <" + email + ">" , "--quiet", "--cleanup=verbatim");
		}
	}
}
