/*
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
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

namespace Builder
{
	public partial class DBWorkFileView : DBView
	{
		private int _work_id;
		private int _file_id;
		private string _filename;
		private bool _hidden;
		private string _mime;
		private string _compressed_mime;
		private bool _internal;

		public int @work_id { get { return _work_id; } }
		public int @file_id { get { return _file_id; } }
		public string @filename { get { return _filename; } }
		public bool @hidden { get { return _hidden; } }
		public string @mime { get { return _mime; } }
		public string @compressed_mime { get { return _compressed_mime; } }
		public bool @internal { get { return _internal; } }



		private static string [] _fields_ = new string [] { "id", "work_id", "file_id", "filename", "hidden", "mime", "compressed_mime", "internal" };
		public override string [] Fields
		{
			get
			{
				return _fields_;
			}
		}
        

		public DBWorkFileView (IDataReader reader) : base (reader)
		{
		}


	}
}

