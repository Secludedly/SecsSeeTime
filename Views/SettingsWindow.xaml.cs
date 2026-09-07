using SecSeeTime.Enums;
using SecSeeTime.Models;
using SecSeeTime.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SecSeeTime.Views
{
    /// <summary>
    /// Application preferences.
    ///
    /// Options apply and save the moment they are changed, matching the
    /// World Clock dialog, and each change raises <c>onChanged</c> so the
    /// caller can push the new values into the live services. That matters
    /// because this is a modal dialog and an alarm can fire while it is open.
    ///
    /// Clock appearance -- 24-hour, seconds, fonts, colors -- deliberately
    /// lives in Appearance Studio, because it is a property of the active
    /// theme rather than an application preference.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly AlarmSettings _settings;
        private readonly StorageService _storage;
        private readonly Action _onChanged;
        private readonly Func<bool> _sendTestNotification;

        /// <summary>Suppresses change handlers while the UI is populated.</summary>
        private bool _loading;

        private static readonly TimeFormatChoice[] TimeFormatChoices =
        {
            new("12-hour  •  1:30 PM", TimeFormatMode.TwelveHour),
            new("24-hour  •  13:30", TimeFormatMode.TwentyFourHour),
            new("Follow the active theme", TimeFormatMode.FollowTheme)
        };

        private static readonly GraceChoice[] GraceChoices =
        {
            new("30 seconds", 30),
            new("1 minute", 60),
            new("5 minutes (default)", 300),
            new("15 minutes", 900),
            new("1 hour", 3600),
            new("6 hours", 21600)
        };


        public SettingsWindow(
            AlarmSettings settings,
            StorageService storage,
            Action onChanged,
            Func<bool> sendTestNotification)
        {
            InitializeComponent();

            _settings = settings;
            _storage = storage;
            _onChanged = onChanged;
            _sendTestNotification = sendTestNotification;

            TimeFormatBox.ItemsSource = TimeFormatChoices;
            GraceBox.ItemsSource = GraceChoices;

            LoadFromSettings();
        }


        // =============================================================
        // LOAD
        // =============================================================

        private void LoadFromSettings()
        {
            _loading = true;

            try
            {
                StartWithWindowsBox.IsChecked = _settings.StartWithWindows;
                MinimizeToTrayBox.IsChecked = _settings.MinimizeToTray;
                NotificationsBox.IsChecked = _settings.NotificationsEnabled;

                TimeFormatBox.SelectedItem =
                    TimeFormatChoices.FirstOrDefault(
                        c => c.Mode == _settings.TimeFormat)
                    ?? TimeFormatChoices[0];

                GraceBox.SelectedItem = NearestGraceChoice(
                    _settings.MissedAlarmGraceSeconds);

                DataFolderText.Text = _storage.Folder;
            }
            finally
            {
                _loading = false;
            }

            UpdateDependentState();
        }


        /// <summary>
        /// A saved value edited by hand will not always match a preset, so
        /// snap to the closest one rather than leaving the box blank.
        /// </summary>
        private static GraceChoice NearestGraceChoice(int seconds) =>
            GraceChoices
                .OrderBy(c => Math.Abs(c.Seconds - seconds))
                .First();


        /// <summary>
        /// Keeps the test button honest: there is nothing to test while
        /// notifications are switched off.
        /// </summary>
        private void UpdateDependentState()
        {
            TestNotificationButton.IsEnabled =
                NotificationsBox.IsChecked == true;

            TimeFormatHint.Text =
                _settings.TimeFormat == TimeFormatMode.FollowTheme
                    ? "Each theme carries its own 24-hour setting, edited in " +
                      "Appearance Studio, so clocks can differ between themes."
                    : "Overrides the 24-hour option in Appearance Studio, so " +
                      "every clock agrees whichever theme is active.";
        }


        // =============================================================
        // SAVE
        // =============================================================

        private void Option_Click(object sender, RoutedEventArgs e)
        {
            if (_loading)
                return;

            bool startupWasEnabled = _settings.StartWithWindows;

            _settings.StartWithWindows = StartWithWindowsBox.IsChecked == true;
            _settings.MinimizeToTray = MinimizeToTrayBox.IsChecked == true;
            _settings.NotificationsEnabled = NotificationsBox.IsChecked == true;

            // Only touch the registry when this option actually moved.
            if (_settings.StartWithWindows != startupWasEnabled)
                StartupService.SetEnabled(_settings.StartWithWindows);

            UpdateDependentState();

            Commit();
        }


        private void TimeFormatBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_loading ||
                TimeFormatBox.SelectedItem is not TimeFormatChoice choice)
            {
                return;
            }

            _settings.TimeFormat = choice.Mode;

            UpdateDependentState();

            Commit();
        }


        private void GraceBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_loading || GraceBox.SelectedItem is not GraceChoice choice)
                return;

            _settings.MissedAlarmGraceSeconds = choice.Seconds;

            Commit();
        }


        private void Commit()
        {
            _storage.SaveSettings(_settings);

            _onChanged();

            StatusText.Text = $"Saved at {DateTime.Now:HH:mm:ss}";
        }


        // =============================================================
        // ACTIONS
        // =============================================================

        private void TestNotification_Click(object sender, RoutedEventArgs e)
        {
            if (_sendTestNotification())
            {
                StatusText.Text =
                    "Test notification sent. Check the notification area.";

                return;
            }

            StatusText.Text = "Windows would not accept the notification.";
        }


        private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(_storage.Folder);

                // UseShellExecute so this goes through the shell rather
                // than trying to exec the directory directly.
                Process.Start(
                    new ProcessStartInfo(_storage.Folder)
                    {
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not open the data folder:\n\n" + ex.Message,
                    "Settings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }


        private void ResetPreferences_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult answer = MessageBox.Show(
                "Reset the options on this page to their defaults?\n\n" +
                "Your alarms, saved themes and World Clock cities are not affected.",
                "Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
                return;

            var defaults = new AlarmSettings();

            _settings.StartWithWindows = defaults.StartWithWindows;
            _settings.MinimizeToTray = defaults.MinimizeToTray;
            _settings.NotificationsEnabled = defaults.NotificationsEnabled;
            _settings.MissedAlarmGraceSeconds = defaults.MissedAlarmGraceSeconds;
            _settings.TimeFormat = defaults.TimeFormat;

            StartupService.SetEnabled(_settings.StartWithWindows);

            LoadFromSettings();

            Commit();

            StatusText.Text = "Preferences reset to defaults.";
        }


        // =============================================================
        // CHROME
        // =============================================================

        private void Header_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            try
            {
                DragMove();
            }
            catch
            {
                // DragMove throws if the button was already released.
            }
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e) =>
            Close();


        private sealed record GraceChoice(string Label, int Seconds)
        {
            public override string ToString() => Label;
        }


        private sealed record TimeFormatChoice(
            string Label,
            TimeFormatMode Mode)
        {
            public override string ToString() => Label;
        }
    }
}
