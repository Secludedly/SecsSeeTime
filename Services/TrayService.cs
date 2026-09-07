using System;
using System.Drawing;
using Forms = System.Windows.Forms;

namespace SecSeeTime.Services
{
    public sealed class TrayService : IDisposable
    {
        private readonly Forms.NotifyIcon _icon;
        private readonly Forms.ContextMenuStrip _menu;
        private readonly Forms.ToolStripMenuItem _startupItem;
        private readonly Forms.ToolStripMenuItem _minimizeItem;
        private bool _disposed;

        // Guards the CheckedChanged handlers while SyncToggles writes the
        // menu state, so mirroring the Settings window does not echo back
        // out as a fresh user toggle.
        private bool _suppressToggleEvents;

        public TrayService(
            Action showClock,
            Action openAlarms,
            Action openWorldClock,
            Action openAppearance,
            Func<bool> getStartWithWindows,
            Action<bool> setStartWithWindows,
            Func<bool> getMinimizeToTray,
            Action<bool> setMinimizeToTray,
            Action exit)
        {
            _menu = new Forms.ContextMenuStrip();

            var show = new Forms.ToolStripMenuItem("Show Clock");
            show.Click += (_, _) => showClock();

            var alarms = new Forms.ToolStripMenuItem("Manage Alarms");
            alarms.Click += (_, _) => openAlarms();

            var world = new Forms.ToolStripMenuItem("World Clock");
            world.Click += (_, _) => openWorldClock();

            var appearance = new Forms.ToolStripMenuItem("Appearance Studio");
            appearance.Click += (_, _) => openAppearance();

            var startup = new Forms.ToolStripMenuItem("Start with Windows")
            {
                Checked = getStartWithWindows(),
                CheckOnClick = true
            };
            startup.CheckedChanged += (_, _) =>
            {
                if (!_suppressToggleEvents)
                    setStartWithWindows(startup.Checked);
            };

            var minimize = new Forms.ToolStripMenuItem("Minimize to Tray")
            {
                Checked = getMinimizeToTray(),
                CheckOnClick = true
            };
            minimize.CheckedChanged += (_, _) =>
            {
                if (!_suppressToggleEvents)
                    setMinimizeToTray(minimize.Checked);
            };

            _startupItem = startup;
            _minimizeItem = minimize;

            _menu.Items.Add(show);
            _menu.Items.Add(new Forms.ToolStripSeparator());
            _menu.Items.Add(alarms);
            _menu.Items.Add(world);
            _menu.Items.Add(appearance);
            _menu.Items.Add(new Forms.ToolStripSeparator());
            _menu.Items.Add(startup);
            _menu.Items.Add(minimize);
            _menu.Items.Add(new Forms.ToolStripSeparator());

            var exitItem = new Forms.ToolStripMenuItem("Exit");
            exitItem.Click += (_, _) => exit();
            _menu.Items.Add(exitItem);

            _icon = new Forms.NotifyIcon
            {
                Text = "Sec's See Time",
                Visible = true,
                ContextMenuStrip = _menu,
                Icon = LoadIcon()
            };

            _icon.MouseClick += (_, e) =>
            {
                if (e.Button == Forms.MouseButtons.Left)
                    showClock();
            };
        }

        /// <summary>
        /// Mirrors externally changed preferences onto the tray menu, so the
        /// checkmarks do not go stale after the Settings window edits them.
        /// </summary>
        public void SyncToggles(bool startWithWindows, bool minimizeToTray)
        {
            if (_disposed)
                return;

            _suppressToggleEvents = true;

            try
            {
                _startupItem.Checked = startWithWindows;
                _minimizeItem.Checked = minimizeToTray;
            }
            finally
            {
                _suppressToggleEvents = false;
            }
        }


        /// <returns><c>true</c> when the balloon was handed to the shell.</returns>
        public bool ShowBalloon(string title, string text)
        {
            if (_disposed)
                return false;

            try
            {
                _icon.ShowBalloonTip(5000, title, text, Forms.ToolTipIcon.Info);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Icon LoadIcon()
        {
            try
            {
                string? executable = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(executable))
                {
                    Icon? icon = Icon.ExtractAssociatedIcon(executable);
                    if (icon != null)
                        return icon;
                }
            }
            catch
            {
                // Fall through to the standard Windows application icon.
            }

            return SystemIcons.Application;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _icon.Visible = false;
            _icon.Dispose();
            _menu.Dispose();
        }
    }
}
