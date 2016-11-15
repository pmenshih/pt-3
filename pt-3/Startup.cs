using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(pt_3.Startup))]
namespace pt_3
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
