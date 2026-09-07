using System;
using System.IO;
using System.Windows;

namespace SecSeeTime
{
    public partial class App : Application
    {
        private static readonly string LogPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SecsSeeTime",
                "crash.log");

        public Services.NotificationService? Notifications { get; private set; }

        public MainWindow? MainWindowInstance { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += (_, args) =>
            {
                Log(args.Exception);

                MessageBox.Show(
                    "Something went wrong:\n\n" +
                    args.Exception.Message +
                    "\n\nDetails were written to:\n" + LogPath,
                    "Sec's See Time",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                Log(args.ExceptionObject as Exception);

            base.OnStartup(e);

            Notifications = new Services.NotificationService(ActivateMainWindow);
            Notifications.Register();

            MainWindowInstance = new MainWindow();
            MainWindowInstance.Show();
        }

        private void ActivateMainWindow()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MainWindow? window = MainWindowInstance;
                if (window == null)
                    return;

                window.ShowFromTray();
            }));
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Notifications?.Dispose();
            }
            catch
            {
                // Never block application exit because notification cleanup failed.
            }

            base.OnExit(e);
        }

        private static void Log(Exception? ex)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(
                    LogPath,
                    $"[{DateTime.Now:u}] {ex}\n\n");
            }
            catch
            {
                // Logging must never throw.
            }
        }
    }
}
