using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(psychoTest.Startup))]
namespace psychoTest
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
