using Microsoft.Win32;
using SecSeeTime.Helpers;
using SecSeeTime.Models;
using SecSeeTime.Services;
using SecSeeTime.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SecSeeTime.Views
{
    public partial class AppearanceStudioWindow : Window
    {
        private readonly ThemePersistenceService _store = new();
        private readonly List<AlarmTheme> _saved = new();
        private AlarmTheme _editing = null!;
        // Starts muted: sliders with a non-zero Minimum (OpacitySlider,
        // OutlineSlider) coerce their Value and raise ValueChanged while
        // InitializeComponent is still parsing the XAML, before the
        // TextBlocks those handlers write to have been created.
        private bool _loading = true;

        public AppearanceStudioWindow()
        {
            InitializeComponent();

            // Populating the controls raises SelectionChanged before there
            // is a theme to edit, so stay muted until BeginEdit hands the
            // handlers a live _editing instance.
            FontCombo.ItemsSource = System.Windows.Media.Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList();
            ImageStretchCombo.SelectedIndex = 0;
            LoadThemeList();

            BeginEdit(ThemeService.Current.Theme.Clone());
        }

        private void LoadThemeList()
        {
            _saved.Clear(); _saved.AddRange(_store.Load());
            ThemeCombo.Items.Clear();
            foreach (var t in ThemePresets.All) ThemeCombo.Items.Add(new ThemeChoice(t, "Preset"));
            foreach (var t in _saved) ThemeCombo.Items.Add(new ThemeChoice(t, "Custom"));
            ThemeCombo.SelectedIndex = 0;
        }

        private void BeginEdit(AlarmTheme theme)
        {
            _loading = true; _editing = theme;
            ThemeNameBox.Text = theme.Name; ThemeDescriptionBox.Text = theme.Description;
            FontCombo.SelectedItem = System.Windows.Media.Fonts.SystemFontFamilies.FirstOrDefault(f => string.Equals(f.Source, theme.ClockFontFamily, StringComparison.OrdinalIgnoreCase)) ?? SystemFonts.MessageFontFamily;
            FontSizeSlider.Value = theme.ClockFontSize; SpacingSlider.Value = theme.ClockLetterSpacing; OpacitySlider.Value = theme.ClockOpacity;
            WeightCombo.SelectedItem = WeightCombo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (int.Parse((string)i.Tag)) == theme.ClockFontWeight.ToOpenTypeWeight()) ?? WeightCombo.Items[4];
            ShowDateBox.IsChecked=theme.ShowDate; ShowSecondsBox.IsChecked=theme.ShowSeconds; Use24Box.IsChecked=theme.Use24Hour; ItalicBox.IsChecked=theme.ClockItalic;
            ClockColorBox.Text=H(theme.TextPrimary); WindowColorBox.Text=H(theme.WindowBackground); PanelColorBox.Text=H(theme.PanelBackground); PanelLightColorBox.Text=H(theme.PanelBackgroundLight); PanelBorderColorBox.Text=H(theme.PanelBorder); TextSecondaryColorBox.Text=H(theme.TextSecondary); TextMutedColorBox.Text=H(theme.TextMuted); AccentColorBox.Text=H(theme.Accent); AccentLightColorBox.Text=H(theme.AccentLight); SuccessColorBox.Text=H(theme.Success); DangerColorBox.Text=H(theme.Danger); PlayingColorBox.Text=H(theme.PlayingAccent); SilenceColorBox.Text=H(theme.SilenceAccent); GradientStartBox.Text=H(theme.BackgroundGradientStart); GradientMidBox.Text=H(theme.BackgroundGradientMid); GradientEndBox.Text=H(theme.BackgroundGradientEnd);
            ImagePathBox.Text=theme.BackgroundImagePath ?? ""; ImageOpacitySlider.Value=theme.BackgroundImageOpacity; ImageStretchCombo.SelectedIndex=theme.BackgroundImageStretch switch { Stretch.Uniform=>1, Stretch.Fill=>2, Stretch.None=>3, _=>0 };
            OverlayColorBox.Text=H(theme.BackgroundOverlayColor); OverlaySlider.Value=theme.BackgroundOverlayOpacity;
            GlowBox.IsChecked=theme.ClockGlowEnabled; GlowSlider.Value=theme.ClockGlowStrength; OutlineBox.IsChecked=theme.ClockOutlineEnabled; OutlineSlider.Value=theme.ClockOutlineThickness; OutlineColorBox.Text=H(theme.ClockOutlineColor);
            ParticlesBox.IsChecked=theme.ParticlesEnabled; ParticleSlider.Value=theme.ParticleCount; ScanlinesBox.IsChecked=theme.ScanlinesEnabled; NoiseBox.IsChecked=theme.NoiseEnabled;
            WorldClockShowBox.IsChecked=theme.WorldClockEnabled; WorldClockLeftBox.IsChecked=theme.WorldClockOnLeft; WorldClockDateBox.IsChecked=theme.WorldClockShowDate; WorldClockZoneBox.IsChecked=theme.WorldClockShowZone; WorldClockFontBox.IsChecked=theme.WorldClockUseClockFont;
            WorldClockSizeSlider.Value=theme.WorldClockFontSize; WorldClockSizeValue.Text=$"{(int)theme.WorldClockFontSize}"; WorldClockOpacitySlider.Value=theme.WorldClockOpacity; WorldClockOpacityValue.Text=$"{theme.WorldClockOpacity:P0}";
            _loading=false; ApplyEditing(false); UpdateStatus("Editing live theme");
        }

        private void ApplyEditing(bool status=true)
        {
            if (_loading || _editing is null) return;
            ThemeService.Current.Apply(_editing.Clone());
            // Preview through the same resolver the real clock uses, so it
            // cannot promise a format the global setting overrides.
            PreviewClock.Text = TimeHelper.FormatClock(
                DateTime.Now,
                TimeFormatService.Use24Hour(_editing.Use24Hour),
                _editing.ShowSeconds);
            if (status) UpdateStatus("Applied live");
        }

        private void ReadControlsIntoTheme()
        {
            if (_editing is null) return;

            _editing.Name =
                string.IsNullOrWhiteSpace(ThemeNameBox.Text)
                    ? "Custom"
                    : ThemeNameBox.Text.Trim();

            _editing.Description = ThemeDescriptionBox.Text.Trim();

            // Clock font
            if (FontCombo.SelectedItem is FontFamily ff)
                _editing.ClockFontFamily = ff.Source;

            _editing.ClockFontSize = FontSizeSlider.Value;
            _editing.ClockLetterSpacing = SpacingSlider.Value;
            _editing.ClockOpacity = OpacitySlider.Value;

            if (WeightCombo.SelectedItem is ComboBoxItem wi &&
                int.TryParse(wi.Tag?.ToString(), out int weight))
            {
                _editing.ClockFontWeight =
                    FontWeight.FromOpenTypeWeight(weight);
            }

            _editing.ShowDate = ShowDateBox.IsChecked == true;
            _editing.ShowSeconds = ShowSecondsBox.IsChecked == true;
            _editing.Use24Hour = Use24Box.IsChecked == true;
            _editing.ClockItalic = ItalicBox.IsChecked == true;

            // Main colors
            TryColor(ClockColorBox.Text,
                c => _editing.TextPrimary = c);

            TryColor(WindowColorBox.Text,
                c => _editing.WindowBackground = c);

            TryColor(PanelColorBox.Text,
                c => _editing.PanelBackground = c);

            TryColor(PanelLightColorBox.Text,
                c => _editing.PanelBackgroundLight = c);

            TryColor(PanelBorderColorBox.Text,
                c => _editing.PanelBorder = c);

            TryColor(TextSecondaryColorBox.Text,
                c => _editing.TextSecondary = c);

            TryColor(TextMutedColorBox.Text,
                c => _editing.TextMuted = c);

            TryColor(AccentColorBox.Text,
                c => _editing.Accent = c);

            TryColor(AccentLightColorBox.Text,
                c => _editing.AccentLight = c);

            TryColor(SuccessColorBox.Text,
                c => _editing.Success = c);

            TryColor(DangerColorBox.Text,
                c => _editing.Danger = c);

            TryColor(PlayingColorBox.Text,
                c => _editing.PlayingAccent = c);

            TryColor(SilenceColorBox.Text,
                c => _editing.SilenceAccent = c);

            // Gradient
            TryColor(GradientStartBox.Text,
                c => _editing.BackgroundGradientStart = c);

            TryColor(GradientMidBox.Text,
                c => _editing.BackgroundGradientMid = c);

            TryColor(GradientEndBox.Text,
                c => _editing.BackgroundGradientEnd = c);

            // Background image
            _editing.BackgroundImagePath =
                string.IsNullOrWhiteSpace(ImagePathBox.Text)
                    ? null
                    : ImagePathBox.Text.Trim();

            _editing.BackgroundImageOpacity =
                ImageOpacitySlider.Value;

            _editing.BackgroundImageStretch =
                ImageStretchCombo.SelectedIndex switch
                {
                    1 => Stretch.Uniform,
                    2 => Stretch.Fill,
                    3 => Stretch.None,
                    _ => Stretch.UniformToFill
                };

            // Background overlay
            TryColor(
                OverlayColorBox.Text,
                c => _editing.BackgroundOverlayColor = c);

            _editing.BackgroundOverlayOpacity =
                OverlaySlider.Value;

            // Clock effects
            _editing.ClockGlowEnabled =
                GlowBox.IsChecked == true;

            _editing.ClockGlowStrength =
                GlowSlider.Value;

            _editing.ClockOutlineEnabled =
                OutlineBox.IsChecked == true;

            _editing.ClockOutlineThickness =
                OutlineSlider.Value;

            TryColor(
                OutlineColorBox.Text,
                c => _editing.ClockOutlineColor = c);

            // Background effects
            _editing.ParticlesEnabled =
                ParticlesBox.IsChecked == true;

            _editing.ParticleCount =
                (int)Math.Round(ParticleSlider.Value);

            _editing.ScanlinesEnabled =
                ScanlinesBox.IsChecked == true;

            _editing.NoiseEnabled =
                NoiseBox.IsChecked == true;

            // World clock rail
            _editing.WorldClockEnabled =
                WorldClockShowBox.IsChecked == true;

            _editing.WorldClockOnLeft =
                WorldClockLeftBox.IsChecked == true;

            _editing.WorldClockShowDate =
                WorldClockDateBox.IsChecked == true;

            _editing.WorldClockShowZone =
                WorldClockZoneBox.IsChecked == true;

            _editing.WorldClockUseClockFont =
                WorldClockFontBox.IsChecked == true;

            _editing.WorldClockFontSize =
                WorldClockSizeSlider.Value;

            _editing.WorldClockOpacity =
                WorldClockOpacitySlider.Value;
        }

        private void UpdateStatus(string s)=>StatusText.Text=s.ToUpperInvariant();
        private static string H(Color c)=>$"#{c.R:X2}{c.G:X2}{c.B:X2}";
        private static bool TryColor(string text, Action<Color> set){try{set(AlarmTheme.FromHex(text));return true;}catch{return false;}}

        private void Color_TextChanged(object s, TextChangedEventArgs e){if(!_loading){ReadControlsIntoTheme();ApplyEditing(false);}}
        private void ThemeNameBox_TextChanged(object s, TextChangedEventArgs e){if(!_loading) {ReadControlsIntoTheme();}}
        private void ThemeDescriptionBox_TextChanged(object s, TextChangedEventArgs e){if(!_loading) {ReadControlsIntoTheme();}}
        private void FontCombo_SelectionChanged(object s, SelectionChangedEventArgs e){if(!_loading){ReadControlsIntoTheme();ApplyEditing(false);}}
        private void WeightCombo_SelectionChanged(object s, SelectionChangedEventArgs e){if(!_loading){ReadControlsIntoTheme();ApplyEditing(false);}}
        private void FontSizeSlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e){if(!_loading){FontSizeValue.Text=$"{(int)e.NewValue}";ReadControlsIntoTheme();ApplyEditing(false);}}
        private void SpacingSlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e){if(!_loading){ReadControlsIntoTheme();ApplyEditing(false);}}
        private void OpacitySlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e){if(!_loading){OpacityValue.Text=$"{e.NewValue:P0}";ReadControlsIntoTheme();ApplyEditing(false);}}
        private void ImageOpacitySlider_ValueChanged(object s,RoutedPropertyChangedEventArgs<double> e){if(!_loading){ImageOpacityValue.Text=$"{e.NewValue:P0}";ReadControlsIntoTheme();ApplyEditing(false);}}
        private void OverlaySlider_ValueChanged(object s,RoutedPropertyChangedEventArgs<double> e){if(!_loading){OverlayValue.Text=$"{e.NewValue:P0}";ReadControlsIntoTheme();ApplyEditing(false);}}
        private void GlowSlider_ValueChanged(object s,RoutedPropertyChangedEventArgs<double> e){if(!_loading){GlowValue.Text=$"{e.NewValue:P0}";ReadControlsIntoTheme();ApplyEditing(false);}}
        private void OutlineSlider_ValueChanged(object s,RoutedPropertyChangedEventArgs<double> e){if(!_loading){OutlineValue.Text=$"{e.NewValue:0.0}";ReadControlsIntoTheme();ApplyEditing(false);}}
        private void ParticleSlider_ValueChanged(object s,RoutedPropertyChangedEventArgs<double> e){if(!_loading){ParticleValue.Text=$"{(int)e.NewValue}";ReadControlsIntoTheme();ApplyEditing(false);}}
        private void WorldClockSizeSlider_ValueChanged(object s,RoutedPropertyChangedEventArgs<double> e){if(!_loading){WorldClockSizeValue.Text=$"{(int)e.NewValue}";ReadControlsIntoTheme();ApplyEditing(false);}}
        private void WorldClockOpacitySlider_ValueChanged(object s,RoutedPropertyChangedEventArgs<double> e){if(!_loading){WorldClockOpacityValue.Text=$"{e.NewValue:P0}";ReadControlsIntoTheme();ApplyEditing(false);}}
        private void AppearanceOption_Click(object s,RoutedEventArgs e){if(!_loading){ReadControlsIntoTheme();ApplyEditing(false);}}
        private void ImageStretchCombo_SelectionChanged(object s,SelectionChangedEventArgs e){if(!_loading){ReadControlsIntoTheme();ApplyEditing(false);}}
        private void ImagePathBox_TextChanged(object s,TextChangedEventArgs e){if(!_loading){ReadControlsIntoTheme();ApplyEditing(false);}}

        private void ThemeCombo_SelectionChanged(object s,SelectionChangedEventArgs e) { }
        private void LoadSelected_Click(object s,RoutedEventArgs e){if(ThemeCombo.SelectedItem is ThemeChoice c) BeginEdit(c.Theme.Clone());}
        private void Apply_Click(object s,RoutedEventArgs e){ReadControlsIntoTheme();ApplyEditing();}
        private void SaveCustom_Click(object s,RoutedEventArgs e){ReadControlsIntoTheme();ApplyEditing(false);_saved.RemoveAll(t=>string.Equals(t.Name,_editing.Name,StringComparison.OrdinalIgnoreCase));_saved.Add(_editing.Clone());_store.Save(_saved);LoadThemeList();UpdateStatus("Custom theme saved");}
        private void DeleteCustom_Click(object s,RoutedEventArgs e){ReadControlsIntoTheme();var found=_saved.FirstOrDefault(t=>string.Equals(t.Name,_editing.Name,StringComparison.OrdinalIgnoreCase));if(found==null){UpdateStatus("Current theme is a preset");return;}if(MessageBox.Show($"Delete custom theme '{found.Name}'?","Delete Theme",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;_saved.Remove(found);_store.Save(_saved);BeginEdit(ThemePresets.Midnight());LoadThemeList();UpdateStatus("Custom theme deleted");}
        private void Import_Click(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Sec's See Time Theme (*.sstheme.json)|*.sstheme.json|JSON (*.json)|*.json"};if(d.ShowDialog()!=true)return;var t=_store.Import(d.FileName);if(t==null){MessageBox.Show("That theme file could not be read.","Theme Import",MessageBoxButton.OK,MessageBoxImage.Error);return;}BeginEdit(t);}
        private void Export_Click(object s,RoutedEventArgs e){ReadControlsIntoTheme();var d=new SaveFileDialog{Filter="Sec's See Time Theme (*.sstheme.json)|*.sstheme.json",FileName=(_editing.Name.Replace(" ","_")+".sstheme.json")};if(d.ShowDialog()==true){try{_store.Export(_editing,d.FileName);UpdateStatus("Theme exported");}catch(Exception ex){MessageBox.Show(ex.Message,"Theme Export",MessageBoxButton.OK,MessageBoxImage.Error);}}}
        private void BrowseImage_Click(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All files|*.*"};if(d.ShowDialog()==true)ImagePathBox.Text=d.FileName;}
        private void Header_MouseLeftButtonDown(object s,MouseButtonEventArgs e){if(e.LeftButton==MouseButtonState.Pressed)try{DragMove();}catch{}}
        private void CloseButton_Click(object s,RoutedEventArgs e){Close();}

        private sealed record ThemeChoice(AlarmTheme Theme,string Kind){public override string ToString()=>Kind+" • "+Theme.Name;}
    }
}
