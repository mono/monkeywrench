/*
 * DBRelease.generated.cs
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
	public partial class DBRelease : DBRecord
	{
		private string _version;
		private string _revision;
		private string _description;
		private string _filename;

		public string @version { get { return _version; } set { _version = value; } }
		public string @revision { get { return _revision; } set { _revision = value; } }
		public string @description { get { return _description; } set { _description = value; } }
		public string @filename { get { return _filename; } set { _filename = value; } }


		public override string Table
		{
			get { return "Release"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "version", "revision", "description", "filename" };
			}
		}
        

	}
}

