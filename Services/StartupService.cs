using System;
using Microsoft.Win32;

namespace SecSeeTime.Services
{
    public static class StartupService
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "SecSeeTime";

        public static bool IsEnabled()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
                return key?.GetValue(ValueName) is string value &&
                       !string.IsNullOrWhiteSpace(value);
            }
            catch
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
                if (key == null)
                    return;

                if (!enabled)
                {
                    key.DeleteValue(ValueName, false);
                    return;
                }

                string? executable = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(executable))
                    return;

                key.SetValue(ValueName, $"\"{executable}\" --startup");
            }
            catch
            {
                // Startup preference is non-critical; do not crash the app.
            }
        }
    }
}
