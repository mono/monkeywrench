using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MonkeyWrench
{
	public interface IDB
	{
		IDbCommand CreateCommand ();
		IDbTransaction BeginTransaction ();
	}
}
