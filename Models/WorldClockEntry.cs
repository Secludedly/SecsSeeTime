using System;

namespace SecSeeTime.Models
{
    public sealed class WorldClockEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TimeZoneId { get; set; } = TimeZoneInfo.Utc.Id;
        public string DisplayName { get; set; } = "UTC";
        public bool IsFavorite { get; set; }
    }
}
