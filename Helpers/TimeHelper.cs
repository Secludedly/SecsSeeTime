using SecSeeTime.Services;
using System;

namespace SecSeeTime.Helpers
{
    /// <summary>
    /// Formatting only. No state, no timers.
    /// </summary>
    public static class TimeHelper
    {
        // =============================================================
        // CLOCK
        // =============================================================

        public static string FormatClock(
            DateTime time,
            bool use24Hour = false,
            bool showSeconds = true)
        {
            // 24-hour is zero-padded, which is how a 24-hour clock is
            // conventionally written.
            if (use24Hour)
            {
                return showSeconds
                    ? time.ToString("HH:mm:ss")
                    : time.ToString("HH:mm");
            }

            // 12-hour drops the leading zero.
            return showSeconds
                ? time.ToString("h:mm:ss tt")
                : time.ToString("h:mm tt");
        }


        /// <summary>
        /// Formats a time of day in whatever format the application is
        /// currently set to. This is the entry point every clock, alarm row and
        /// status line should use, so they can never disagree.
        /// </summary>
        public static string FormatTime(
            DateTime time,
            bool showSeconds = false)
        {
            return FormatClock(
                time,
                TimeFormatService.Use24HourNow,
                showSeconds);
        }


        /// <summary>
        /// Formats a scheduled time of day, such as an alarm's, in the
        /// application's current format.
        /// </summary>
        public static string FormatTime(TimeSpan timeOfDay)
        {
            // Normalize into a day so the 12-hour branch can produce AM/PM.
            DateTime asTime =
                DateTime.Today.Add(
                    new TimeSpan(
                        timeOfDay.Hours,
                        timeOfDay.Minutes,
                        timeOfDay.Seconds));

            return FormatClock(
                asTime,
                TimeFormatService.Use24HourNow,
                showSeconds: false);
        }


        public static string FormatDate(DateTime time)
        {
            return time.ToString("dddd, MMMM d, yyyy");
        }


        // =============================================================
        // COUNTDOWNS
        // =============================================================

        /// <summary>
        /// m:ss, or h:mm:ss once the span passes an hour.
        /// </summary>
        public static string FormatCountdown(TimeSpan span)
        {
            if (span < TimeSpan.Zero)
                span = TimeSpan.Zero;

            // Round up so a countdown never shows 00:00 while still running.
            span = TimeSpan.FromSeconds(
                Math.Ceiling(span.TotalSeconds));

            if (span.TotalHours >= 1)
            {
                return string.Format(
                    "{0}:{1:00}:{2:00}",
                    (int)span.TotalHours,
                    span.Minutes,
                    span.Seconds);
            }

            return string.Format(
                "{0}:{1:00}",
                (int)span.TotalMinutes,
                span.Seconds);
        }


        /// <summary>
        /// "in 4 minutes", "in 2 hours 15 minutes", "tomorrow at 7:00 AM".
        /// </summary>
        public static string DescribeTimeUntil(DateTime target, DateTime now)
        {
            TimeSpan span = target - now;

            if (span <= TimeSpan.Zero)
                return "now";

            if (span.TotalMinutes < 1)
                return $"in {Math.Max(1, (int)span.TotalSeconds)}s";

            if (span.TotalHours < 1)
                return $"in {(int)span.TotalMinutes}m";

            if (span.TotalHours < 24)
                return $"in {(int)span.TotalHours}h {span.Minutes}m";

            return target.ToString("ddd ") + FormatTime(target);
        }


        // =============================================================
        // DURATIONS
        // =============================================================

        /// <summary>
        /// "30 seconds", "1 minute", "2 minutes".
        /// </summary>
        public static string DescribeDuration(int seconds)
        {
            if (seconds < 60)
                return $"{seconds} seconds";

            int minutes = seconds / 60;

            if (seconds % 60 == 0)
            {
                return minutes == 1
                    ? "1 minute"
                    : $"{minutes} minutes";
            }

            return $"{minutes}m {seconds % 60}s";
        }


        // =============================================================
        // TIME ZONES
        // =============================================================

        public static string FriendlyZoneName(TimeZoneInfo zone)
        {
            string id = zone.Id;

            if (id.Contains("Eastern"))
                return "NEW YORK";

            if (id.Contains("Central"))
                return "CENTRAL";

            if (id.Contains("Mountain"))
                return "MOUNTAIN";

            if (id.Contains("Pacific"))
                return "PACIFIC";

            if (id.Contains("Alaska"))
                return "ALASKA";

            if (id.Contains("Hawaii"))
                return "HAWAII";

            return zone.StandardName.ToUpperInvariant();
        }


        public static string CurrentZoneLabel(
            TimeZoneInfo zone,
            DateTime now)
        {
            string abbreviation =
                zone.IsDaylightSavingTime(now)
                    ? zone.DaylightName
                    : zone.StandardName;

            return $"{FriendlyZoneName(zone)} • {abbreviation.ToUpperInvariant()}";
        }
    }
}
