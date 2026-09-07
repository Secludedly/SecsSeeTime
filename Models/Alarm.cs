using System;
using System.IO;
using SecSeeTime.Enums;

namespace SecSeeTime.Models
{
    public class Alarm
    {
        // =============================================================
        // IDENTIFICATION
        // =============================================================

        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = "New Alarm";


        // =============================================================
        // ENABLED STATE
        // =============================================================

        public bool IsEnabled { get; set; } = true;


        // =============================================================
        // ALARM TIME
        // =============================================================

        /// <summary>
        /// The time of day the alarm should activate.
        /// </summary>
        public TimeSpan Time { get; set; } = new TimeSpan(7, 0, 0);


        // =============================================================
        // REPEAT SETTINGS
        // =============================================================

        public AlarmRepeatMode RepeatMode { get; set; } =
            AlarmRepeatMode.Once;


        /// <summary>
        /// Used when RepeatMode is Custom.
        /// </summary>
        public DayOfWeek[] CustomDays { get; set; } =
            Array.Empty<DayOfWeek>();


        // =============================================================
        // SOUND SETTINGS
        // =============================================================

        public AlarmSoundType SoundType { get; set; } =
            AlarmSoundType.BuiltIn;


        /// <summary>
        /// Name of a built-in alarm sound.
        /// </summary>
        public string BuiltInSoundName { get; set; } =
            "Classic Alarm";


        /// <summary>
        /// Full path to a locally stored audio file.
        /// </summary>
        public string? LocalAudioPath { get; set; }


        /// <summary>
        /// YouTube URL used as an alarm source.
        /// </summary>
        public string? YouTubeUrl { get; set; }


        // =============================================================
        // AUDIO SETTINGS
        // =============================================================

        /// <summary>
        /// Alarm volume from 0.0 to 1.0.
        /// </summary>
        public double Volume { get; set; } = 1.0;


        // =============================================================
        // ALARM BEHAVIOR
        // =============================================================

        /*
         * These are stored in seconds rather than minutes.
         *
         * The editor offers a "30 seconds" option, and a minute-based
         * field could not represent it: it rounded down to zero and the
         * engine clamped that to a single second.
         */

        /// <summary>
        /// How long the alarm is allowed to sound before a silent period begins.
        /// </summary>
        public int SoundDurationSeconds { get; set; } = 120;


        /// <summary>
        /// How long the alarm stays silent before sounding again.
        /// </summary>
        public int SilenceDurationSeconds { get; set; } = 120;


        /// <summary>
        /// Whether the alarm should continue its sound/silence
        /// cycle until manually dismissed.
        /// </summary>
        public bool RepeatSoundCycle { get; set; } = true;


        // =============================================================
        // SNOOZE
        // =============================================================

        public bool SnoozeEnabled { get; set; } = true;


        public int SnoozeMinutes { get; set; } = 5;


        // =============================================================
        // METADATA
        // =============================================================

        public DateTime CreatedAt { get; set; } =
            DateTime.Now;


        // =============================================================
        // SCHEDULING
        // =============================================================

        public bool ShouldRunOn(DateTime date)
        {
            return RepeatMode switch
            {
                AlarmRepeatMode.Once =>
                    true,

                AlarmRepeatMode.Daily =>
                    true,

                AlarmRepeatMode.Weekdays =>
                    date.DayOfWeek != DayOfWeek.Saturday &&
                    date.DayOfWeek != DayOfWeek.Sunday,

                AlarmRepeatMode.Weekends =>
                    date.DayOfWeek == DayOfWeek.Saturday ||
                    date.DayOfWeek == DayOfWeek.Sunday,

                AlarmRepeatMode.Custom =>
                    Array.Exists(
                        CustomDays,
                        day => day == date.DayOfWeek),

                _ => false
            };
        }


        /// <summary>
        /// This alarm's scheduled moment on the given calendar day.
        /// </summary>
        public DateTime GetDateTime(DateTime date)
        {
            return date.Date.Add(Time);
        }


        /// <summary>
        /// The next moment this alarm is due, or null if it will
        /// never fire again.
        /// </summary>
        public DateTime? GetNextOccurrence(DateTime after)
        {
            // Look far enough ahead to cover any weekly pattern.
            for (int dayOffset = 0; dayOffset <= 8; dayOffset++)
            {
                DateTime day =
                    after.Date.AddDays(dayOffset);

                if (!ShouldRunOn(day))
                    continue;

                DateTime candidate =
                    GetDateTime(day);

                if (candidate > after)
                    return candidate;
            }

            return null;
        }


        // =============================================================
        // DESCRIPTION
        // =============================================================

        /// <summary>
        /// A short description of where this alarm's
        /// audio comes from. Shown on the alarm screen.
        /// </summary>
        public string DescribeSource()
        {
            switch (SoundType)
            {
                case AlarmSoundType.LocalFile:

                    if (string.IsNullOrWhiteSpace(LocalAudioPath))
                        return "MY AUDIO FILE";

                    return "FILE • " +
                        Path.GetFileName(LocalAudioPath)!.ToUpperInvariant();

                case AlarmSoundType.YouTube:

                    return "YOUTUBE • " +
                        (string.IsNullOrWhiteSpace(YouTubeUrl)
                            ? "NO LINK"
                            : "STREAMING");

                default:

                    return "BUILT-IN • " +
                        BuiltInSoundName.ToUpperInvariant();
            }
        }


        public string DescribeRepeat()
        {
            return RepeatMode switch
            {
                AlarmRepeatMode.Daily => "Every day",

                AlarmRepeatMode.Weekdays => "Weekdays",

                AlarmRepeatMode.Weekends => "Weekends",

                AlarmRepeatMode.Custom =>
                    CustomDays.Length == 0
                        ? "Custom"
                        : string.Join(
                            " ",
                            Array.ConvertAll(
                                CustomDays,
                                day => day.ToString()
                                    .Substring(0, 3)
                                    .ToUpperInvariant())),

                _ => "Once"
            };
        }


        /// <summary>
        /// The alarm's time in the application's current 12/24-hour format.
        /// Bound by the alarm list.
        /// </summary>
        public string TimeDisplay => Helpers.TimeHelper.FormatTime(Time);


        /// <summary>Bindable form of <see cref="DescribeRepeat"/>.</summary>
        public string RepeatDescription => DescribeRepeat();


        /// <summary>Bindable form of <see cref="DescribeSource"/>.</summary>
        public string SourceSummary => DescribeSource();


        public override string ToString()
        {
            return $"{Name} - {Time:h\\:mm}";
        }
    }
}
