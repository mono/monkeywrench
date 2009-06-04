/*
 * DBFile.generated.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */


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
	public partial class DBFile : DBRecord
	{
		private string _filename;
		private string _md5;
		private int? _file_id;
		private string _mime;
		private string _compressed_mime;
		private int _size;
		private bool _hidden;

		public string @filename { get { return _filename; } set { _filename = value; } }
		public string @md5 { get { return _md5; } set { _md5 = value; } }
		public int? @file_id { get { return _file_id; } set { _file_id = value; } }
		public string @mime { get { return _mime; } set { _mime = value; } }
		public string @compressed_mime { get { return _compressed_mime; } set { _compressed_mime = value; } }
		public int @size { get { return _size; } set { _size = value; } }
		public bool @hidden { get { return _hidden; } set { _hidden = value; } }


		public override string Table
		{
			get { return "File"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "filename", "md5", "file_id", "mime", "compressed_mime", "size", "hidden" };
			}
		}
        

	}
}

