/*
 * Async.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MonkeyWrench
{
	public static class Async
	{
		public static void Execute (WaitCallback callback, object state)
		{
			ThreadPool.QueueUserWorkItem (delegate (object st)
			{
				try {
					callback (state);
				} catch (Exception ex) {
					// This is really exceptional, so don't try any fancy logging.
					// Leaking an exception here will cause the entire process to die.
					try {
						Console.WriteLine ("Exception during async execution: {0}", ex);
					} catch {
						// ignore completely
					}
				}
			});
		}
		public static void Execute (WaitCallback callback)
		{
			Execute (callback, null);
		}
	}
}
