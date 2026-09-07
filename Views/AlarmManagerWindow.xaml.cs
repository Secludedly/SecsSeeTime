using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using SecSeeTime.Enums;
using SecSeeTime.Helpers;
using SecSeeTime.Models;
using SecSeeTime.Services;

namespace SecSeeTime.Views
{
    public partial class AlarmManagerWindow : Window
    {
        private readonly AlarmService _alarmService;
        private readonly SoundLibraryService _library;
        private readonly AudioPreviewService _preview;

        private Alarm? _editingAlarm;


        // =============================================================
        // CONSTRUCTOR
        // =============================================================

        public AlarmManagerWindow(
            AlarmService alarmService,
            SoundLibraryService library,
            AudioPreviewService preview)
        {
            InitializeComponent();

            _alarmService = alarmService;
            _library = library;
            _preview = preview;

            _preview.PreviewChanged += Preview_PreviewChanged;
            _library.LibraryChanged += Library_LibraryChanged;

            RefreshSoundList();

            InitializeTimeControls();

            InitializeDefaultValues();

            RefreshAlarmList();

            UpdateNetworkStatus();
        }


        // =============================================================
        // SOUND LIBRARY
        // =============================================================

        private void RefreshSoundList()
        {
            SoundDefinition? previous = GetSelectedLibrarySound();

            BuiltInSoundComboBox.ItemsSource = _library.All;

            SelectLibrarySound(
                previous == null
                    ? _library.Default
                    : _library.FindById(previous.Id) ?? _library.Default);
        }


        private SoundDefinition? GetSelectedLibrarySound()
        {
            return BuiltInSoundComboBox.SelectedItem as SoundDefinition;
        }


        private void SelectLibrarySound(SoundDefinition? sound)
        {
            BuiltInSoundComboBox.SelectedItem = sound ?? _library.Default;

            UpdateSelectedSoundDescription();
        }


        private void UpdateSelectedSoundDescription()
        {
            SoundDefinition? sound = GetSelectedLibrarySound();

            SelectedSoundDescription.Text =
                sound == null
                    ? ""
                    : $"{sound.CategoryLabel}  •  {sound.Description}";
        }


        private void BuiltInSoundComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                UpdateSelectedSoundDescription();
                return;
            }

            // Switching sound mid-audition would leave the old one
            // playing under the new selection.
            if (_preview.IsPlaying)
                _preview.Stop();

            UpdateSelectedSoundDescription();
        }


        private void Library_LibraryChanged(object? sender, EventArgs e)
        {
            RefreshSoundList();
        }


        // =============================================================
        // PREVIEW
        // =============================================================

        private double PreviewVolume =>
            Math.Clamp(VolumeSlider.Value / 100.0, 0.0, 1.0);


        private void Preview_PreviewChanged(object? sender, EventArgs e)
        {
            bool playing = _preview.IsPlaying;

            PreviewSoundButton.Content =
                playing ? "■  STOP" : "▶  PREVIEW";

            PreviewFileButton.Content =
                playing ? "■  STOP" : "▶  PREVIEW";
        }


        private void PreviewSoundButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SoundDefinition? sound = GetSelectedLibrarySound();

            if (sound == null)
                return;

            _preview.Toggle(sound, PreviewVolume);
        }


        private void PreviewFileButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_preview.IsPlaying)
            {
                _preview.Stop();
                return;
            }

            string path = LocalAudioPathTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(path) ||
                path == "No audio file selected" ||
                !File.Exists(path))
            {
                MessageBox.Show(
                    "Choose an audio file first.",
                    "Preview",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            _preview.Play(
                new SoundDefinition
                {
                    Id = path,
                    Name = Path.GetFileName(path),
                    Category = SoundCategory.MySounds,
                    Kind = SoundKind.UserFile,
                    FilePath = path
                },
                PreviewVolume);
        }


        private void BrowseLibraryButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _preview.Stop();

            SoundLibraryWindow browser =
                new SoundLibraryWindow(
                    _library,
                    _preview,
                    PreviewVolume,
                    GetSelectedLibrarySound())
                {
                    Owner = this
                };

            browser.ShowDialog();

            RefreshSoundList();

            if (browser.SelectedSound != null)
            {
                SoundTypeComboBox.SelectedIndex = 0;

                SelectLibrarySound(browser.SelectedSound);

                UpdateSoundPanels();
            }
        }


        private void AddToLibraryButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string path = LocalAudioPathTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(path) ||
                path == "No audio file selected" ||
                !File.Exists(path))
            {
                MessageBox.Show(
                    "Choose an audio file first.",
                    "Add To Library",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            SoundDefinition? imported =
                _library.Import(path, out string error);

            if (imported == null)
            {
                MessageBox.Show(
                    error,
                    "Add To Library",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // Point the alarm at the library copy, which is the whole
            // reason for importing: the original can now move or be
            // deleted without breaking the alarm.
            SoundTypeComboBox.SelectedIndex = 0;

            SelectLibrarySound(imported);

            UpdateSoundPanels();
        }


        // =============================================================
        // TIME CONTROLS
        // =============================================================

        /// <summary>
        /// True when the editor is showing a 0-23 hour list with no AM/PM,
        /// which follows the application-wide time format.
        /// </summary>
        private bool Use24HourEditor => TimeFormatService.Use24HourNow;


        /// <summary>
        /// Shows a 24-hour clock time in the picker, in whichever format the
        /// picker is currently built for.
        /// </summary>
        private void SetTimePicker(int hour24, int minute)
        {
            hour24 = Math.Clamp(hour24, 0, 23);
            minute = Math.Clamp(minute, 0, 59);

            if (Use24HourEditor)
            {
                HourComboBox.SelectedItem = hour24.ToString();
            }
            else
            {
                int hour12 = hour24 % 12;

                if (hour12 == 0)
                    hour12 = 12;

                HourComboBox.SelectedItem = hour12.ToString();

                PeriodComboBox.SelectedItem = hour24 >= 12 ? "PM" : "AM";
            }

            MinuteComboBox.SelectedItem = minute.ToString("00");
        }


        /// <summary>
        /// Reads the picker back as a 24-hour value. Returns false when the
        /// selection is incomplete.
        /// </summary>
        private bool TryReadTimePicker(out int hour24, out int minute)
        {
            hour24 = 0;
            minute = 0;

            if (HourComboBox.SelectedItem == null ||
                MinuteComboBox.SelectedItem == null)
            {
                return false;
            }

            if (!int.TryParse(
                    HourComboBox.SelectedItem.ToString(),
                    out hour24))
            {
                return false;
            }

            if (!int.TryParse(
                    MinuteComboBox.SelectedItem.ToString(),
                    out minute))
            {
                return false;
            }

            if (Use24HourEditor)
                return hour24 is >= 0 and <= 23;

            if (PeriodComboBox.SelectedItem == null)
                return false;

            string period = PeriodComboBox.SelectedItem.ToString()!;

            if (period == "PM" && hour24 != 12)
                hour24 += 12;

            if (period == "AM" && hour24 == 12)
                hour24 = 0;

            return true;
        }


        private void InitializeTimeControls()
        {
            HourComboBox.Items.Clear();

            // Hours are unpadded to match every other time display in
            // the app. Minutes stay padded -- "4:5" is not a time.
            if (Use24HourEditor)
            {
                for (int hour = 0; hour <= 23; hour++)
                    HourComboBox.Items.Add(hour.ToString());
            }
            else
            {
                for (int hour = 1; hour <= 12; hour++)
                    HourComboBox.Items.Add(hour.ToString());
            }


            MinuteComboBox.Items.Clear();

            for (int minute = 0; minute < 60; minute++)
            {
                MinuteComboBox.Items.Add(
                    minute.ToString("00"));
            }


            PeriodComboBox.Items.Clear();

            PeriodComboBox.Items.Add("AM");
            PeriodComboBox.Items.Add("PM");

            // AM/PM is meaningless on a 24-hour clock, so it is hidden
            // rather than left to contradict the hour beside it.
            PeriodComboBox.Visibility =
                Use24HourEditor
                    ? Visibility.Collapsed
                    : Visibility.Visible;


            // 7am either way.
            HourComboBox.SelectedIndex = Use24HourEditor ? 7 : 6;

            MinuteComboBox.SelectedIndex = 0;

            PeriodComboBox.SelectedIndex = 0;


            RepeatComboBox.SelectedIndex = 0;

            SoundTypeComboBox.SelectedIndex = 0;

            SoundDurationComboBox.SelectedIndex = 2;

            SilenceDurationComboBox.SelectedIndex = 2;

            SnoozeComboBox.SelectedIndex = 1;


            VolumeSlider.ValueChanged +=
                VolumeSlider_ValueChanged;
        }


        // =============================================================
        // DEFAULT VALUES
        // =============================================================

        private void InitializeDefaultValues()
        {
            DateTime suggestedTime =
                DateTime.Now.AddMinutes(5);

            SetTimePicker(
                suggestedTime.Hour,
                suggestedTime.Minute);


            AlarmNameTextBox.Text =
                "New Alarm";


            RepeatSoundCycleCheckBox.IsChecked =
                true;


            SnoozeCheckBox.IsChecked =
                true;


            VolumeSlider.Value = 100;

            UpdateCustomSnoozePanel();
        }


        // =============================================================
        // REFRESH LIST
        // =============================================================

        private void RefreshAlarmList()
        {
            AlarmList.ItemsSource = null;

            AlarmList.ItemsSource =
                _alarmService.Alarms.ToList();


            int total =
                _alarmService.Alarms.Count;


            int enabled =
                _alarmService.ActiveAlarmCount;


            AlarmSummaryText.Text =
                $"{total} alarm" +
                (total == 1 ? "" : "s") +
                $" configured • {enabled} active";
        }


        // =============================================================
        // NEW ALARM
        // =============================================================

        private void NewAlarmButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            BeginNewAlarm();
        }


        private void BeginNewAlarm()
        {
            _editingAlarm = null;

            EditorTitle.Text =
                "CREATE NEW ALARM";

            SaveButton.Content =
                "CREATE ALARM";

            CancelEditButton.Visibility =
                Visibility.Collapsed;


            AlarmNameTextBox.Text =
                "New Alarm";


            DateTime suggestedTime =
                DateTime.Now.AddMinutes(5);


            SetTimePicker(
                suggestedTime.Hour,
                suggestedTime.Minute);


            RepeatComboBox.SelectedIndex = 0;

            SoundTypeComboBox.SelectedIndex = 0;

            SelectLibrarySound(_library.Default);

            SoundDurationComboBox.SelectedIndex = 2;

            SilenceDurationComboBox.SelectedIndex = 2;

            RepeatSoundCycleCheckBox.IsChecked = true;

            SnoozeCheckBox.IsChecked = true;

            SnoozeComboBox.SelectedIndex = 1;

            VolumeSlider.Value = 100;


            LocalAudioPathTextBox.Text =
                "No audio file selected";

            YouTubeUrlTextBox.Text =
                "";


            ClearCustomDays();

            UpdateCustomSnoozePanel();

            UpdateSoundPanels();
        }


        // =============================================================
        // SAVE ALARM
        // =============================================================

        private void SaveAlarmButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!TryBuildAlarm(
                    out Alarm? alarm,
                    out string errorMessage))
            {
                MessageBox.Show(
                    errorMessage,
                    "Invalid Alarm",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            if (_editingAlarm == null)
            {
                _alarmService.AddAlarm(alarm!);
            }
            else
            {
                CopyAlarmValues(
                    alarm!,
                    _editingAlarm);

                // Edited in place, so the service has to be told to
                // write the file and notify the main window.
                _alarmService.Commit();
            }


            RefreshAlarmList();

            BeginNewAlarm();
        }


        // =============================================================
        // BUILD ALARM
        // =============================================================

        private bool TryBuildAlarm(
            out Alarm? alarm,
            out string errorMessage)
        {
            alarm = null;

            errorMessage = "";


            string name =
                AlarmNameTextBox.Text.Trim();


            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage =
                    "Please enter a name for the alarm.";

                return false;
            }


            if (!TryReadTimePicker(out int hour, out int minute))
            {
                errorMessage =
                    "Please select a valid alarm time.";

                return false;
            }


            AlarmRepeatMode repeatMode =
                GetSelectedRepeatMode();


            DayOfWeek[] customDays =
                GetSelectedCustomDays();


            if (repeatMode == AlarmRepeatMode.Custom &&
                customDays.Length == 0)
            {
                errorMessage =
                    "Custom repeat mode requires at least one day.";

                return false;
            }


            AlarmSoundType soundType =
                GetSelectedSoundType();


            string? localPath = null;

            string? youtubeUrl = null;

            string builtInName =
                _editingAlarm?.BuiltInSoundName
                ?? BuiltInSounds.DefaultSoundName;


            if (soundType ==
                AlarmSoundType.LocalFile)
            {
                string path =
                    LocalAudioPathTextBox.Text.Trim();


                if (string.IsNullOrWhiteSpace(path) ||
                    path == "No audio file selected")
                {
                    errorMessage =
                        "Please select an audio file.";

                    return false;
                }


                if (!File.Exists(path))
                {
                    errorMessage =
                        "The selected audio file could not be found.";

                    return false;
                }


                localPath = path;
            }


            if (soundType ==
                AlarmSoundType.YouTube)
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    errorMessage =
                        "There is no network connection. " +
                        "YouTube alarms require Internet access.";

                    return false;
                }


                youtubeUrl =
                    YouTubeUrlTextBox.Text.Trim();


                if (string.IsNullOrWhiteSpace(youtubeUrl) ||
                    youtubeUrl == "Paste YouTube URL here")
                {
                    errorMessage =
                        "Please enter a YouTube URL.";

                    return false;
                }


                if (!YouTubeService.LooksLikeYouTubeUrl(youtubeUrl))
                {
                    errorMessage =
                        "Please enter a valid YouTube URL.";

                    return false;
                }


                /*
                 * Validate with the same parser the player uses, so a
                 * link cannot be accepted here and then rejected at
                 * seven in the morning.
                 */

                if (!YouTubeService.TryGetVideoId(youtubeUrl, out _))
                {
                    errorMessage =
                        "That YouTube link does not contain a video ID.\n\n" +
                        "Watch, share, shorts and embed links all work.";

                    return false;
                }
            }


            /*
             * "Sound Library" covers both the synthesized catalog and
             * anything the user imported. A built-in is stored by name;
             * an import is stored as a local file pointing at the
             * library's own copy.
             */

            if (soundType == AlarmSoundType.BuiltIn)
            {
                SoundDefinition? selected = GetSelectedLibrarySound();

                if (selected == null)
                {
                    errorMessage =
                        "Please choose a sound from the library.";

                    return false;
                }

                if (selected.Kind == SoundKind.UserFile)
                {
                    if (!File.Exists(selected.FilePath))
                    {
                        errorMessage =
                            $"'{selected.Name}' is missing from your library.\n\n" +
                            "Import it again, or pick a different sound.";

                        return false;
                    }

                    soundType = AlarmSoundType.LocalFile;

                    localPath = selected.FilePath;
                }
                else
                {
                    builtInName = selected.Name;
                }
            }


            int soundDuration =
                GetDurationFromCombo(
                    SoundDurationComboBox);


            int silenceDuration =
                GetDurationFromCombo(
                    SilenceDurationComboBox);


            if (!TryGetSnoozeMinutes(
                    out int snoozeMinutes,
                    out string snoozeError))
            {
                errorMessage = snoozeError;

                return false;
            }


            alarm = new Alarm
            {
                Id = _editingAlarm?.Id ?? Guid.NewGuid(),

                Name = name,

                IsEnabled =
                    _editingAlarm?.IsEnabled ?? true,

                Time =
                    new TimeSpan(
                        hour,
                        minute,
                        0),

                RepeatMode =
                    repeatMode,

                CustomDays =
                    customDays,

                SoundType =
                    soundType,

                BuiltInSoundName =
                    builtInName,

                LocalAudioPath =
                    localPath,

                YouTubeUrl =
                    youtubeUrl,

                Volume =
                    VolumeSlider.Value / 100.0,

                SoundDurationSeconds =
                    soundDuration,

                SilenceDurationSeconds =
                    silenceDuration,

                RepeatSoundCycle =
                    RepeatSoundCycleCheckBox.IsChecked == true,

                SnoozeEnabled =
                    SnoozeCheckBox.IsChecked == true,

                SnoozeMinutes =
                    snoozeMinutes,

                CreatedAt =
                    _editingAlarm?.CreatedAt ?? DateTime.Now
            };


            return true;
        }


        // =============================================================
        // COPY ALARM VALUES
        // =============================================================

        private void CopyAlarmValues(
            Alarm source,
            Alarm destination)
        {
            destination.Name =
                source.Name;

            destination.Time =
                source.Time;

            destination.RepeatMode =
                source.RepeatMode;

            destination.CustomDays =
                source.CustomDays;

            destination.SoundType =
                source.SoundType;

            destination.BuiltInSoundName =
                source.BuiltInSoundName;

            destination.LocalAudioPath =
                source.LocalAudioPath;

            destination.YouTubeUrl =
                source.YouTubeUrl;

            destination.Volume =
                source.Volume;

            destination.SoundDurationSeconds =
                source.SoundDurationSeconds;

            destination.SilenceDurationSeconds =
                source.SilenceDurationSeconds;

            destination.RepeatSoundCycle =
                source.RepeatSoundCycle;

            destination.SnoozeEnabled =
                source.SnoozeEnabled;

            destination.SnoozeMinutes =
                source.SnoozeMinutes;
        }


        // =============================================================
        // EDIT
        // =============================================================

        private void EditAlarmButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not Alarm alarm)
            {
                return;
            }


            _editingAlarm = alarm;


            EditorTitle.Text =
                "EDIT ALARM";

            SaveButton.Content =
                "SAVE CHANGES";

            CancelEditButton.Visibility =
                Visibility.Visible;


            AlarmNameTextBox.Text =
                alarm.Name;


            SetTimePicker(
                alarm.Time.Hours,
                alarm.Time.Minutes);


            RepeatComboBox.SelectedIndex =
                (int)alarm.RepeatMode;


            SetCustomDays(
                alarm.CustomDays);


            /*
             * An alarm using an imported sound is stored as a local
             * file. If that path is still in the library, show it as a
             * library selection rather than as a raw path the user
             * never typed.
             */

            SoundDefinition? importedMatch =
                alarm.SoundType == AlarmSoundType.LocalFile
                    ? _library.UserSounds.FirstOrDefault(
                        sound => string.Equals(
                            sound.FilePath,
                            alarm.LocalAudioPath,
                            StringComparison.OrdinalIgnoreCase))
                    : null;

            if (importedMatch != null)
            {
                SoundTypeComboBox.SelectedIndex = 0;

                SelectLibrarySound(importedMatch);
            }
            else if (alarm.SoundType == AlarmSoundType.LocalFile &&
                     AudioRecordingService.IsInRecordingsFolder(
                         alarm.LocalAudioPath))
            {
                // Stored as a local file, but it is one of ours, so
                // reopen the editor on the panel it was made in.
                SoundTypeComboBox.SelectedIndex = RecordingIndex;
            }
            else
            {
                SoundTypeComboBox.SelectedIndex =
                    (int)alarm.SoundType;

                SelectLibrarySound(
                    _library.FindByName(alarm.BuiltInSoundName));
            }


            LocalAudioPathTextBox.Text =
                string.IsNullOrWhiteSpace(
                    alarm.LocalAudioPath)
                    ? "No audio file selected"
                    : alarm.LocalAudioPath;


            YouTubeUrlTextBox.Text =
                alarm.YouTubeUrl ?? "";


            VolumeSlider.Value =
                alarm.Volume * 100;


            SelectDuration(
                SoundDurationComboBox,
                alarm.SoundDurationSeconds);


            SelectDuration(
                SilenceDurationComboBox,
                alarm.SilenceDurationSeconds);


            RepeatSoundCycleCheckBox.IsChecked =
                alarm.RepeatSoundCycle;


            SnoozeCheckBox.IsChecked =
                alarm.SnoozeEnabled;


            SelectSnooze(
                alarm.SnoozeMinutes);


            UpdateSoundPanels();
        }


        // =============================================================
        // DELETE
        // =============================================================

        private void DeleteAlarmButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not Alarm alarm)
            {
                return;
            }


            MessageBoxResult result =
                MessageBox.Show(
                    $"Delete '{alarm.Name}'?\n\n" +
                    "This cannot be undone.",
                    "Delete Alarm",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);


            if (result != MessageBoxResult.Yes)
                return;


            _alarmService.RemoveAlarm(
                alarm.Id);


            if (_editingAlarm?.Id == alarm.Id)
            {
                BeginNewAlarm();
            }


            RefreshAlarmList();
        }


        // =============================================================
        // ENABLE / DISABLE
        // =============================================================

        private void AlarmEnabled_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox &&
                checkBox.Tag is Alarm alarm)
            {
                alarm.IsEnabled =
                    checkBox.IsChecked == true;

                _alarmService.Commit();

                RefreshAlarmList();
            }
        }


        // =============================================================
        // REPEAT MODE
        // =============================================================

        private void RepeatComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;


            CustomDaysPanel.Visibility =
                GetSelectedRepeatMode() ==
                AlarmRepeatMode.Custom
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }


        private AlarmRepeatMode GetSelectedRepeatMode()
        {
            return RepeatComboBox.SelectedIndex switch
            {
                1 => AlarmRepeatMode.Daily,

                2 => AlarmRepeatMode.Weekdays,

                3 => AlarmRepeatMode.Weekends,

                4 => AlarmRepeatMode.Custom,

                _ => AlarmRepeatMode.Once
            };
        }


        // =============================================================
        // CUSTOM DAYS
        // =============================================================

        private DayOfWeek[] GetSelectedCustomDays()
        {
            List<DayOfWeek> days =
                new List<DayOfWeek>();


            if (MondayCheckBox.IsChecked == true)
                days.Add(DayOfWeek.Monday);

            if (TuesdayCheckBox.IsChecked == true)
                days.Add(DayOfWeek.Tuesday);

            if (WednesdayCheckBox.IsChecked == true)
                days.Add(DayOfWeek.Wednesday);

            if (ThursdayCheckBox.IsChecked == true)
                days.Add(DayOfWeek.Thursday);

            if (FridayCheckBox.IsChecked == true)
                days.Add(DayOfWeek.Friday);

            if (SaturdayCheckBox.IsChecked == true)
                days.Add(DayOfWeek.Saturday);

            if (SundayCheckBox.IsChecked == true)
                days.Add(DayOfWeek.Sunday);


            return days.ToArray();
        }


        private void SetCustomDays(
            DayOfWeek[] days)
        {
            MondayCheckBox.IsChecked =
                days.Contains(DayOfWeek.Monday);

            TuesdayCheckBox.IsChecked =
                days.Contains(DayOfWeek.Tuesday);

            WednesdayCheckBox.IsChecked =
                days.Contains(DayOfWeek.Wednesday);

            ThursdayCheckBox.IsChecked =
                days.Contains(DayOfWeek.Thursday);

            FridayCheckBox.IsChecked =
                days.Contains(DayOfWeek.Friday);

            SaturdayCheckBox.IsChecked =
                days.Contains(DayOfWeek.Saturday);

            SundayCheckBox.IsChecked =
                days.Contains(DayOfWeek.Sunday);
        }


        private void ClearCustomDays()
        {
            MondayCheckBox.IsChecked = false;
            TuesdayCheckBox.IsChecked = false;
            WednesdayCheckBox.IsChecked = false;
            ThursdayCheckBox.IsChecked = false;
            FridayCheckBox.IsChecked = false;
            SaturdayCheckBox.IsChecked = false;
            SundayCheckBox.IsChecked = false;
        }


        // =============================================================
        // SOUND TYPE
        // =============================================================

        private void SoundTypeComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            UpdateSoundPanels();
        }


        private void UpdateSoundPanels()
        {
            /*
             * Switched on the selected index rather than the sound type:
             * "My Audio File" and "Record Audio" are both stored as a
             * local file, and only the panel differs.
             */

            int index = SoundTypeComboBox.SelectedIndex;


            BuiltInSoundPanel.Visibility =
                index == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;


            LocalFilePanel.Visibility =
                index == 1
                    ? Visibility.Visible
                    : Visibility.Collapsed;


            YouTubePanel.Visibility =
                index == 2
                    ? Visibility.Visible
                    : Visibility.Collapsed;


            RecordingPanel.Visibility =
                index == RecordingIndex
                    ? Visibility.Visible
                    : Visibility.Collapsed;


            if (index == RecordingIndex)
            {
                UpdateRecordingSummary();
            }


            if (index == 2)
            {
                UpdateNetworkStatus();
            }
        }


        private AlarmSoundType GetSelectedSoundType()
        {
            return SoundTypeComboBox.SelectedIndex switch
            {
                1 => AlarmSoundType.LocalFile,

                2 => AlarmSoundType.YouTube,

                // A recording is just a local file the app made itself.
                RecordingIndex => AlarmSoundType.LocalFile,

                _ => AlarmSoundType.BuiltIn
            };
        }


        // =============================================================
        // RECORDED AUDIO
        // =============================================================

        /// <summary>Index of the "Record Audio" item in the source list.</summary>
        private const int RecordingIndex = 3;


        private void RecordAudioButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _preview.Stop();

            RecordAudioWindow recorder =
                new RecordAudioWindow(_preview, PreviewVolume)
                {
                    Owner = this
                };

            if (recorder.ShowDialog() != true ||
                string.IsNullOrWhiteSpace(recorder.SavedPath))
            {
                return;
            }

            // The recording is stored exactly like a browsed file, so
            // saving, preview and playback all take the existing path.
            LocalAudioPathTextBox.Text = recorder.SavedPath;

            UpdateRecordingSummary();
        }


        private void PreviewRecordingButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            PreviewFileButton_Click(sender, e);
        }


        private void UpdateRecordingSummary()
        {
            string path = LocalAudioPathTextBox.Text.Trim();

            if (!AudioRecordingService.IsInRecordingsFolder(path) ||
                !File.Exists(path))
            {
                RecordingNameText.Text = "No recording yet";

                RecordingDetailText.Text =
                    "Record a voice memo and this alarm will play it.";

                PreviewRecordingButton.IsEnabled = false;

                return;
            }

            FileInfo info = new FileInfo(path);

            RecordingNameText.Text =
                Path.GetFileNameWithoutExtension(path);

            RecordingDetailText.Text =
                $"{info.Length / 1024:N0} KB • recorded " +
                $"{info.CreationTime:MMM d}, " +
                TimeHelper.FormatTime(info.CreationTime);

            PreviewRecordingButton.IsEnabled = true;
        }


        // =============================================================
        // AUDIO FILE
        // =============================================================

        private void BrowseAudioButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenFileDialog dialog =
                new OpenFileDialog
                {
                    Title =
                        "Select Alarm Audio",

                    Filter =
                        "Audio Files|" +
                        "*.mp3;*.wav;*.m4a;*.aac;*.flac;*.ogg;*.wma|" +
                        "MP3 Files|*.mp3|" +
                        "WAV Files|*.wav|" +
                        "M4A Files|*.m4a|" +
                        "AAC Files|*.aac|" +
                        "FLAC Files|*.flac|" +
                        "OGG Files|*.ogg|" +
                        "WMA Files|*.wma|" +
                        "All Files|*.*",

                    Multiselect = false
                };


            if (dialog.ShowDialog() == true)
            {
                LocalAudioPathTextBox.Text =
                    dialog.FileName;
            }
        }


        // =============================================================
        // YOUTUBE NETWORK STATUS
        // =============================================================

        private void UpdateNetworkStatus()
        {
            bool connected =
                NetworkInterface.GetIsNetworkAvailable();


            if (connected)
            {
                YouTubeUrlTextBox.IsEnabled = true;

                YouTubeOfflineText.Visibility =
                    Visibility.Collapsed;
            }
            else
            {
                YouTubeUrlTextBox.IsEnabled = false;

                YouTubeOfflineText.Visibility =
                    Visibility.Visible;
            }
        }


        // =============================================================
        // VOLUME
        // =============================================================

        private void VolumeSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (VolumeValueText == null)
                return;


            VolumeValueText.Text =
                $"{Math.Round(VolumeSlider.Value)}%";
        }


        // =============================================================
        // DURATION HELPERS
        // =============================================================

        /*
         * These are seconds, not minutes.
         *
         * The first option is "30 seconds", which a minute-based field
         * could only round down to zero -- and the engine then clamped
         * that to a single second.
         */

        private static readonly int[] DurationSeconds =
            { 30, 60, 120, 180, 300, 600 };


        private int GetDurationFromCombo(ComboBox comboBox)
        {
            int index = comboBox.SelectedIndex;

            return index >= 0 && index < DurationSeconds.Length
                ? DurationSeconds[index]
                : 120;
        }


        private void SelectDuration(
            ComboBox comboBox,
            int seconds)
        {
            int index = Array.IndexOf(DurationSeconds, seconds);

            comboBox.SelectedIndex = index >= 0 ? index : 2;
        }


        // =============================================================
        // SNOOZE HELPERS
        // =============================================================

        private static readonly int[] SnoozeMinuteOptions =
            { 1, 5, 10, 15, 20, 30, 45, 60 };


        private int CustomSnoozeIndex =>
            SnoozeMinuteOptions.Length;


        private void SnoozeComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            UpdateCustomSnoozePanel();
        }


        private void UpdateCustomSnoozePanel()
        {
            CustomSnoozePanel.Visibility =
                SnoozeComboBox.SelectedIndex == CustomSnoozeIndex
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }


        private bool TryGetSnoozeMinutes(
            out int minutes,
            out string errorMessage)
        {
            errorMessage = "";

            int index = SnoozeComboBox.SelectedIndex;

            if (index >= 0 && index < SnoozeMinuteOptions.Length)
            {
                minutes = SnoozeMinuteOptions[index];

                return true;
            }

            // Custom.
            minutes = 0;

            if (!int.TryParse(
                    CustomSnoozeHoursTextBox.Text.Trim(),
                    out int hours) ||
                hours < 0)
            {
                errorMessage =
                    "Custom snooze hours must be a whole number of 0 or more.";

                return false;
            }

            if (!int.TryParse(
                    CustomSnoozeMinutesTextBox.Text.Trim(),
                    out int extraMinutes) ||
                extraMinutes < 0)
            {
                errorMessage =
                    "Custom snooze minutes must be a whole number of 0 or more.";

                return false;
            }

            minutes = (hours * 60) + extraMinutes;

            if (minutes < 1)
            {
                errorMessage =
                    "A custom snooze has to be at least one minute.";

                return false;
            }

            if (minutes > 720)
            {
                errorMessage =
                    "A snooze longer than 12 hours is not a snooze.";

                return false;
            }

            return true;
        }


        private void SelectSnooze(int minutes)
        {
            int index = Array.IndexOf(SnoozeMinuteOptions, minutes);

            if (index >= 0)
            {
                SnoozeComboBox.SelectedIndex = index;
            }
            else
            {
                SnoozeComboBox.SelectedIndex = CustomSnoozeIndex;

                CustomSnoozeHoursTextBox.Text =
                    (minutes / 60).ToString();

                CustomSnoozeMinutesTextBox.Text =
                    (minutes % 60).ToString();
            }

            UpdateCustomSnoozePanel();
        }


        // =============================================================
        // CANCEL EDIT
        // =============================================================

        private void CancelEditButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            BeginNewAlarm();
        }


        // =============================================================
        // WINDOW DRAGGING
        // =============================================================

        private void Header_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton ==
                MouseButtonState.Pressed)
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


        // =============================================================
        // CLOSE
        // =============================================================

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }


        protected override void OnClosed(EventArgs e)
        {
            _preview.PreviewChanged -= Preview_PreviewChanged;
            _library.LibraryChanged -= Library_LibraryChanged;

            // A preview still looping after the editor closes would be
            // indistinguishable from an alarm nobody can stop.
            _preview.Stop();

            base.OnClosed(e);
        }
    }
}