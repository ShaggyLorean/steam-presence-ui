using System;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading.Tasks;

namespace SteamPresenceUI
{
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                MessageBox.Show((e.ExceptionObject as Exception)?.ToString() ?? "Unknown AppDomain Error", "Fatal AppDomain Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                MessageBox.Show(e.Exception.ToString(), "Fatal Task Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.SetObserved();
            };
        }

        public static bool IsMinimizedStartup { get; private set; }
        private static System.Threading.Mutex _mutex = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            foreach (var arg in e.Args)
            {
                if (arg.ToLower() == "--minimized" || arg.ToLower() == "-minimized")
                {
                    IsMinimizedStartup = true;
                }
            }

            const string appName = "SteamPresenceUI_Mutex_Lock";
            bool createdNew;

            _mutex = new System.Threading.Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("SteamPresence Companion is already running in the background.\nPlease check your system tray.", "Already Running", MessageBoxButton.OK, MessageBoxImage.Information);
                Environment.Exit(0);
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
