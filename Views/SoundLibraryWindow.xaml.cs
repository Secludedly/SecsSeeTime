using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using SecSeeTime.Enums;
using SecSeeTime.Models;
using SecSeeTime.Services;

namespace SecSeeTime.Views
{
    /// <summary>
    /// Browse, preview and import alarm sounds.
    /// </summary>
    public partial class SoundLibraryWindow : Window
    {
        private readonly SoundLibraryService _library;
        private readonly AudioPreviewService _preview;
        private readonly double _volume;

        private SoundCategory _category = SoundCategory.Alarms;


        /// <summary>The sound the user chose, if they chose one.</summary>
        public SoundDefinition? SelectedSound { get; private set; }


        public SoundLibraryWindow(
            SoundLibraryService library,
            AudioPreviewService preview,
            double volume,
            SoundDefinition? startAt = null)
        {
            InitializeComponent();

            _library = library;
            _preview = preview;
            _volume = Math.Clamp(volume, 0.0, 1.0);

            if (startAt != null)
                _category = startAt.Category;

            _preview.PreviewChanged += Preview_PreviewChanged;
            _library.LibraryChanged += Library_LibraryChanged;

            Refresh();
        }


        // =============================================================
        // LIST BUILDING
        // =============================================================

        private void Refresh()
        {
            IReadOnlyList<SoundCategory> categories = _library.Categories;

            if (!categories.Contains(_category))
                _category = categories[0];

            CategoryList.ItemsSource =
                categories
                    .Select(category => new CategoryRow
                    {
                        Category = category,
                        Label = Label(category),
                        Count = _library.InCategory(category).Count.ToString(),
                        Highlight = category == _category
                            ? Frozen(ThemeService.Current.Theme.PanelBackgroundLight)
                            : Brushes.Transparent,
                        Foreground = Frozen(
                            category == _category
                                ? ThemeService.Current.Theme.TextPrimary
                                : ThemeService.Current.Theme.TextSecondary)
                    })
                    .ToList();

            List<SoundDefinition> sounds =
                _library.InCategory(_category).ToList();

            SoundList.ItemsSource =
                sounds
                    .Select(sound => new SoundRow
                    {
                        Sound = sound,

                        PreviewGlyph =
                            _preview.IsPlayingId(sound.Id) ? "■" : "▶",

                        RowBorder = Frozen(
                            _preview.IsPlayingId(sound.Id)
                                ? ThemeService.Current.Theme.Accent
                                : ThemeService.Current.Theme.PanelBorder),

                        RemoveVisibility = sound.IsUserSound
                            ? Visibility.Visible
                            : Visibility.Collapsed
                    })
                    .ToList();

            SubtitleText.Text =
                $"{_library.All.Count} SOUNDS • {_library.UserSounds.Count} IMPORTED";

            FooterHintText.Text =
                _preview.IsPlaying
                    ? $"Previewing “{_preview.Current!.Name}” — previews stop after 8 seconds."
                    : "Supported for import: " +
                      SoundLibraryService.SupportedExtensionsLabel;
        }


        private static string Label(SoundCategory category)
        {
            return category switch
            {
                SoundCategory.Alarms => "ALARMS",
                SoundCategory.Chimes => "CHIMES",
                SoundCategory.Electronic => "ELECTRONIC",
                SoundCategory.Ambient => "AMBIENT",
                SoundCategory.MySounds => "MY SOUNDS",
                _ => "OTHER"
            };
        }


        private static Brush Frozen(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);

            brush.Freeze();

            return brush;
        }


        // =============================================================
        // EVENTS FROM THE SERVICES
        // =============================================================

        private void Preview_PreviewChanged(object? sender, EventArgs e)
        {
            Refresh();
        }


        private void Library_LibraryChanged(object? sender, EventArgs e)
        {
            Refresh();
        }


        // =============================================================
        // ACTIONS
        // =============================================================

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: SoundCategory category })
            {
                _category = category;

                Refresh();
            }
        }


        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: SoundDefinition sound })
                _preview.Toggle(sound, _volume);
        }


        private void UseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: SoundDefinition sound })
                return;

            SelectedSound = sound;

            _preview.Stop();

            DialogResult = true;

            Close();
        }


        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: SoundDefinition sound })
                return;

            MessageBoxResult result =
                MessageBox.Show(
                    $"Remove '{sound.Name}' from your library?\n\n" +
                    "The copy inside Sec's See Time is deleted. Your " +
                    "original file is not touched.\n\n" +
                    "Any alarm using this sound will fall back to the " +
                    "default alarm tone.",
                    "Remove Sound",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            if (_preview.IsPlayingId(sound.Id))
                _preview.Stop();

            _library.Remove(sound);
        }


        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog =
                new OpenFileDialog
                {
                    Title = "Import Alarm Sound",

                    Filter =
                        "Audio Files|" +
                        "*.mp3;*.wav;*.m4a;*.aac;*.flac;*.ogg;*.opus;*.wma;*.aiff;*.aif|" +
                        "MP3 Files|*.mp3|" +
                        "WAV Files|*.wav|" +
                        "FLAC Files|*.flac|" +
                        "All Files|*.*",

                    Multiselect = true
                };

            if (dialog.ShowDialog() != true)
                return;

            List<string> failures = new();

            SoundDefinition? last = null;

            foreach (string path in dialog.FileNames)
            {
                SoundDefinition? imported =
                    _library.Import(path, out string error);

                if (imported == null)
                    failures.Add($"{System.IO.Path.GetFileName(path)}: {error}");
                else
                    last = imported;
            }

            if (last != null)
                _category = SoundCategory.MySounds;

            Refresh();

            if (failures.Count > 0)
            {
                MessageBox.Show(
                    "Some sounds could not be imported.\n\n" +
                    string.Join("\n\n", failures),
                    "Import",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }


        // =============================================================
        // WINDOW CHROME
        // =============================================================

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


        protected override void OnClosed(EventArgs e)
        {
            _preview.PreviewChanged -= Preview_PreviewChanged;
            _library.LibraryChanged -= Library_LibraryChanged;

            // Leaving a preview looping after the window is gone would
            // be indistinguishable from a stuck alarm.
            _preview.Stop();

            base.OnClosed(e);
        }


        // =============================================================
        // ROW SHAPES
        // =============================================================

        private sealed class CategoryRow
        {
            public SoundCategory Category { get; init; }

            public string Label { get; init; } = "";

            public string Count { get; init; } = "";

            public Brush Highlight { get; init; } = Brushes.Transparent;

            public Brush Foreground { get; init; } = Brushes.White;
        }


        private sealed class SoundRow
        {
            public SoundDefinition Sound { get; init; } = new();

            public string PreviewGlyph { get; init; } = "▶";

            public Brush RowBorder { get; init; } = Brushes.Gray;

            public Visibility RemoveVisibility { get; init; }
        }
    }
}
