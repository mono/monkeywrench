/*
 * ProcessHelperWindows.cs
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MonkeyWrench
{
	internal class ProcessHelperWindows : IProcessHelper
	{
		protected override List<int> GetChildren (int pid)
		{
			List<int> result = null;

			IntPtr snapshot = CreateToolhelp32Snapshot (TH32CS_SNAPPROCESS, 0);
			PROCESSENTRY32 process = new PROCESSENTRY32 ();

			if (Process32First (snapshot, ref process)) {
				do {
					if (process.th32ParentProcessID == pid) {
						if (result == null)
							result = new List<int> ();
						result.Add ((int) process.th32ProcessID);
					}
					process = new PROCESSENTRY32 (); // zero out memory
				} while (Process32Next (snapshot, ref process));
			}
			CloseHandle (snapshot);

			return result;
		}
		[DllImport ("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		static extern IntPtr CreateToolhelp32Snapshot ([In]UInt32 dwFlags, [In]UInt32 th32ProcessID);

		[DllImport ("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		static extern bool Process32First ([In]IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

		[DllImport ("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		static extern bool Process32Next ([In]IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

		[DllImport ("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle (IntPtr hSnapshot);

		private const uint TH32CS_SNAPPROCESS = 0x00000002;

		[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct PROCESSENTRY32
		{
			const int MAX_PATH = 260;
			internal UInt32 dwSize;
			internal UInt32 cntUsage;
			internal UInt32 th32ProcessID;
			internal IntPtr th32DefaultHeapID;
			internal UInt32 th32ModuleID;
			internal UInt32 cntThreads;
			internal UInt32 th32ParentProcessID;
			internal Int32 pcPriClassBase;
			internal UInt32 dwFlags;
			[MarshalAs (UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
			internal string szExeFile;
		}
	}
}
