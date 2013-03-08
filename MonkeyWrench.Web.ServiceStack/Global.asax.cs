
using System;
using System.Web;
using System.Web.SessionState;

using ServiceStack.WebHost.Endpoints;
using ServiceStack.ServiceInterface;
using Funq;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Configuration;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Authentication.OpenId;
using ServiceStack.ServiceInterface.Validation;

namespace MonkeyWrench.Web.ServiceStack
{
	public class Global : System.Web.HttpApplication
	{
		public class WrenchAppHost : AppHostBase
		{
			//Tell Service Stack the name of your application and where to find your web services
			public WrenchAppHost ()
				: base ("Wrench ServiceStack-based service", typeof (WrenchAppHost).Assembly)
			{

			}
			
			public override void Configure (Container container)
			{
				container.Register<ICacheClient> (new MemoryCacheClient ());
				var appSettings = new AppSettings ();
				Plugins.Add (new AuthFeature (() => new WrenchAuthUserSession (),
				                              new[] { new GoogleOpenIdOAuthProvider (appSettings) }));
				Plugins.Add (new ValidationFeature ());
				container.RegisterValidators (typeof (WrenchAppHost).Assembly);
				Routes.AddFromAssembly (typeof (WrenchAppHost).Assembly);
			}
		}
		
		protected virtual void Application_Start (Object sender, EventArgs e)
		{
			new WrenchAppHost ().Init ();
		}
		
		protected virtual void Session_Start (Object sender, EventArgs e)
		{
		}
		
		protected virtual void Application_BeginRequest (Object sender, EventArgs e)
		{
		}
		
		protected virtual void Application_EndRequest (Object sender, EventArgs e)
		{
		}
		
		protected virtual void Application_AuthenticateRequest (Object sender, EventArgs e)
		{
		}
		
		protected virtual void Application_Error (Object sender, EventArgs e)
		{
		}
		
		protected virtual void Session_End (Object sender, EventArgs e)
		{
		}
		
		protected virtual void Application_End (Object sender, EventArgs e)
		{
		}
	}
}

