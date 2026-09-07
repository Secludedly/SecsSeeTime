using SecSeeTime.Enums;
using SecSeeTime.Themes;
using System.Collections.Generic;

namespace SecSeeTime.Models
{
    /// <summary>
    /// Application-level preferences that survive a restart.
    /// </summary>
    public sealed class AlarmSettings
    {
        /// <summary>Name of the active theme preset.</summary>
        public string ThemeName { get; set; } =
            ThemePresets.DefaultThemeName;


        /// <summary>
        /// 12- or 24-hour display for every clock in the application. Read
        /// through <see cref="Services.TimeFormatService"/> rather than
        /// directly, so nothing formats a time on its own.
        /// </summary>
        public TimeFormatMode TimeFormat { get; set; } =
            TimeFormatMode.TwelveHour;


        /*
         * Superseded properties, kept only so older settings.json files still
         * deserialize. Use24HourClock was never written by any UI and is now
         * replaced by TimeFormat above; ShowSeconds is a property of the active
         * AlarmTheme, edited in Appearance Studio.
         */

        public bool Use24HourClock { get; set; }


        public bool ShowSeconds { get; set; } = true;

        /// <summary>Saved World Clock cities, in user-defined order.</summary>
        public List<WorldClockEntry> WorldClocks { get; set; } = new();


        /// <summary>
        /// How long after its scheduled time an alarm may still fire.
        /// Prevents a machine waking from sleep at noon from blasting
        /// an alarm that was due at 7am.
        /// </summary>
        public int MissedAlarmGraceSeconds { get; set; } = 300;

        /// <summary>Launch Sec's See Time automatically when Windows starts.</summary>
        public bool StartWithWindows { get; set; }

        /// <summary>Hide the main window in the notification area when minimized/closed.</summary>
        public bool MinimizeToTray { get; set; } = true;

        /// <summary>Raise a Windows notification when an alarm starts ringing.</summary>
        public bool NotificationsEnabled { get; set; } = true;
    }
}
