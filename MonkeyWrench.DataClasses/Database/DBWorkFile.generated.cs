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
	public partial class DBWorkFile : DBRecord
	{
		private int _work_id;
		private int _file_id;
		private bool _hidden;
		private string _filename;

		public int @work_id { get { return _work_id; } set { _work_id = value; } }
		public int @file_id { get { return _file_id; } set { _file_id = value; } }
		public bool @hidden { get { return _hidden; } set { _hidden = value; } }
		public string @filename { get { return _filename; } set { _filename = value; } }


		public override string Table
		{
			get { return "WorkFile"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "work_id", "file_id", "hidden", "filename" };
			}
		}
        

	}
}

