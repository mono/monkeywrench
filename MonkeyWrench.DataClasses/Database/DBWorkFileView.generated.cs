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
	public partial class DBWorkFileView : DBView
	{
		private int _work_id;
		private int _file_id;
		private string _filename;
		private bool _hidden;
		private string _mime;
		private string _compressed_mime;
		private string _md5;
		private bool _internal;
		private int? _file_file_id;

		public int @work_id { get { return _work_id; } set { _work_id = value; } }
		public int @file_id { get { return _file_id; } set { _file_id = value; } }
		public string @filename { get { return _filename; } set { _filename = value; } }
		public bool @hidden { get { return _hidden; } set { _hidden = value; } }
		public string @mime { get { return _mime; } set { _mime = value; } }
		public string @compressed_mime { get { return _compressed_mime; } set { _compressed_mime = value; } }
		public string @md5 { get { return _md5; } set { _md5 = value; } }
		public bool @internal { get { return _internal; } set { _internal = value; } }
		public int? @file_file_id { get { return _file_file_id; } set { _file_file_id = value; } }


		public const string SQL = 
@"SELECT WorkFile.id, WorkFile.work_id, WorkFile.file_id, WorkFile.filename, WorkFile.hidden, File.mime, File.compressed_mime, File.md5, Command.internal, File.file_id AS file_file_id 
	FROM WorkFile 
		INNER JOIN File ON WorkFile.file_id = File.id 
		INNER JOIN Work ON WorkFile.work_id = Work.id 
		INNER JOIN Command ON Work.command_id = Command.id;";


		private static string [] _fields_ = new string [] { "work_id", "file_id", "filename", "hidden", "mime", "compressed_mime", "md5", "internal", "file_file_id" };
		public override string [] Fields
		{
			get
			{
				return _fields_;
			}
		}
        

		public DBWorkFileView ()
		{
		}
	
		public DBWorkFileView (IDataReader reader) 
			: base (reader)
		{
		}


	}
}

