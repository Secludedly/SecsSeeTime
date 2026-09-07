using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SecSeeTime.Controls;
using SecSeeTime.Enums;
using SecSeeTime.Helpers;
using SecSeeTime.Models;
using SecSeeTime.Services;

namespace SecSeeTime.Views
{
    /// <summary>
    /// The alarm screen.
    ///
    /// It renders whatever the alarm engine says is happening and never
    /// guesses. The countdown is derived from the phase deadline in
    /// <see cref="AlarmSessionState"/> rather than from a local
    /// decrementing counter, so the display cannot drift away from the
    /// audio it is describing.
    /// </summary>
    public partial class AlarmWindow : Window
    {
        private readonly Alarm _alarm;
        private readonly DispatcherTimer _renderTimer;

        private AlarmSessionState? _state;
        private YouTubePlayerControl? _player;

        private bool _closingFromEngine;
        private bool _stopRaised;
        private AlarmPhase _lastRenderedPhase = AlarmPhase.Idle;


        // =============================================================
        // EVENTS
        // =============================================================

        public event EventHandler? StopRequested;

        public event Action<int>? SnoozeRequested;


        // =============================================================
        // CONSTRUCTION
        // =============================================================

        public AlarmWindow(Alarm alarm)
        {
            InitializeComponent();

            _alarm = alarm;

            AlarmNameText.Text =
                (string.IsNullOrWhiteSpace(alarm.Name)
                    ? "Alarm"
                    : alarm.Name)
                .ToUpperInvariant();

            SourceText.Text = alarm.DescribeSource();

            SnoozeButton.Visibility =
                alarm.SnoozeEnabled && alarm.SnoozeMinutes > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            SnoozeDetailText.Text =
                $"{alarm.SnoozeMinutes} MINUTE" +
                (alarm.SnoozeMinutes == 1 ? "" : "S");

            ApplyTheme(ThemeService.Current.Theme);

            ThemeService.Current.ThemeChanged += ThemeService_ThemeChanged;

            _renderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };

            _renderTimer.Tick += (_, _) => Render();

            Loaded += AlarmWindow_Loaded;
        }


        private void AlarmWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowHelper.ForceToForeground(this);

            Render();

            _renderTimer.Start();
        }


        // =============================================================
        // THEME
        // =============================================================

        private void ThemeService_ThemeChanged(
            object? sender,
            AlarmTheme theme)
        {
            ApplyTheme(theme);
        }


        private void ApplyTheme(AlarmTheme theme)
        {
            RootGrid.Background =
                ThemeService.BuildAlarmGradient(theme);

            FrameBorder.BorderBrush =
                new SolidColorBrush(theme.PanelBorder);

            // A YouTube alarm has already given the clock a smaller
            // slice of the screen; a theme change must not take the
            // video's room back.
            if (_player == null)
            {
                ClockText.FontSize = theme.ClockFontSize;

                ClockText.Effect = ThemeService.BuildClockGlow(theme);
            }
            else
            {
                ClockText.FontSize =
                    Math.Min(theme.ClockFontSize, 66);
            }
        }


        // =============================================================
        // STATE FROM THE ENGINE
        // =============================================================

        public void ApplyState(AlarmSessionState state)
        {
            _state = state;

            Render();
        }


        // =============================================================
        // RENDER
        // =============================================================

        private void Render()
        {
            DateTime now = DateTime.Now;

            ClockText.Text = TimeHelper.FormatTime(now, showSeconds: true);

            AlarmSessionState? state = _state;

            if (state == null)
            {
                RemainingText.Text = "Starting…";
                return;
            }

            bool playing = state.Phase == AlarmPhase.Playing;

            if (state.Phase != _lastRenderedPhase)
            {
                _lastRenderedPhase = state.Phase;
                ApplyPhaseChrome(playing);
            }

            CycleText.Text = $"CYCLE {state.CycleNumber}";

            SourceText.Text = state.SourceDescription;


            // ---------------------------------------------------------
            // COUNTDOWN
            // ---------------------------------------------------------

            TimeSpan remaining =
                state.RemainingAt(DateTime.UtcNow);

            string label =
                playing
                    ? (state.IsFinalCycle ? "Alarm ends in " : "Sound ends in ")
                    : "Next sound in ";

            RemainingText.Text =
                label + TimeHelper.FormatCountdown(remaining);


            // ---------------------------------------------------------
            // PROGRESS
            // ---------------------------------------------------------

            UpdateProgress(state.ProgressAt(DateTime.UtcNow));
        }


        private void ApplyPhaseChrome(bool playing)
        {
            AlarmTheme theme = ThemeService.Current.Theme;

            Color accent =
                playing ? theme.PlayingAccent : theme.SilenceAccent;

            SolidColorBrush accentBrush = new SolidColorBrush(accent);

            PhaseIcon.Text = playing ? "🔊" : "🔇";
            PhaseText.Text = playing ? "PLAYING" : "SILENCE";
            PhaseText.Foreground = accentBrush;
            PhaseBadge.BorderBrush = accentBrush;
            ProgressFill.Background = accentBrush;

            // A slow pulse while the sound is going, still while it is
            // not. It reads at a glance from across a dark room.
            PhaseBadge.BeginAnimation(OpacityProperty, null);

            if (playing)
            {
                DoubleAnimation pulse =
                    new DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.45,
                        Duration = TimeSpan.FromMilliseconds(900),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };

                PhaseBadge.BeginAnimation(OpacityProperty, pulse);
            }
            else
            {
                PhaseBadge.Opacity = 1.0;
            }
        }


        private void UpdateProgress(double progress)
        {
            double width = ProgressTrack.ActualWidth;

            if (width <= 0)
                return;

            ProgressFill.Width =
                Math.Clamp(progress, 0.0, 1.0) * width;
        }


        private void ProgressTrack_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            if (_state != null)
                UpdateProgress(_state.ProgressAt(DateTime.UtcNow));
        }


        // =============================================================
        // YOUTUBE
        // =============================================================

        /// <summary>
        /// Creates the YouTube surface inside the alarm screen. Called
        /// by the engine on the UI thread when the alarm's source is a
        /// YouTube link.
        /// </summary>
        public YouTubePlayerControl AttachYouTubePlayer()
        {
            if (_player != null)
                return _player;

            _player = new YouTubePlayerControl();

            _player.Failed += (_, message) =>
            {
                // The alarm keeps running and the screen keeps counting
                // down; only the video is missing. The control shows
                // the reason in place of the picture.
                SourceText.Text = "YOUTUBE • UNAVAILABLE";
            };

            VideoHost.Child = _player;
            VideoHost.Visibility = Visibility.Visible;

            // Hand the video the leftover height and stop centering the
            // block, so the picture fills the screen rather than the
            // clock floating in the middle of it.
            VideoRow.Height = new GridLength(1, GridUnitType.Star);

            CenterArea.VerticalAlignment = VerticalAlignment.Stretch;

            ClockText.FontSize =
                Math.Min(ThemeService.Current.Theme.ClockFontSize, 66);

            ClockText.Effect = null;

            AlarmNameText.FontSize = 22;

            return _player;
        }


        // =============================================================
        // ACTIONS
        // =============================================================

        private void SnoozeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_alarm.SnoozeEnabled || _alarm.SnoozeMinutes <= 0)
                return;

            _stopRaised = true;

            SnoozeRequested?.Invoke(_alarm.SnoozeMinutes);
        }


        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseStop();
        }


        private void RaiseStop()
        {
            if (_stopRaised)
                return;

            _stopRaised = true;

            StopRequested?.Invoke(this, EventArgs.Empty);
        }


        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                RaiseStop();

                e.Handled = true;

                return;
            }

            base.OnKeyDown(e);
        }


        // =============================================================
        // SHUTDOWN
        // =============================================================

        /// <summary>
        /// Closes the screen on the engine's instruction. Distinct from
        /// the user closing it, which has to be treated as STOP.
        /// </summary>
        public void CloseFromEngine()
        {
            _closingFromEngine = true;

            Close();
        }


        protected override void OnClosed(EventArgs e)
        {
            _renderTimer.Stop();

            ThemeService.Current.ThemeChanged -= ThemeService_ThemeChanged;

            _player?.Shutdown();
            _player = null;

            /*
             * Alt+F4, or anything else that closes this window behind
             * the engine's back, must not leave the audio running.
             */
            if (!_closingFromEngine)
                RaiseStop();

            base.OnClosed(e);
        }
    }
}
