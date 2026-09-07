using System;
using System.Windows;
using System.Windows.Media;
using SecSeeTime.Models;
using SecSeeTime.Themes;

namespace SecSeeTime.Services
{
    /// <summary>
    /// The Theme Engine.
    ///
    /// It owns the single source of truth for appearance and pushes it
    /// into the application resource dictionary. Views bind the simple
    /// values with DynamicResource and handle <see cref="ThemeChanged"/>
    /// for anything a resource lookup cannot express, such as gradients
    /// and drop-shadow effects.
    /// </summary>
    public sealed class ThemeService
    {
        private static readonly Lazy<ThemeService> Instance =
            new(() => new ThemeService());

        public static ThemeService Current => Instance.Value;


        private ThemeService()
        {
            Theme = ThemePresets.Midnight();
        }


        // =============================================================
        // STATE
        // =============================================================

        public AlarmTheme Theme { get; private set; }


        public event EventHandler<AlarmTheme>? ThemeChanged;


        // =============================================================
        // APPLY
        // =============================================================

        public void ApplyByName(string? name)
        {
            Apply(ThemePresets.Get(name));
        }


        public void Apply(AlarmTheme theme)
        {
            ArgumentNullException.ThrowIfNull(theme);

            Theme = theme;

            PushResources(theme);

            ThemeChanged?.Invoke(this, theme);
        }


        /// <summary>
        /// Re-raises the current theme so a newly created window can
        /// style itself without duplicating the apply logic.
        /// </summary>
        public void Refresh()
        {
            PushResources(Theme);

            ThemeChanged?.Invoke(this, Theme);
        }


        // =============================================================
        // RESOURCE DICTIONARY
        // =============================================================

        private static void PushResources(AlarmTheme theme)
        {
            Application? app = Application.Current;

            if (app == null)
                return;

            ResourceDictionary resources = app.Resources;

            SetBrush(resources, "ThemeWindowBackground", theme.WindowBackground);
            SetBrush(resources, "ThemePanelBackground", theme.PanelBackground);
            SetBrush(resources, "ThemePanelBackgroundLight", theme.PanelBackgroundLight);
            SetBrush(resources, "ThemePanelBorder", theme.PanelBorder);

            SetBrush(resources, "ThemeTextPrimary", theme.TextPrimary);
            SetBrush(resources, "ThemeTextSecondary", theme.TextSecondary);
            SetBrush(resources, "ThemeTextMuted", theme.TextMuted);

            SetBrush(resources, "ThemeAccent", theme.Accent);
            SetBrush(resources, "ThemeAccentLight", theme.AccentLight);
            SetBrush(resources, "ThemeSuccess", theme.Success);
            SetBrush(resources, "ThemeDanger", theme.Danger);

            SetBrush(resources, "ThemePlayingAccent", theme.PlayingAccent);
            SetBrush(resources, "ThemeSilenceAccent", theme.SilenceAccent);

            resources["ThemeClockFontFamily"] =
                new FontFamily(theme.ClockFontFamily);

            resources["ThemeClockFontSize"] =
                theme.ClockFontSize;

            // World clock rail. The cards live inside a DataTemplate, so
            // these ride in as resources rather than being poked directly.
            resources["ThemeWorldClockFontFamily"] =
                new FontFamily(
                    theme.WorldClockUseClockFont
                        ? theme.ClockFontFamily
                        : "Consolas");

            resources["ThemeWorldClockFontSize"] =
                theme.WorldClockFontSize;

            resources["ThemeWorldClockDateVisibility"] =
                theme.WorldClockShowDate
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            resources["ThemeWorldClockZoneVisibility"] =
                theme.WorldClockShowZone
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }


        private static void SetBrush(
            ResourceDictionary resources,
            string key,
            Color color)
        {
            SolidColorBrush brush =
                new SolidColorBrush(color);

            brush.Freeze();

            resources[key] = brush;
        }


        // =============================================================
        // DERIVED BRUSHES
        // =============================================================

        /// <summary>
        /// The main window's atmospheric background gradient.
        /// Built in code because gradient stops cannot follow a
        /// DynamicResource once the brush is frozen.
        /// </summary>
        public static Brush BuildWindowGradient(AlarmTheme theme)
        {
            LinearGradientBrush brush =
                new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };

            brush.GradientStops.Add(
                new GradientStop(theme.BackgroundGradientStart, 0));

            brush.GradientStops.Add(
                new GradientStop(theme.BackgroundGradientMid, 0.45));

            brush.GradientStops.Add(
                new GradientStop(theme.BackgroundGradientEnd, 1));

            brush.Freeze();

            return brush;
        }


        /// <summary>
        /// The alarm screen's radial wash.
        /// </summary>
        public static Brush BuildAlarmGradient(AlarmTheme theme)
        {
            RadialGradientBrush brush =
                new RadialGradientBrush
                {
                    Center = new Point(0.5, 0.35),
                    GradientOrigin = new Point(0.5, 0.35),
                    RadiusX = 0.85,
                    RadiusY = 0.85
                };

            brush.GradientStops.Add(
                new GradientStop(theme.AlarmGlowCenter, 0));

            brush.GradientStops.Add(
                new GradientStop(theme.AlarmGlowMid, 0.48));

            brush.GradientStops.Add(
                new GradientStop(theme.AlarmGlowEdge, 1));

            brush.Freeze();

            return brush;
        }


        /// <summary>
        /// The clock's glow, or null when the theme disables it.
        /// </summary>
        public static System.Windows.Media.Effects.DropShadowEffect? BuildClockGlow(
            AlarmTheme theme)
        {
            if (!theme.ClockGlowEnabled ||
                theme.ClockGlowStrength <= 0)
            {
                return null;
            }

            return new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 28,
                ShadowDepth = 0,
                Opacity = theme.ClockGlowStrength,
                Color = theme.Accent
            };
        }
    }
}
