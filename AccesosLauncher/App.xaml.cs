using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading;
using System.Windows;

namespace AccesosLauncher
{
    public partial class App : System.Windows.Application
    {
        public static IConfiguration Configuration { get; private set; } = null!;
        private static Mutex? _mutex;

        public App()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "AccesosLauncher_Mutex_2d5e7c1e-9b2c-4a6a-9b6d-2f3e7c8d9a1b";
            
            _mutex = new Mutex(true, appName, out bool createdNew);

            if (!createdNew)
            {
                System.Windows.MessageBox.Show("AccesosLauncher ya se está ejecutando.", "Aplicación en ejecución", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Windows.Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);

            MainWindow = new MainWindow();
            MainWindow.Show();
            // The MainWindow's own Loaded event will handle hiding to tray
        }
    }
}