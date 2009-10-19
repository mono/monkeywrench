using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace MonkeyWrench.Test
{
	public static class Database
	{
		public static void Clean (DB db)
		{
			using (IDbTransaction transaction = db.BeginTransaction ()) {
				db.ExecuteNonQuery (@"
DELETE FROM Login;
DELETE FROM Person;
DELETE FROM WorkFile;
DELETE FROM Work;
DELETE FROM RevisionWork;
DELETE FROM Revision;
DELETE FROM File;
DELETE FROM Lanefiles;
DELETE FROM Lanefile;
DELETE FROM HostLane;
DELETE FROM Command;
DELETE FROM LaneDependency;
DELETE FROM LaneDeletionDirective;
DELETE FROM FileDeletionDirective;
DELETE FROM EnvironmentVariable;
DELETE FROM Lane;
DELETE FROM MasterHost;
DELETE FROM Host;

INSERT INTO Person (login, password, roles) VALUES ('scheduler', 'hithere', 'Administrator');
");
				transaction.Commit ();
			}
		}

		public static void Create ()
		{
			StringBuilder stdout = new StringBuilder ();
			StringBuilder stderr = new StringBuilder ();
			TestBase.Execute (Runner.TemporaryTestDirectory, Path.Combine (Runner.SourceDirectory, Path.Combine ("scripts", "dbcontrol.sh")), TimeSpan.FromMinutes (5), stdout, stderr, "create");
		}

		public static void Stop ()
		{
			TestBase.Execute (Runner.TemporaryTestDirectory, Path.Combine (Runner.SourceDirectory, Path.Combine ("scripts", "dbcontrol.sh")), "stop");
		}

	}
}
