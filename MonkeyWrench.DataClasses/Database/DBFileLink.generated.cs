/*
 * This file has been generated. 
 * If you modify it you'll loose your changes.
 */ 


using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;

#pragma warning disable 649

namespace MonkeyWrench.DataClasses
{
	public partial class DBFileLink : DBRecord
	{
		private string _link;
		private int _work_id;

		public string @link { get { return _link; } set { _link = value; } }
		public int @work_id { get { return _work_id; } set { _work_id = value; } }


		public override string Table
		{
			get { return "FileLink"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "link", "work_id" };
			}
		}
        

	}
}

