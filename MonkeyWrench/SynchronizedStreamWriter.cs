using System;
using System.IO;

namespace MonkeyWrench
{
	public class SynchronizedStreamWriter : IDisposable
	{
		StreamWriter writer;
		object sync_object = new object ();
		DateTime last_stamp;

		public SynchronizedStreamWriter (StreamWriter writer)
		{
			this.writer = writer;
			this.last_stamp = DateTime.Now;
		}

		public DateTime LastStamp {
			get {
				lock (sync_object)
					return last_stamp;
			}
		}

		public void WriteLine (string line)
		{
			lock (sync_object) {
				writer.WriteLine (line);
				writer.Flush ();
				last_stamp = DateTime.Now;
			}
		}

		public void Dispose ()
		{
			lock (sync_object) {
				writer.Dispose ();
			}
		}
	}
}

