using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace CryptexWeb
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter("Bearer"));

            // Web API configuration and services
            //config.SuppressHostPrincipal();
            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new {controller = "Default", id = RouteParameter.Optional}
            );
        }
    }
}