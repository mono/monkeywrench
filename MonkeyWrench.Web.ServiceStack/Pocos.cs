using System;

using MonkeyWrench.DataClasses;

namespace MonkeyWrench.Web.ServiceStack
{
	public struct BuildBot
	{
		public int ID { get; set; }
		public string Name { get; set; }
		public string Arch { get; set; }
		public string Description { get; set; }
	}
	
	public struct Build
	{
		// Either end date for finished job or start date for running/not ran job
		public DateTime Date { get; set; }
		public string Commit { get; set; }
		public int? CommitId { get; set; }
		public string Lane { get; set; }
		public int LaneID { get; set; }
		public string Project { get; set; }
		public DBState State { get; set; }
		public string Author { get; set; }
		public string BuildBot { get; set; }
		public int HostID { get; set; }
		public Uri Url { get; set; }
	}
	
	public struct Lane
	{
		public int ID { get; set; }
		public string Name { get; set; }
	}
	
	public struct BuildStep
	{
		public string Name { get; set; }
		public DBState State { get; set; }
		public DateTime? StartDate { get; set; }
		public TimeSpan? Duration { get; set; }
		public string Author { get; set; } // Safeguard if we couldn't get info from other source
		public string LogId { get; set; } // ID of the log file for the step
	}
}

