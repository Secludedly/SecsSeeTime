using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SecSeeTime.Models;

namespace SecSeeTime.Services
{
    public sealed class ThemePersistenceService
    {
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public ThemePersistenceService(string? folder = null)
        {
            string root = folder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SecsSeeTime");
            Directory.CreateDirectory(root);
            _path = Path.Combine(root, "custom-themes.json");
        }

        public List<AlarmTheme> Load()
        {
            try
            {
                if (!File.Exists(_path)) return new();
                var records = JsonSerializer.Deserialize<List<ThemeRecord>>(File.ReadAllText(_path), Options) ?? new();
                return records.Select(ToTheme).ToList();
            }
            catch { return new(); }
        }

        public void Save(IEnumerable<AlarmTheme> themes)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllText(_path, JsonSerializer.Serialize(themes.Select(ToRecord).ToList(), Options));
            }
            catch { }
        }

        public void Export(AlarmTheme theme, string path)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(ToRecord(theme), Options));
        }

        public AlarmTheme? Import(string path)
        {
            try
            {
                return ToTheme(JsonSerializer.Deserialize<ThemeRecord>(File.ReadAllText(path), Options)
                    ?? throw new InvalidDataException("Invalid theme file."));
            }
            catch { return null; }
        }

        private static ThemeRecord ToRecord(AlarmTheme t) => new()
        {
            Name=t.Name, Description=t.Description,
            WindowBackground=H(t.WindowBackground), PanelBackground=H(t.PanelBackground), PanelBackgroundLight=H(t.PanelBackgroundLight), PanelBorder=H(t.PanelBorder),
            BackgroundGradientStart=H(t.BackgroundGradientStart), BackgroundGradientMid=H(t.BackgroundGradientMid), BackgroundGradientEnd=H(t.BackgroundGradientEnd),
            TextPrimary=H(t.TextPrimary), TextSecondary=H(t.TextSecondary), TextMuted=H(t.TextMuted),
            Accent=H(t.Accent), AccentLight=H(t.AccentLight), Success=H(t.Success), Danger=H(t.Danger),
            ClockFontFamily=t.ClockFontFamily, ClockFontSize=t.ClockFontSize,
            ClockFontWeight = t.ClockFontWeight.ToOpenTypeWeight(), ClockItalic=t.ClockItalic,
            ClockGlowEnabled=t.ClockGlowEnabled, ClockGlowStrength=t.ClockGlowStrength, ClockLetterSpacing=t.ClockLetterSpacing,
            ShowDate=t.ShowDate, ShowSeconds=t.ShowSeconds, Use24Hour=t.Use24Hour,
            ClockOpacity=t.ClockOpacity, ClockOutlineEnabled=t.ClockOutlineEnabled, ClockOutlineThickness=t.ClockOutlineThickness, ClockOutlineColor=H(t.ClockOutlineColor),
            BackgroundImagePath=t.BackgroundImagePath, BackgroundImageOpacity=t.BackgroundImageOpacity, BackgroundImageStretch=(int)t.BackgroundImageStretch,
            BackgroundOverlayColor=H(t.BackgroundOverlayColor), BackgroundOverlayOpacity=t.BackgroundOverlayOpacity,
            ScanlinesEnabled=t.ScanlinesEnabled, ScanlineOpacity=t.ScanlineOpacity, NoiseEnabled=t.NoiseEnabled, ParticlesEnabled=t.ParticlesEnabled, ParticleCount=t.ParticleCount,
            WorldClockEnabled=t.WorldClockEnabled, WorldClockOnLeft=t.WorldClockOnLeft, WorldClockFontSize=t.WorldClockFontSize, WorldClockShowDate=t.WorldClockShowDate,
            WorldClockShowZone=t.WorldClockShowZone, WorldClockUseClockFont=t.WorldClockUseClockFont, WorldClockOpacity=t.WorldClockOpacity,
            AlarmGlowCenter=H(t.AlarmGlowCenter), AlarmGlowMid=H(t.AlarmGlowMid), AlarmGlowEdge=H(t.AlarmGlowEdge), PlayingAccent=H(t.PlayingAccent), SilenceAccent=H(t.SilenceAccent)
        };

        private static AlarmTheme ToTheme(ThemeRecord r) => new()
        {
            Name=string.IsNullOrWhiteSpace(r.Name)?"Custom":r.Name, Description=r.Description ?? "Custom theme",
            WindowBackground=C(r.WindowBackground), PanelBackground=C(r.PanelBackground), PanelBackgroundLight=C(r.PanelBackgroundLight), PanelBorder=C(r.PanelBorder),
            BackgroundGradientStart=C(r.BackgroundGradientStart), BackgroundGradientMid=C(r.BackgroundGradientMid), BackgroundGradientEnd=C(r.BackgroundGradientEnd),
            TextPrimary=C(r.TextPrimary), TextSecondary=C(r.TextSecondary), TextMuted=C(r.TextMuted), Accent=C(r.Accent), AccentLight=C(r.AccentLight), Success=C(r.Success), Danger=C(r.Danger),
            ClockFontFamily=string.IsNullOrWhiteSpace(r.ClockFontFamily)?"Consolas":r.ClockFontFamily, ClockFontSize=r.ClockFontSize, ClockFontWeight=System.Windows.FontWeight.FromOpenTypeWeight(Math.Clamp(r.ClockFontWeight, 1, 999)),
            ClockItalic=r.ClockItalic, ClockGlowEnabled=r.ClockGlowEnabled, ClockGlowStrength=r.ClockGlowStrength, ClockLetterSpacing=r.ClockLetterSpacing,
            ShowDate=r.ShowDate, ShowSeconds=r.ShowSeconds, Use24Hour=r.Use24Hour, ClockOpacity=r.ClockOpacity, ClockOutlineEnabled=r.ClockOutlineEnabled, ClockOutlineThickness=r.ClockOutlineThickness, ClockOutlineColor=C(r.ClockOutlineColor),
            BackgroundImagePath=r.BackgroundImagePath, BackgroundImageOpacity=r.BackgroundImageOpacity, BackgroundImageStretch=(System.Windows.Media.Stretch)Math.Clamp(r.BackgroundImageStretch,0,4), BackgroundOverlayColor=C(r.BackgroundOverlayColor), BackgroundOverlayOpacity=r.BackgroundOverlayOpacity,
            ScanlinesEnabled=r.ScanlinesEnabled, ScanlineOpacity=r.ScanlineOpacity, NoiseEnabled=r.NoiseEnabled, ParticlesEnabled=r.ParticlesEnabled, ParticleCount=r.ParticleCount,
            WorldClockEnabled=r.WorldClockEnabled, WorldClockOnLeft=r.WorldClockOnLeft, WorldClockFontSize=Math.Clamp(r.WorldClockFontSize,10,60), WorldClockShowDate=r.WorldClockShowDate,
            WorldClockShowZone=r.WorldClockShowZone, WorldClockUseClockFont=r.WorldClockUseClockFont, WorldClockOpacity=Math.Clamp(r.WorldClockOpacity,0.2,1),
            AlarmGlowCenter=C(r.AlarmGlowCenter), AlarmGlowMid=C(r.AlarmGlowMid), AlarmGlowEdge=C(r.AlarmGlowEdge), PlayingAccent=C(r.PlayingAccent), SilenceAccent=C(r.SilenceAccent)
        };

        private static string H(System.Windows.Media.Color c) => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        private static System.Windows.Media.Color C(string? s) { try { return AlarmTheme.FromHex(string.IsNullOrWhiteSpace(s)?"#000000":s); } catch { return System.Windows.Media.Colors.Black; } }

        private sealed class ThemeRecord
        {
            public string Name {get;set;}="Custom"; public string Description {get;set;}="";
            public string WindowBackground {get;set;}="#090B12"; public string PanelBackground {get;set;}="#121622"; public string PanelBackgroundLight {get;set;}="#181D2B"; public string PanelBorder {get;set;}="#2A3144";
            public string BackgroundGradientStart {get;set;}="#080A11"; public string BackgroundGradientMid {get;set;}="#101225"; public string BackgroundGradientEnd {get;set;}="#090B14";
            public string TextPrimary {get;set;}="#F4F7FF"; public string TextSecondary {get;set;}="#9CA6BA"; public string TextMuted {get;set;}="#697388";
            public string Accent {get;set;}="#8B7CFF"; public string AccentLight {get;set;}="#B1A8FF"; public string Success {get;set;}="#55D6A7"; public string Danger {get;set;}="#FF6685";
            public string ClockFontFamily {get;set;}="Consolas"; public double ClockFontSize {get;set;}=104; public int ClockFontWeight {get;set;}=700; public bool ClockItalic {get;set;}=false;
            public bool ClockGlowEnabled {get;set;}=true; public double ClockGlowStrength {get;set;}=.4; public double ClockLetterSpacing {get;set;}=0; public bool ShowDate {get;set;}=true; public bool ShowSeconds {get;set;}=true; public bool Use24Hour {get;set;}=false;
            public double ClockOpacity {get;set;}=1; public bool ClockOutlineEnabled {get;set;}=false; public double ClockOutlineThickness {get;set;}=1; public string ClockOutlineColor {get;set;}="#FFFFFF";
            public string? BackgroundImagePath {get;set;}=null; public double BackgroundImageOpacity {get;set;}=0; public int BackgroundImageStretch {get;set;}=0; public string BackgroundOverlayColor {get;set;}="#000000"; public double BackgroundOverlayOpacity {get;set;}=0;
            public bool ScanlinesEnabled {get;set;}=false; public double ScanlineOpacity {get;set;}=.08; public bool NoiseEnabled {get;set;}=false; public bool ParticlesEnabled {get;set;}=true; public int ParticleCount {get;set;}=45;
            public bool WorldClockEnabled {get;set;}=true; public bool WorldClockOnLeft {get;set;}=false; public double WorldClockFontSize {get;set;}=21; public bool WorldClockShowDate {get;set;}=true;
            public bool WorldClockShowZone {get;set;}=false; public bool WorldClockUseClockFont {get;set;}=false; public double WorldClockOpacity {get;set;}=1;
            public string AlarmGlowCenter {get;set;}="#28134A"; public string AlarmGlowMid {get;set;}="#100C22"; public string AlarmGlowEdge {get;set;}="#05050B"; public string PlayingAccent {get;set;}="#FF8FE7"; public string SilenceAccent {get;set;}="#8FD9FF";
        }
    }
}
