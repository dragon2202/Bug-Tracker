using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Bug_Tracker
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        //https://stackoverflow.com/questions/5282677/how-to-use-session-variable-in-asp-using-c-sharp
        void Session_Start(object sender, EventArgs e)
        {
            // Code that runs when a new session is started  
            if (Session["UserName"] != null)
            {
                //Redirect to Welcome Page if Session is not null 
            }
            else
            {
                Response.Redirect("/");
            }


        }
        void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            if (ex is HttpException && ((HttpException)ex).GetHttpCode() == 404)
            {
                Response.Redirect("~/Error/NotFound");
            }
        }
    }
}
