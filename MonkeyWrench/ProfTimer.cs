using System;
using System.Diagnostics;

namespace MonkeyWrench
{
	public class ProfTimer : IDisposable
	{
		private readonly string name;
		private Stopwatch watch;

		public ProfTimer (string name)
		{
			this.name = name;
			watch = new Stopwatch ();
			watch.Start ();
		}

		#region IDisposable implementation

		public void Dispose ()
		{
			watch.Stop ();
			Logger.Log ("TIMER: {0} - {1} ms", name, watch.ElapsedMilliseconds);
		}

		#endregion
	}
}

