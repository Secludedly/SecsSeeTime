using SecSeeTime.Enums;

namespace SecSeeTime.Services
{
    /// <summary>
    /// The single authority on 12- versus 24-hour display.
    ///
    /// Every clock in the application resolves its format through here rather
    /// than reading a theme or formatting inline, which is what previously let
    /// the main clock show "6:58 PM" while the alarm list showed "19:00".
    ///
    /// Ambient static state, matching <see cref="ThemeService"/>: the format is
    /// a property of the whole application, and threading it through every
    /// window and data template would buy nothing.
    /// </summary>
    public static class TimeFormatService
    {
        /// <summary>
        /// Current preference. Set from saved settings at startup and whenever
        /// the Settings window changes it.
        /// </summary>
        public static TimeFormatMode Mode { get; set; } =
            TimeFormatMode.TwelveHour;


        /// <summary>
        /// Resolves the effective 24-hour flag. <paramref name="themeUse24Hour"/>
        /// is only consulted in <see cref="TimeFormatMode.FollowTheme"/>.
        /// </summary>
        public static bool Use24Hour(bool themeUse24Hour) =>
            Mode switch
            {
                TimeFormatMode.TwelveHour => false,

                TimeFormatMode.TwentyFourHour => true,

                _ => themeUse24Hour
            };


        /// <summary>
        /// The effective 24-hour flag for the theme that is currently active.
        /// Use this from anywhere that does not already have a theme in hand.
        /// </summary>
        public static bool Use24HourNow =>
            Mode switch
            {
                TimeFormatMode.TwelveHour => false,

                TimeFormatMode.TwentyFourHour => true,

                // Only reached in FollowTheme, so a global override never
                // depends on the theme system being up.
                _ => ThemeService.Current.Theme.Use24Hour
            };
    }
}
