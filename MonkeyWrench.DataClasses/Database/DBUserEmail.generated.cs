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
	public partial class DBUserEmail : DBRecord
	{
		private int _person_id;
		private string _email;

		public int @person_id { get { return _person_id; } set { _person_id = value; } }
		public string @email { get { return _email; } set { _email = value; } }


		public override string Table
		{
			get { return "UserEmail"; }
		}
        

		public override string [] Fields
		{
			get
			{
				return new string [] { "person_id", "email" };
			}
		}
        

	}
}

