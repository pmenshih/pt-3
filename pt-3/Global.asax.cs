using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Configuration;

namespace psychoTest
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

        void Application_Error(object sender, EventArgs e)
        {
            /*Exception exc = Server.GetLastError();

            string to = ConfigurationManager.AppSettings["EmailDeveloperAddress"];
            string subject = ConfigurationManager.AppSettings["EmailSystemName"] + ": server error";
            string message = exc.Source + "<br/><br/><br/>" + exc.Message;

            Core.BLL.SendEmail(to
                                , subject
                                , message);*/
        }
    }
}
