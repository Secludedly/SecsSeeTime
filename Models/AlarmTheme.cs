using System;
using System.Windows.Media;
using System.Windows;

namespace SecSeeTime.Models
{
    /// <summary>
    /// Appearance, fully separated from behavior.
    ///
    /// Nothing in the alarm, audio or time engines reads this type.
    /// The clock surfaces and the alarm screen render themselves from
    /// it, which is what lets the Appearance Studio grow later without
    /// touching a single line of alarm code.
    /// </summary>
    public sealed class AlarmTheme
    {
        public string Name { get; set; } = "Midnight";

        public string Description { get; set; } = "";


        // =============================================================
        // SURFACES
        // =============================================================

        public Color WindowBackground { get; set; } =
            FromHex("#090B12");

        public Color PanelBackground { get; set; } =
            FromHex("#121622");

        public Color PanelBackgroundLight { get; set; } =
            FromHex("#181D2B");

        public Color PanelBorder { get; set; } =
            FromHex("#2A3144");


        // =============================================================
        // BACKGROUND GRADIENT
        // =============================================================

        public Color BackgroundGradientStart { get; set; } =
            FromHex("#080A11");

        public Color BackgroundGradientMid { get; set; } =
            FromHex("#101225");

        public Color BackgroundGradientEnd { get; set; } =
            FromHex("#090B14");


        // =============================================================
        // TEXT
        // =============================================================

        public Color TextPrimary { get; set; } =
            FromHex("#F4F7FF");

        public Color TextSecondary { get; set; } =
            FromHex("#9CA6BA");

        public Color TextMuted { get; set; } =
            FromHex("#697388");


        // =============================================================
        // ACCENTS
        // =============================================================

        public Color Accent { get; set; } =
            FromHex("#8B7CFF");

        public Color AccentLight { get; set; } =
            FromHex("#B1A8FF");

        public Color Success { get; set; } =
            FromHex("#55D6A7");

        public Color Danger { get; set; } =
            FromHex("#FF6685");


        // =============================================================
        // CLOCK
        // =============================================================

        public string ClockFontFamily { get; set; } = "Consolas";

        public double ClockFontSize { get; set; } = 104;

        public FontWeight ClockFontWeight { get; set; } = FontWeights.Bold;

        public bool ClockItalic { get; set; }

        public double ClockLetterSpacing { get; set; }

        public bool ShowDate { get; set; } = true;

        public bool ShowSeconds { get; set; } = true;

        public bool Use24Hour { get; set; }

        public double ClockOpacity { get; set; } = 1.0;

        public bool ClockOutlineEnabled { get; set; }

        public double ClockOutlineThickness { get; set; } = 1.0;

        public Color ClockOutlineColor { get; set; } = Colors.White;

        public bool ClockGlowEnabled { get; set; } = true;

        public double ClockGlowStrength { get; set; } = 0.40;


        // =============================================================
        // WORLD CLOCK RAIL
        //
        // The secondary clocks beside the main clock. Their colors come
        // from the palette like every other surface; these control the
        // rail's shape and what each card shows.
        // =============================================================

        public bool WorldClockEnabled { get; set; } = true;

        public bool WorldClockOnLeft { get; set; }

        public double WorldClockFontSize { get; set; } = 21;

        public bool WorldClockShowDate { get; set; } = true;

        public bool WorldClockShowZone { get; set; }

        public bool WorldClockUseClockFont { get; set; }

        public double WorldClockOpacity { get; set; } = 1.0;


        // =============================================================
        // BACKGROUND / EFFECTS
        // =============================================================

        public string? BackgroundImagePath { get; set; }

        public double BackgroundImageOpacity { get; set; }

        public Stretch BackgroundImageStretch { get; set; } = Stretch.UniformToFill;

        public Color BackgroundOverlayColor { get; set; } = Colors.Black;

        public double BackgroundOverlayOpacity { get; set; }

        public bool ScanlinesEnabled { get; set; }

        public double ScanlineOpacity { get; set; } = 0.08;

        public bool NoiseEnabled { get; set; }

        // =============================================================
        // ALARM SCREEN
        // =============================================================

        public Color AlarmGlowCenter { get; set; } =
            FromHex("#28134A");

        public Color AlarmGlowMid { get; set; } =
            FromHex("#100C22");

        public Color AlarmGlowEdge { get; set; } =
            FromHex("#05050B");

        public Color PlayingAccent { get; set; } =
            FromHex("#FF8FE7");

        public Color SilenceAccent { get; set; } =
            FromHex("#8FD9FF");


        // =============================================================
        // EFFECTS
        // =============================================================

        public bool ParticlesEnabled { get; set; } = true;

        public int ParticleCount { get; set; } = 45;


        // =============================================================
        // HELPERS
        // =============================================================

        public static Color FromHex(string hex)
        {
            object converted =
                ColorConverter.ConvertFromString(hex)
                ?? throw new ArgumentException(
                    $"'{hex}' is not a color.",
                    nameof(hex));

            return (Color)converted;
        }


        public AlarmTheme Clone()
        {
            return (AlarmTheme)MemberwiseClone();
        }
    }
}
