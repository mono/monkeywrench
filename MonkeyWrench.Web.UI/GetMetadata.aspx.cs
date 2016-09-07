using System;
using System.IO;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Linq;
using System.Collections.Generic;

using MonkeyWrench.DataClasses;
using MonkeyWrench.DataClasses.Logic;
using MonkeyWrench.Web.WebServices;

namespace MonkeyWrench.Web.UI
{
	public partial class GetMetadata : GetData
	{
		protected override string Filename {
			get { return "metadata.json"; }
		}
	}
}

