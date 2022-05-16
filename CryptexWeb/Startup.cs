using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CryptexWeb.Startup))]

namespace CryptexWeb
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}