using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SecSeeTime.Enums;
using SecSeeTime.Models;

namespace SecSeeTime.Services
{
    /// <summary>
    /// Reads and writes the user's alarms and preferences.
    ///
    /// Alarms are serialized through a flat record rather than the
    /// domain model, so adding a property to <see cref="Alarm"/> can
    /// never silently change or invalidate a saved file.
    /// </summary>
    public sealed class StorageService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                WriteIndented = true
            };

        private readonly string _folder;
        private readonly string _alarmsPath;
        private readonly string _settingsPath;


        public StorageService(string? folder = null)
        {
            _folder = folder ?? Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "SecsSeeTime");

            _alarmsPath = Path.Combine(_folder, "alarms.json");
            _settingsPath = Path.Combine(_folder, "settings.json");
        }


        public string AlarmsPath => _alarmsPath;


        public string SettingsPath => _settingsPath;


        /// <summary>Folder holding alarms.json and settings.json.</summary>
        public string Folder => _folder;


        // =============================================================
        // ALARMS
        // =============================================================

        public List<Alarm> LoadAlarms()
        {
            try
            {
                if (!File.Exists(_alarmsPath))
                    return new List<Alarm>();

                string json = File.ReadAllText(_alarmsPath);

                List<AlarmRecord>? records =
                    JsonSerializer.Deserialize<List<AlarmRecord>>(
                        json,
                        JsonOptions);

                if (records == null)
                    return new List<Alarm>();

                return records
                    .Select(ToAlarm)
                    .ToList();
            }
            catch
            {
                /*
                 * A corrupt or unreadable file must not stop the app
                 * from starting. The user loses their saved alarms,
                 * which is bad, but a clock that will not launch is
                 * worse.
                 */
                return new List<Alarm>();
            }
        }


        public void SaveAlarms(IEnumerable<Alarm> alarms)
        {
            try
            {
                List<AlarmRecord> records =
                    alarms.Select(ToRecord).ToList();

                WriteAtomic(
                    _alarmsPath,
                    JsonSerializer.Serialize(records, JsonOptions));
            }
            catch
            {
                // Persistence is best-effort; never break the UI over it.
            }
        }


        // =============================================================
        // SETTINGS
        // =============================================================

        public AlarmSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                    return new AlarmSettings();

                return JsonSerializer.Deserialize<AlarmSettings>(
                           File.ReadAllText(_settingsPath),
                           JsonOptions)
                       ?? new AlarmSettings();
            }
            catch
            {
                return new AlarmSettings();
            }
        }


        public void SaveSettings(AlarmSettings settings)
        {
            try
            {
                WriteAtomic(
                    _settingsPath,
                    JsonSerializer.Serialize(settings, JsonOptions));
            }
            catch
            {
                // Best-effort.
            }
        }


        // =============================================================
        // FILE WRITING
        // =============================================================

        private void WriteAtomic(string path, string contents)
        {
            Directory.CreateDirectory(_folder);

            string temporary = path + ".tmp";

            File.WriteAllText(temporary, contents);

            // Replacing in one step means a crash mid-write cannot
            // leave a half-written alarm list behind.
            File.Move(temporary, path, overwrite: true);
        }


        // =============================================================
        // MAPPING
        // =============================================================

        private static AlarmRecord ToRecord(Alarm alarm)
        {
            return new AlarmRecord
            {
                Id = alarm.Id,
                Name = alarm.Name,
                IsEnabled = alarm.IsEnabled,
                TimeOfDaySeconds = (int)alarm.Time.TotalSeconds,
                RepeatMode = (int)alarm.RepeatMode,
                CustomDays = alarm.CustomDays
                    .Select(day => (int)day)
                    .ToArray(),
                SoundType = (int)alarm.SoundType,
                BuiltInSoundName = alarm.BuiltInSoundName,
                LocalAudioPath = alarm.LocalAudioPath,
                YouTubeUrl = alarm.YouTubeUrl,
                Volume = alarm.Volume,
                SoundDurationSeconds = alarm.SoundDurationSeconds,
                SilenceDurationSeconds = alarm.SilenceDurationSeconds,
                RepeatSoundCycle = alarm.RepeatSoundCycle,
                SnoozeEnabled = alarm.SnoozeEnabled,
                SnoozeMinutes = alarm.SnoozeMinutes,
                CreatedAt = alarm.CreatedAt
            };
        }


        private static Alarm ToAlarm(AlarmRecord record)
        {
            return new Alarm
            {
                Id = record.Id == Guid.Empty
                    ? Guid.NewGuid()
                    : record.Id,

                Name = string.IsNullOrWhiteSpace(record.Name)
                    ? "Alarm"
                    : record.Name,

                IsEnabled = record.IsEnabled,

                Time = TimeSpan.FromSeconds(
                    Math.Clamp(record.TimeOfDaySeconds, 0, 86_399)),

                RepeatMode = Enum.IsDefined(
                    typeof(AlarmRepeatMode),
                    record.RepeatMode)
                    ? (AlarmRepeatMode)record.RepeatMode
                    : AlarmRepeatMode.Once,

                CustomDays = (record.CustomDays ?? Array.Empty<int>())
                    .Where(day => day is >= 0 and <= 6)
                    .Select(day => (DayOfWeek)day)
                    .ToArray(),

                SoundType = Enum.IsDefined(
                    typeof(AlarmSoundType),
                    record.SoundType)
                    ? (AlarmSoundType)record.SoundType
                    : AlarmSoundType.BuiltIn,

                BuiltInSoundName = string.IsNullOrWhiteSpace(record.BuiltInSoundName)
                    ? "Classic Alarm"
                    : record.BuiltInSoundName,

                LocalAudioPath = record.LocalAudioPath,

                YouTubeUrl = record.YouTubeUrl,

                Volume = Math.Clamp(record.Volume, 0.0, 1.0),

                SoundDurationSeconds = Math.Clamp(
                    record.SoundDurationSeconds,
                    5,
                    3600),

                SilenceDurationSeconds = Math.Clamp(
                    record.SilenceDurationSeconds,
                    5,
                    3600),

                RepeatSoundCycle = record.RepeatSoundCycle,

                SnoozeEnabled = record.SnoozeEnabled,

                SnoozeMinutes = Math.Clamp(record.SnoozeMinutes, 1, 720),

                CreatedAt = record.CreatedAt == default
                    ? DateTime.Now
                    : record.CreatedAt
            };
        }


        // =============================================================
        // ON-DISK SHAPE
        // =============================================================

        private sealed class AlarmRecord
        {
            public Guid Id { get; set; }

            public string Name { get; set; } = "Alarm";

            public bool IsEnabled { get; set; } = true;

            public int TimeOfDaySeconds { get; set; }

            public int RepeatMode { get; set; }

            public int[] CustomDays { get; set; } = Array.Empty<int>();

            public int SoundType { get; set; }

            public string BuiltInSoundName { get; set; } = "Classic Alarm";

            public string? LocalAudioPath { get; set; }

            public string? YouTubeUrl { get; set; }

            public double Volume { get; set; } = 1.0;

            public int SoundDurationSeconds { get; set; } = 120;

            public int SilenceDurationSeconds { get; set; } = 120;

            public bool RepeatSoundCycle { get; set; } = true;

            public bool SnoozeEnabled { get; set; } = true;

            public int SnoozeMinutes { get; set; } = 5;

            public DateTime CreatedAt { get; set; }
        }
    }
}
