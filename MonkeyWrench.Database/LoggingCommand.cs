
using System.Data;
using System.Diagnostics;
using log4net;

namespace MonkeyWrench.Database
{
	public class LoggingCommand : IDbCommand
	{
		static readonly ILog log = LogManager.GetLogger (typeof (LoggingCommand));

		IDbCommand cmd;
		DB db;

		public LoggingCommand (DB db, IDbCommand command)
		{
			this.cmd = command;
			this.db = db;
		}

		#region IDbCommand implementation

		public void Cancel ()
		{
			cmd.Cancel ();
		}

		public IDbDataParameter CreateParameter ()
		{
			return cmd.CreateParameter ();
		}

		public int ExecuteNonQuery ()
		{
			var watch = new Stopwatch ();
			watch.Start ();

			try {
				return cmd.ExecuteNonQuery ();
			} finally {
				watch.Stop ();
				log.DebugFormat ("ExecuteNonQuery {1} ms: {0}", CommandText, watch.ElapsedMilliseconds);
			}
		}

		public IDataReader ExecuteReader ()
		{
			var watch = new Stopwatch ();
			watch.Start ();

			try {
				return cmd.ExecuteReader ();
			} finally {
				watch.Stop ();
				log.DebugFormat ("ExecuteReader {1} ms: {0}", CommandText, watch.ElapsedMilliseconds);
			}
		}

		public IDataReader ExecuteReader (CommandBehavior behavior)
		{
			var watch = new Stopwatch ();
			watch.Start ();

			try {
				return cmd.ExecuteReader (behavior);
			} finally {
				watch.Stop ();
				log.DebugFormat ("ExecuteReader ({2}) {1} ms: {0}", CommandText, watch.ElapsedMilliseconds, behavior);
			}
		}

		public object ExecuteScalar ()
		{
			var watch = new Stopwatch ();
			watch.Start ();

			try {
				return cmd.ExecuteScalar ();
			} finally {
				watch.Stop ();
				log.DebugFormat ("ExecuteScalar ({1}) ms: {0}", CommandText, watch.ElapsedMilliseconds);
			}
		}

		public void Prepare ()
		{
			cmd.Prepare ();
		}

		public string CommandText {
			get {
				return cmd.CommandText;
			}
			set {
				cmd.CommandText = value;
			}
		}

		public int CommandTimeout {
			get {
				return cmd.CommandTimeout;
			}
			set {
				cmd.CommandTimeout = value;
			}
		}

		public CommandType CommandType {
			get {
				return cmd.CommandType;
			}
			set {
				cmd.CommandType = value;
			}
		}

		public IDbConnection Connection {
			get {
				return cmd.Connection;
			}
			set {
				cmd.Connection = value;
			}
		}

		public IDataParameterCollection Parameters {
			get {
				return cmd.Parameters;
			}
		}

		public IDbTransaction Transaction {
			get {
				return cmd.Transaction;
			}
			set {
				cmd.Transaction = value;
			}
		}

		public UpdateRowSource UpdatedRowSource {
			get {
				return cmd.UpdatedRowSource;
			}
			set {
				cmd.UpdatedRowSource = value;
			}
		}

		#endregion

		#region IDisposable implementation

		public void Dispose ()
		{
			cmd.Dispose ();
		}

		#endregion
	}
}

