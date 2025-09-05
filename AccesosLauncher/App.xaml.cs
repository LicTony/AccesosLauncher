using Microsoft.Extensions.Configuration;
using System.IO;
using System.Windows;
using Application = System.Windows.Application;

namespace AccesosLauncher
{
    public partial class App : Application
    {
        public static IConfiguration Configuration { get; private set; }

        public App()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();
        }
    }
}