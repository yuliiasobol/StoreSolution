using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(StoreWebApplication.Startup))]
namespace StoreWebApplication
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
