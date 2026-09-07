using SecSeeTime.Enums;
using SecSeeTime.Models;
using SecSeeTime.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SecSeeTime.Views
{
    /// <summary>
    /// Records a voice memo and hands back the saved WAV.
    ///
    /// The result is an ordinary audio file, so the alarm stores it the
    /// same way it stores any other local file and the playback engine
    /// needs no special case.
    /// </summary>
    public partial class RecordAudioWindow : Window
    {
        /// <summary>Recordings are capped so a forgotten take cannot fill the disk.</summary>
        private static readonly TimeSpan MaxLength = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Peak below which a take counts as silent. Real inputs idle with a
        /// little noise, so this sits above zero rather than at it.
        /// </summary>
        private const float SilenceThreshold = 0.005f;

        private readonly AudioRecordingService _recorder = new();
        private readonly AudioPreviewService _preview;
        private readonly double _previewVolume;

        private readonly DispatcherTimer _tick =
            new() { Interval = TimeSpan.FromMilliseconds(200) };

        private string? _takePath;

        /// <summary>Destination of the take currently being recorded.</summary>
        private string? _pendingPath;

        /// <summary>Loudest level seen during the current take.</summary>
        private float _peakLevel;

        private bool _dotOn;


        public RecordAudioWindow(
            AudioPreviewService preview,
            double previewVolume)
        {
            InitializeComponent();

            _preview = preview;
            _previewVolume = previewVolume;

            NameBox.Text = "Recording " + DateTime.Now.ToString("MMM d h.mm tt");

            _tick.Tick += Tick;

            Closed += (_, __) => Cleanup();

            LoadDevices();
        }


        // =============================================================
        // DEVICES
        // =============================================================

        /// <summary>
        /// Fills the source list, reselecting the endpoint used last time
        /// where it is still present.
        /// </summary>
        private void LoadDevices()
        {
            List<RecordingDeviceInfo> devices =
                AudioRecordingService.GetDevices();

            DeviceBox.ItemsSource = devices;

            if (devices.Count == 0)
            {
                DeviceBox.IsEnabled = false;
                RecordButton.IsEnabled = false;

                StatusText.Text = "NO RECORDING DEVICES";

                DeviceHintText.Text =
                    "Windows is not reporting any active recording or playback " +
                    "devices. Connect a microphone or webcam and press refresh.";

                return;
            }

            DeviceBox.IsEnabled = true;
            RecordButton.IsEnabled = true;

            RecordingDeviceInfo? remembered = devices.FirstOrDefault(
                d => d.Id == AudioRecordingService.LastDeviceId);

            // Prefer a real input over a loopback source for a fresh start:
            // this dialog exists to record a voice memo.
            DeviceBox.SelectedItem =
                remembered
                ?? devices.FirstOrDefault(d => !d.IsLoopback)
                ?? devices[0];
        }


        private void RefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            if (_recorder.IsActive)
                return;

            LoadDevices();

            StatusText.Text = "DEVICE LIST REFRESHED";
        }


        private void DeviceBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (DeviceBox.SelectedItem is not RecordingDeviceInfo device)
                return;

            AudioRecordingService.LastDeviceId = device.Id;

            DeviceHintText.Text = device.IsLoopback
                ? "Records what this device is playing. Anything audible " +
                  "through it while recording is captured, so start the audio " +
                  "you want first."
                : "Records from this input. Windows must allow microphone " +
                  "access under Settings › Privacy & security › Microphone.";
        }


        /// <summary>The saved recording, once the dialog returns true.</summary>
        public string? SavedPath { get; private set; }


        // =============================================================
        // RECORDING
        // =============================================================

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            if (_recorder.IsActive)
            {
                StopRecording();
                return;
            }

            if (DeviceBox.SelectedItem is not RecordingDeviceInfo device)
            {
                MessageBox.Show(
                    "Choose a recording source first.",
                    "Record Audio",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            _preview.Stop();

            DiscardTake();

            // WASAPI streams straight to the file, so the destination is
            // chosen up front rather than on stop.
            string path =
                Path.Combine(
                    Path.GetTempPath(),
                    "sst-take-" + Guid.NewGuid().ToString("N") + ".wav");

            if (!_recorder.Start(device, path, out string error))
            {
                MessageBox.Show(
                    "Recording could not start.\n\n" + error,
                    "Record Audio",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            _pendingPath = path;
            _peakLevel = 0f;

            RecordButton.Content = "■  STOP RECORDING";
            RecordDot.Visibility = Visibility.Visible;
            PreviewButton.IsEnabled = false;
            UseButton.IsEnabled = false;
            DeviceBox.IsEnabled = false;
            RefreshDevicesButton.IsEnabled = false;
            StatusText.Text = "RECORDING";

            _tick.Start();
        }


        private void StopRecording()
        {
            _tick.Stop();

            string? path = _pendingPath;

            bool saved = _recorder.Stop(out string error);

            _pendingPath = null;

            RecordButton.Content = "●  RECORD AGAIN";
            RecordDot.Visibility = Visibility.Hidden;
            DeviceBox.IsEnabled = true;
            RefreshDevicesButton.IsEnabled = true;

            SetMeter(0);

            if (!saved || path == null)
            {
                StatusText.Text = "RECORDING FAILED";

                MessageBox.Show(
                    "The recording could not be saved.\n\n" + error,
                    "Record Audio",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            _takePath = path;

            PreviewButton.IsEnabled = true;
            UseButton.IsEnabled = true;

            StatusText.Text =
                $"RECORDED • {new FileInfo(path).Length / 1024:N0} KB";

            if (_peakLevel < SilenceThreshold)
            {
                StatusText.Text += " • SILENT";

                MessageBox.Show(
                    "The recording saved, but no sound was detected on " +
                    "\"" + (DeviceBox.SelectedItem as RecordingDeviceInfo)?.Name +
                    "\".\n\nIf that is not the source you meant, pick another " +
                    "one and record again. For a webcam microphone, check it " +
                    "is not muted in Windows sound settings.",
                    "Record Audio",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }


        private void Tick(object? sender, EventArgs e)
        {
            TimeSpan elapsed = _recorder.Elapsed;

            if (elapsed >= MaxLength)
            {
                StopRecording();

                StatusText.Text = "STOPPED AT THE 10 MINUTE LIMIT";

                return;
            }

            ElapsedText.Text =
                $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:00}";

            float level = _recorder.Level;

            if (level > _peakLevel)
                _peakLevel = level;

            SetMeter(level);

            _dotOn = !_dotOn;

            RecordDot.Opacity = _dotOn ? 1 : 0.25;
        }


        /// <summary>
        /// Draws the input level. Scaled as a rough square root so quiet
        /// speech still moves the bar visibly.
        /// </summary>
        private void SetMeter(float level)
        {
            double fraction = Math.Clamp(Math.Sqrt(level), 0, 1);

            MeterFill.Width = MeterTrack.Width * fraction;

            MeterFill.Background =
                level < SilenceThreshold
                    ? (Brush)FindResource("ThemePanelBorder")
                    : (Brush)FindResource("ThemeSuccess");
        }


        // =============================================================
        // PREVIEW
        // =============================================================

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (_preview.IsPlaying)
            {
                _preview.Stop();
                return;
            }

            if (_takePath == null || !File.Exists(_takePath))
                return;

            _preview.Play(
                new SoundDefinition
                {
                    Id = _takePath,
                    Name = Path.GetFileName(_takePath),
                    Category = SoundCategory.MySounds,
                    Kind = SoundKind.UserFile,
                    FilePath = _takePath
                },
                _previewVolume);
        }


        // =============================================================
        // RESULT
        // =============================================================

        private void Use_Click(object sender, RoutedEventArgs e)
        {
            if (_takePath == null || !File.Exists(_takePath))
                return;

            _preview.Stop();

            try
            {
                Directory.CreateDirectory(
                    AudioRecordingService.RecordingsFolder);

                string destination = BuildDestination(NameBox.Text);

                File.Move(_takePath, destination);

                _takePath = null;

                SavedPath = destination;

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The recording could not be saved.\n\n" + ex.Message,
                    "Record Audio",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }


        private static string BuildDestination(string requestedName)
        {
            string name = requestedName.Trim();

            if (string.IsNullOrWhiteSpace(name))
                name = "Recording";

            foreach (char bad in Path.GetInvalidFileNameChars())
                name = name.Replace(bad, '_');

            string candidate =
                Path.Combine(
                    AudioRecordingService.RecordingsFolder,
                    name + ".wav");

            int suffix = 2;

            while (File.Exists(candidate))
            {
                candidate =
                    Path.Combine(
                        AudioRecordingService.RecordingsFolder,
                        $"{name} ({suffix}).wav");

                suffix++;
            }

            return candidate;
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }


        private void Cleanup()
        {
            _tick.Stop();
            _preview.Stop();
            _recorder.Dispose();

            DiscardTake();
        }


        private void DiscardTake()
        {
            if (_takePath == null)
                return;

            try
            {
                if (File.Exists(_takePath))
                    File.Delete(_takePath);
            }
            catch { }

            _takePath = null;
        }


        private void Header_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }
    }
}
