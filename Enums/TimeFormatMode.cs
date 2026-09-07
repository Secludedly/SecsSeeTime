namespace SecSeeTime.Enums
{
    /// <summary>
    /// How every clock in the application renders the time of day.
    /// </summary>
    public enum TimeFormatMode
    {
        /// <summary>
        /// Defer to the active theme's own clock format setting. This is the default value.
        /// </summary>
        FollowTheme = 0,

        /// <summary>1:30 PM</summary>
        TwelveHour = 1,

        /// <summary>13:30</summary>
        TwentyFourHour = 2
    }
}
