using SecSeeTime.Helpers;
using SecSeeTime.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SecSeeTime.ViewModels
{
    /// <summary>
    /// One live world clock.
    ///
    /// Shared by the World Clock window and the main window's secondary
    /// clock rail so both read the same entries and format them the same
    /// way. The zone is resolved once, at construction.
    /// </summary>
    public sealed class WorldClockTile : INotifyPropertyChanged
    {
        private readonly TimeZoneInfo? _zone;

        public WorldClockTile(WorldClockEntry entry)
        {
            Entry = entry;

            _zone =
                TimeZoneInfo.TryFindSystemTimeZoneById(entry.TimeZoneId, out var z)
                    ? z
                    : null;
        }


        /// <summary>The saved entry this tile renders.</summary>
        public WorldClockEntry Entry { get; }

        public string DisplayName => Entry.DisplayName;

        public string TimeZoneId => Entry.TimeZoneId;

        public bool IsFavorite => Entry.IsFavorite;


        private string _timeText = "--:--:--";
        public string TimeText
        {
            get => _timeText;
            private set => Set(ref _timeText, value);
        }


        private string _dateText = "--";
        public string DateText
        {
            get => _dateText;
            private set => Set(ref _dateText, value);
        }


        private string _metaText = "--";
        public string MetaText
        {
            get => _metaText;
            private set => Set(ref _metaText, value);
        }


        /// <summary>
        /// Recomputes this tile against the given UTC instant.
        /// </summary>
        public void Update(
            DateTime utc,
            bool use24Hour,
            bool showSeconds = true)
        {
            if (_zone == null)
            {
                TimeText = "--:--:--";
                DateText = "Unknown time zone";
                MetaText = TimeZoneId;

                return;
            }

            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utc, _zone);

            TimeText = TimeHelper.FormatClock(local, use24Hour, showSeconds);
            DateText = TimeHelper.FormatDate(local);

            TimeSpan offset = _zone.GetUtcOffset(utc);
            string sign = offset < TimeSpan.Zero ? "-" : "+";

            MetaText =
                $"{(_zone.IsDaylightSavingTime(local) ? "DST" : "STANDARD")} • " +
                $"UTC{sign}{offset.Duration():hh\\:mm}";
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set(
            ref string field,
            string value,
            [CallerMemberName] string? name = null)
        {
            if (field == value)
                return;

            field = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name!));
        }
    }
}
