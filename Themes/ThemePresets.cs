using System.Collections.Generic;
using SecSeeTime.Models;

namespace SecSeeTime.Themes
{
    /// <summary>
    /// The starter set of themes.
    ///
    /// These are ordinary <see cref="AlarmTheme"/> instances with no
    /// special status, which is the point: the Appearance Studio will
    /// produce and save exactly the same kind of object.
    /// </summary>
    public static class ThemePresets
    {
        public const string DefaultThemeName = "Midnight";


        public static IReadOnlyList<AlarmTheme> All { get; } =
            new List<AlarmTheme>
            {
                Midnight(),
                Neon(),
                Ember(),
                Minimal()
            };


        public static AlarmTheme Get(string? name)
        {
            foreach (AlarmTheme theme in All)
            {
                if (string.Equals(
                        theme.Name,
                        name,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return theme.Clone();
                }
            }

            return Midnight();
        }


        // =============================================================
        // MIDNIGHT — the original Sec's See Time look
        // =============================================================

        public static AlarmTheme Midnight()
        {
            return new AlarmTheme
            {
                Name = "Midnight",
                Description = "Deep violet, soft glow. The original."
            };
        }


        // =============================================================
        // NEON
        // =============================================================

        public static AlarmTheme Neon()
        {
            return new AlarmTheme
            {
                Name = "Neon",
                Description = "Hard cyan on black. Loud and awake.",

                WindowBackground = AlarmTheme.FromHex("#04070A"),
                PanelBackground = AlarmTheme.FromHex("#08131A"),
                PanelBackgroundLight = AlarmTheme.FromHex("#0C1D27"),
                PanelBorder = AlarmTheme.FromHex("#12414F"),

                BackgroundGradientStart = AlarmTheme.FromHex("#03080B"),
                BackgroundGradientMid = AlarmTheme.FromHex("#04202B"),
                BackgroundGradientEnd = AlarmTheme.FromHex("#03070A"),

                TextPrimary = AlarmTheme.FromHex("#E8FEFF"),
                TextSecondary = AlarmTheme.FromHex("#79B9C6"),
                TextMuted = AlarmTheme.FromHex("#487681"),

                Accent = AlarmTheme.FromHex("#22E0FF"),
                AccentLight = AlarmTheme.FromHex("#8CF2FF"),
                Success = AlarmTheme.FromHex("#3BFFB0"),
                Danger = AlarmTheme.FromHex("#FF4D6D"),

                ClockFontSize = 112,
                ClockGlowStrength = 0.75,

                AlarmGlowCenter = AlarmTheme.FromHex("#063846"),
                AlarmGlowMid = AlarmTheme.FromHex("#04141C"),
                AlarmGlowEdge = AlarmTheme.FromHex("#010507"),

                PlayingAccent = AlarmTheme.FromHex("#22E0FF"),
                SilenceAccent = AlarmTheme.FromHex("#3BFFB0"),

                ParticleCount = 70
            };
        }


        // =============================================================
        // EMBER
        // =============================================================

        public static AlarmTheme Ember()
        {
            return new AlarmTheme
            {
                Name = "Ember",
                Description = "Warm amber. Easier on the eyes at 3am.",

                WindowBackground = AlarmTheme.FromHex("#0E0906"),
                PanelBackground = AlarmTheme.FromHex("#1A1109"),
                PanelBackgroundLight = AlarmTheme.FromHex("#231710"),
                PanelBorder = AlarmTheme.FromHex("#40291A"),

                BackgroundGradientStart = AlarmTheme.FromHex("#0C0705"),
                BackgroundGradientMid = AlarmTheme.FromHex("#231208"),
                BackgroundGradientEnd = AlarmTheme.FromHex("#0B0604"),

                TextPrimary = AlarmTheme.FromHex("#FFF3E4"),
                TextSecondary = AlarmTheme.FromHex("#C6A184"),
                TextMuted = AlarmTheme.FromHex("#856A54"),

                Accent = AlarmTheme.FromHex("#FF9540"),
                AccentLight = AlarmTheme.FromHex("#FFC18A"),
                Success = AlarmTheme.FromHex("#B6D65A"),
                Danger = AlarmTheme.FromHex("#FF5A48"),

                ClockGlowStrength = 0.5,

                AlarmGlowCenter = AlarmTheme.FromHex("#4A2109"),
                AlarmGlowMid = AlarmTheme.FromHex("#1F1006"),
                AlarmGlowEdge = AlarmTheme.FromHex("#080402"),

                PlayingAccent = AlarmTheme.FromHex("#FFB259"),
                SilenceAccent = AlarmTheme.FromHex("#C6A184"),

                ParticleCount = 35
            };
        }


        // =============================================================
        // MINIMAL
        // =============================================================

        public static AlarmTheme Minimal()
        {
            return new AlarmTheme
            {
                Name = "Minimal",
                Description = "No glow, no particles. Just the time.",

                WindowBackground = AlarmTheme.FromHex("#0B0B0D"),
                PanelBackground = AlarmTheme.FromHex("#131316"),
                PanelBackgroundLight = AlarmTheme.FromHex("#1A1A1E"),
                PanelBorder = AlarmTheme.FromHex("#2B2B31"),

                BackgroundGradientStart = AlarmTheme.FromHex("#0B0B0D"),
                BackgroundGradientMid = AlarmTheme.FromHex("#101013"),
                BackgroundGradientEnd = AlarmTheme.FromHex("#0B0B0D"),

                TextPrimary = AlarmTheme.FromHex("#FFFFFF"),
                TextSecondary = AlarmTheme.FromHex("#9A9AA4"),
                TextMuted = AlarmTheme.FromHex("#5F5F68"),

                Accent = AlarmTheme.FromHex("#E4E4EA"),
                AccentLight = AlarmTheme.FromHex("#FFFFFF"),
                Success = AlarmTheme.FromHex("#8FBF9F"),
                Danger = AlarmTheme.FromHex("#D07A85"),

                ClockFontFamily = "Segoe UI Light",
                ClockFontSize = 118,
                ClockGlowEnabled = false,
                ClockGlowStrength = 0.0,

                AlarmGlowCenter = AlarmTheme.FromHex("#17171B"),
                AlarmGlowMid = AlarmTheme.FromHex("#101013"),
                AlarmGlowEdge = AlarmTheme.FromHex("#050506"),

                PlayingAccent = AlarmTheme.FromHex("#FFFFFF"),
                SilenceAccent = AlarmTheme.FromHex("#9A9AA4"),

                ParticlesEnabled = false,
                ParticleCount = 0
            };
        }
    }
}
