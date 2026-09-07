using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SecSeeTime.Models;
using SecSeeTime.Services;
using SecSeeTime.Themes;

namespace SecSeeTime.Views
{
    /// <summary>
    /// A preset picker sitting directly on the theme engine.
    ///
    /// This is the seed of the Appearance Studio: the Studio will edit
    /// the same <see cref="AlarmTheme"/> objects and hand them to the
    /// same <see cref="ThemeService"/>.
    /// </summary>
    public partial class ThemePickerWindow : Window
    {
        public ThemePickerWindow()
        {
            InitializeComponent();

            BuildList();
        }


        private void BuildList()
        {
            List<PresetRow> rows = new();

            string activeName = ThemeService.Current.Theme.Name;

            foreach (AlarmTheme theme in ThemePresets.All)
            {
                rows.Add(
                    new PresetRow
                    {
                        Theme = theme,
                        Name = theme.Name.ToUpperInvariant(),
                        Description = theme.Description,
                        Surface = Frozen(theme.PanelBackgroundLight),
                        Accent = Frozen(theme.Accent),
                        Marker = theme.Name == activeName ? "ACTIVE" : ""
                    });
            }

            PresetList.ItemsSource = rows;
        }


        private static Brush Frozen(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);

            brush.Freeze();

            return brush;
        }


        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not AlarmTheme theme)
            {
                return;
            }

            ThemeService.Current.Apply(theme.Clone());

            BuildList();
        }


        private void Header_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch
                {
                    // Ignore rapid mouse event errors.
                }
            }
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private sealed class PresetRow
        {
            public AlarmTheme Theme { get; init; } = new();

            public string Name { get; init; } = "";

            public string Description { get; init; } = "";

            public Brush Surface { get; init; } = Brushes.Black;

            public Brush Accent { get; init; } = Brushes.White;

            public string Marker { get; init; } = "";
        }
    }
}
