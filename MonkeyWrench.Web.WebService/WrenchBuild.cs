using System;
using MonkeyWrench.DataClasses;

namespace MonkeyWrench.WebServices
{
	// POCO representing a build
	public struct Build
	{
		// Either end date for finished job or start date for running/not ran job
		public DateTime Date { get; set; }
		public string Commit { get; set; }
		public int? CommitId { get; set; }
		public string Lane { get; set; }
		public string Project { get; set; }
		public DBState State { get; set; }
		public string Author { get; set; }
		public string BuildBot { get; set; }
		public string Url { get; set; }
	}
}

