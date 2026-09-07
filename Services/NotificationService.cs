using System;
using SecSeeTime.Models;

namespace SecSeeTime.Services
{
    /// <summary>
    /// Raises Windows notifications for alarm events.
    /// <para>
    /// Delivery goes through the notification-area icon. On Windows 10/11 the
    /// shell renders a NotifyIcon balloon as an ordinary toast and files it in
    /// Action Center, so these are real Windows notifications with no extra
    /// dependency. The Windows App SDK's AppNotificationManager is deliberately
    /// not used: it requires either a packaged identity or the Windows App
    /// Runtime bootstrapper, and merely referencing Microsoft.WindowsAppSDK
    /// makes WebView2's build targets drop the WPF control assembly.
    /// </para>
    /// </summary>
    public sealed class NotificationService : IDisposable
    {
        private const string AppTitle = "Sec's See Time";

        private readonly Action _activateApp;
        private Func<string, string, bool>? _sink;
        private bool _disposed;

        public NotificationService(Action activateApp)
        {
            _activateApp = activateApp;
        }

        /// <summary>True once a delivery channel is attached and usable.</summary>
        public bool IsRegistered => !_disposed && _sink != null;

        /// <summary>
        /// Kept so existing startup code reads naturally. No process-wide
        /// registration is required for shell notifications.
        /// </summary>
        public void Register()
        {
        }

        /// <summary>
        /// Connects the delivery channel, normally <see cref="TrayService.ShowBalloon"/>.
        /// Notifications raised before this is called are dropped rather than queued,
        /// which is what we want: a stale alarm toast is worse than none.
        /// </summary>
        public void AttachSink(Func<string, string, bool> sink)
        {
            if (_disposed)
                return;

            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public void DetachSink()
        {
            _sink = null;
        }

        /// <summary>Brings the main window forward, e.g. when a notification is clicked.</summary>
        public void ActivateApp()
        {
            if (!_disposed)
                _activateApp();
        }

        /// <returns><c>true</c> when the notification was handed to Windows.</returns>
        public bool ShowAlarm(Alarm alarm)
        {
            if (alarm == null)
                return false;

            string name = string.IsNullOrWhiteSpace(alarm.Name)
                ? "Alarm"
                : alarm.Name;

            return Show(AppTitle, $"Alarm ringing: {name}");
        }

        /// <returns><c>true</c> when the notification was handed to Windows.</returns>
        public bool Show(string title, string message)
        {
            if (_disposed)
                return false;

            Func<string, string, bool>? sink = _sink;
            if (sink == null)
                return false;

            try
            {
                return sink(title, message);
            }
            catch
            {
                // A notification is never important enough to take the app down.
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _sink = null;
        }
    }
}
