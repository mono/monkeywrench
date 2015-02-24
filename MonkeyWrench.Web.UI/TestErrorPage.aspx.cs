using System;
using System.Web;
using System.Web.UI;

namespace MonkeyWrench.Web.UI
{
	
	public partial class TestErrorPage : System.Web.UI.Page
	{
		protected void Page_Load (object sender, EventArgs e) {
			throw new ApplicationException ("I am test exception");
		}
	}
}

