using SecSeeTime.Enums;
using SecSeeTime.Helpers;
using SecSeeTime.Models;
using SecSeeTime.Services;
using SecSeeTime.Views;
using SecSeeTime.Themes;
using SecSeeTime.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;

namespace SecSeeTime
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _particleTimer;

        private readonly Random _random = new Random();

        private readonly List<Particle> _particles = new List<Particle>();

        private readonly StorageService _storage;
        private readonly AlarmSettings _settings;
        private readonly TimeService _timeService;
        private readonly SoundLibraryService _soundLibrary;
        private readonly AudioPreviewService _audioPreview;
        private readonly AlarmService _alarmService;
        private readonly AlarmPlaybackService _alarmPlaybackService;

        private List<WorldClockTile> _worldClocks = new List<WorldClockTile>();

        private bool _isMaximized;

        private TrayService? _trayService;

        private bool _allowRealClose;

        private readonly TaskbarItemInfo _taskbarItemInfo = new TaskbarItemInfo
        {
            Description = "Sec's See Time"
        };


        // =============================================================
        // CONSTRUCTOR
        // =============================================================

        public MainWindow()
        {
            InitializeComponent();


            // ---------------------------------------------------------
            // STORAGE + SETTINGS
            // ---------------------------------------------------------

            _storage = new StorageService();

            _settings = _storage.LoadSettings();

            // Before anything formats a time.
            TimeFormatService.Mode = _settings.TimeFormat;

            TaskbarItemInfo = _taskbarItemInfo;

            _trayService = new TrayService(
                showClock: ShowFromTray,
                openAlarms: OpenAlarmsFromTray,
                openWorldClock: OpenWorldClockFromTray,
                openAppearance: OpenAppearanceFromTray,
                getStartWithWindows: () => _settings.StartWithWindows,
                setStartWithWindows: enabled =>
                {
                    _settings.StartWithWindows = enabled;
                    StartupService.SetEnabled(enabled);
                    _storage.SaveSettings(_settings);
                },
                getMinimizeToTray: () => _settings.MinimizeToTray,
                setMinimizeToTray: enabled =>
                {
                    _settings.MinimizeToTray = enabled;
                    _storage.SaveSettings(_settings);
                },
                exit: ExitApplication);

            // The tray icon is the delivery channel for Windows notifications.
            ((App)Application.Current).Notifications?.AttachSink(
                (title, message) => _trayService?.ShowBalloon(title, message) == true);


            // ---------------------------------------------------------
            // TIME ENGINE
            // ---------------------------------------------------------

            _timeService = new TimeService();

            _timeService.Tick += TimeService_Tick;


            // ---------------------------------------------------------
            // SOUND LIBRARY + PREVIEW
            // ---------------------------------------------------------

            _soundLibrary = new SoundLibraryService();

            _audioPreview = new AudioPreviewService(_soundLibrary);


            // ---------------------------------------------------------
            // ALARM ENGINE
            // ---------------------------------------------------------

            _alarmService = new AlarmService(_storage)
            {
                MissedAlarmGrace =
                    TimeSpan.FromSeconds(_settings.MissedAlarmGraceSeconds)
            };

            _alarmService.AlarmTriggered += AlarmService_AlarmTriggered;

            _alarmService.AlarmsChanged += AlarmService_AlarmsChanged;

            _alarmPlaybackService =
                new AlarmPlaybackService(
                    new AudioService(_soundLibrary));

            _alarmPlaybackService.StateChanged +=
                AlarmPlaybackService_StateChanged;


            // ---------------------------------------------------------
            // PARTICLE TIMER
            // ---------------------------------------------------------

            _particleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };

            _particleTimer.Tick += ParticleTimer_Tick;


            // ---------------------------------------------------------
            // THEME ENGINE
            //
            // Applied last: ApplyTheme rebuilds the particle field, so
            // the particle timer has to exist before the first theme
            // change lands.
            // ---------------------------------------------------------

            ThemeService.Current.ThemeChanged += ThemeService_ThemeChanged;

            AlarmTheme startupTheme = ThemePresets.Get(_settings.ThemeName);
            var customThemes = new ThemePersistenceService().Load();
            var customTheme = customThemes.Find(t => string.Equals(t.Name, _settings.ThemeName, StringComparison.OrdinalIgnoreCase));
            ThemeService.Current.Apply((customTheme ?? startupTheme).Clone());


            // ---------------------------------------------------------
            // WINDOW EVENTS
            // ---------------------------------------------------------

            Loaded += MainWindow_Loaded;

            SizeChanged += MainWindow_SizeChanged;

            StateChanged += MainWindow_StateChanged;

            Closing += MainWindow_Closing;
        }


        // =============================================================
        // WINDOW INITIALIZATION
        // =============================================================

        private void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            ApplyTheme(ThemeService.Current.Theme);

            RebuildWorldClocks();

            UpdateClock(DateTime.Now);

            UpdateInternetStatus();

            _timeService.Start();

            _alarmService.Start();

            RefreshAlarmDisplays();

            if (_settings.StartWithWindows)
                StartupService.SetEnabled(true);

            bool launchedByWindows =
                Environment.GetCommandLineArgs()
                    .Any(arg => string.Equals(
                        arg,
                        "--startup",
                        StringComparison.OrdinalIgnoreCase));

            if (launchedByWindows && _settings.MinimizeToTray)
                Dispatcher.BeginInvoke(new Action(HideToTray));
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


        /// <summary>
        /// Applies the parts of a theme that a resource lookup cannot
        /// express: the background gradient, the clock glow, and the
        /// particle field.
        /// </summary>
        private void ApplyTheme(AlarmTheme theme)
        {
            BackgroundLayer.Background =
                ThemeService.BuildWindowGradient(theme);

            GlowTopRight.Fill = BuildGlow(theme.Accent);
            GlowBottomLeft.Fill = BuildGlow(theme.AccentLight);

            ClockText.FontFamily = new FontFamily(theme.ClockFontFamily);
            ClockText.FontSize = theme.ClockFontSize;
            ClockText.FontWeight = theme.ClockFontWeight;
            ClockText.FontStyle = theme.ClockItalic ? FontStyles.Italic : FontStyles.Normal;
            ClockText.LetterSpacing = theme.ClockLetterSpacing;
            ClockText.Opacity = Math.Clamp(theme.ClockOpacity, 0.15, 1.0);
            ClockText.Effect = ThemeService.BuildClockGlow(theme);
            ClockText.OutlineEnabled = theme.ClockOutlineEnabled;
            ClockText.OutlineThickness = theme.ClockOutlineThickness;
            ClockText.OutlineColor = new SolidColorBrush(theme.ClockOutlineColor);

            DateText.Visibility = theme.ShowDate ? Visibility.Visible : Visibility.Collapsed;
            ScanlineLayer.Opacity = theme.ScanlinesEnabled ? Math.Clamp(theme.ScanlineOpacity, 0, 1) : 0;
            NoiseLayer.Opacity = theme.NoiseEnabled ? 0.035 : 0;

            BackgroundOverlayLayer.Fill = new SolidColorBrush(theme.BackgroundOverlayColor);
            BackgroundOverlayLayer.Opacity = Math.Clamp(theme.BackgroundOverlayOpacity, 0, 1);
            BackgroundImageLayer.Opacity = 0;
            if (!string.IsNullOrWhiteSpace(theme.BackgroundImagePath) && System.IO.File.Exists(theme.BackgroundImagePath))
            {
                try
                {
                    BackgroundImageLayer.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(theme.BackgroundImagePath, UriKind.Absolute));
                    BackgroundImageLayer.Stretch = theme.BackgroundImageStretch;
                    BackgroundImageLayer.Opacity = Math.Clamp(theme.BackgroundImageOpacity, 0, 1);
                }
                catch { BackgroundImageLayer.Source = null; }
            }

            ThemeNameText.Text = theme.Name.ToUpperInvariant();

            ApplyWorldClockRail(theme);

            CreateParticles();

            if (_settings.ThemeName != theme.Name)
            {
                _settings.ThemeName = theme.Name;

                _storage.SaveSettings(_settings);
            }
        }


        private static Brush BuildGlow(Color color)
        {
            RadialGradientBrush brush = new RadialGradientBrush();

            brush.GradientStops.Add(new GradientStop(color, 0));
            brush.GradientStops.Add(new GradientStop(color, 0.35));
            brush.GradientStops.Add(new GradientStop(Colors.Transparent, 1));

            brush.Freeze();

            return brush;
        }


        // =============================================================
        // CLOCK
        // =============================================================

        private void TimeService_Tick(object? sender, DateTime now)
        {
            UpdateClock(now);
        }


        private void UpdateClock(DateTime now)
        {
            AlarmTheme theme = ThemeService.Current.Theme;
            ClockText.Text = TimeHelper.FormatClock(
                now,
                TimeFormatService.Use24Hour(theme.Use24Hour),
                theme.ShowSeconds);
            DateText.Text = TimeHelper.FormatDate(now);

            TimeZoneText.Text = _timeService.ZoneLabel;

            UpdateWorldClocks(theme);

            UpdateEngineStatus(now);
        }


        // =============================================================
        // ALARM ENGINE
        // =============================================================

        private void AlarmService_AlarmTriggered(
            object? sender,
            AlarmTriggeredEventArgs e)
        {
            // Single gate for the preference, so the fallback below cannot
            // sneak a notification out after the user switched them off.
            if (_settings.NotificationsEnabled)
            {
                // NotificationService delivers through the tray icon, so only
                // fall back to a direct balloon if it had no channel attached.
                bool notified =
                    ((App)Application.Current).Notifications?.ShowAlarm(e.Alarm) == true;

                if (!notified)
                {
                    _trayService?.ShowBalloon(
                        "Sec's See Time",
                        $"Alarm ringing: {e.Alarm.Name}");
                }
            }

            _alarmPlaybackService.Start(e.Alarm);

            RefreshAlarmDisplays();
        }


        private void AlarmService_AlarmsChanged(
            object? sender,
            EventArgs e)
        {
            RefreshAlarmDisplays();
        }


        private void AlarmPlaybackService_StateChanged(
            object? sender,
            AlarmSessionState? state)
        {
            RefreshAlarmDisplays();
        }


        private void EngineStopButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // One button for both cases: it stops a sounding alarm and
            // cancels a pending snooze.
            _alarmPlaybackService.Stop();

            RefreshAlarmDisplays();
        }


        // =============================================================
        // ALARM DISPLAY
        // =============================================================

        private void RefreshAlarmDisplays()
        {
            int count = _alarmService.ActiveAlarmCount;

            AlarmCountText.Text = $"{count} ACTIVE";

            StatusAlarmText.Text =
                $"{count} ALARM" + (count == 1 ? "" : "S");

            UpdateEngineStatus(DateTime.Now);
        }


        /// <summary>
        /// The one place that answers "what is the alarm system doing
        /// right now?" for the main window.
        /// </summary>
        private void UpdateEngineStatus(DateTime now)
        {
            UpdateTaskbarStatus(now);

            AlarmSessionState? state =
                _alarmPlaybackService.CurrentState;

            AlarmTheme theme = ThemeService.Current.Theme;


            // ---------------------------------------------------------
            // AN ALARM IS ON SCREEN
            // ---------------------------------------------------------

            if (state != null &&
                state.Phase is AlarmPhase.Playing or AlarmPhase.Silence)
            {
                bool playing = state.Phase == AlarmPhase.Playing;

                EngineStatusText.Text =
                    (playing ? "🔊 PLAYING • " : "🔇 SILENCE • ") +
                    state.AlarmName.ToUpperInvariant() +
                    "   " +
                    TimeHelper.FormatCountdown(
                        state.RemainingAt(DateTime.UtcNow));

                EngineStatusDot.Foreground =
                    new SolidColorBrush(
                        playing ? theme.PlayingAccent : theme.SilenceAccent);

                EngineStopButton.Content = "STOP";
                EngineStopButton.Visibility = Visibility.Visible;

                NextAlarmDot.Foreground =
                    new SolidColorBrush(theme.PlayingAccent);

                NextAlarmText.Text = "ALARM IN PROGRESS";

                return;
            }


            // ---------------------------------------------------------
            // SNOOZED
            // ---------------------------------------------------------

            if (state != null && state.Phase == AlarmPhase.Snoozed)
            {
                DateTime? endsAt = state.PhaseEndsLocal;

                EngineStatusText.Text =
                    "😴 SNOOZED • " +
                    state.AlarmName.ToUpperInvariant() +
                    (endsAt == null
                        ? ""
                        : "   " + TimeHelper.FormatCountdown(
                            state.RemainingAt(DateTime.UtcNow)));

                EngineStatusDot.Foreground =
                    new SolidColorBrush(theme.AccentLight);

                EngineStopButton.Content = "CANCEL";
                EngineStopButton.Visibility = Visibility.Visible;

                NextAlarmDot.Foreground =
                    new SolidColorBrush(theme.AccentLight);

                NextAlarmText.Text =
                    endsAt == null
                        ? "SNOOZED"
                        : ("SNOOZED UNTIL " +
                           TimeHelper.FormatTime(endsAt.Value))
                          .ToUpperInvariant();

                return;
            }


            // ---------------------------------------------------------
            // IDLE
            // ---------------------------------------------------------

            EngineStatusText.Text = "CLOCK RUNNING";

            EngineStatusDot.Foreground =
                new SolidColorBrush(theme.Success);

            EngineStopButton.Visibility = Visibility.Collapsed;

            (Alarm Alarm, DateTime DueAt)? next =
                _alarmService.GetNextAlarm(now);

            if (next == null)
            {
                NextAlarmDot.Foreground =
                    new SolidColorBrush(theme.TextMuted);

                NextAlarmText.Text = "NO ALARMS SCHEDULED";

                return;
            }

            NextAlarmDot.Foreground =
                new SolidColorBrush(theme.Success);

            NextAlarmText.Text =
                string.Format(
                    "NEXT ALARM • {0} • {1}",
                    TimeHelper.FormatTime(next.Value.DueAt),
                    TimeHelper.DescribeTimeUntil(next.Value.DueAt, now))
                .ToUpperInvariant();
        }


        private void UpdateTaskbarStatus(DateTime now)
        {
            AlarmSessionState? state = _alarmPlaybackService.CurrentState;

            if (state != null &&
                state.Phase is AlarmPhase.Playing or AlarmPhase.Silence)
            {
                _taskbarItemInfo.Description =
                    $"Sec's See Time • ALARM: {state.AlarmName}";
                _taskbarItemInfo.ProgressState =
                    TaskbarItemProgressState.Indeterminate;
                return;
            }

            if (state != null && state.Phase == AlarmPhase.Snoozed)
            {
                _taskbarItemInfo.Description =
                    $"Sec's See Time • SNOOZED: {state.AlarmName}";
                _taskbarItemInfo.ProgressState =
                    TaskbarItemProgressState.Paused;
                return;
            }

            var next = _alarmService.GetNextAlarm(now);

            if (next != null)
            {
                _taskbarItemInfo.Description =
                    "Sec's See Time • Next alarm " +
                    TimeHelper.FormatTime(next.Value.DueAt);
            }
            else
            {
                _taskbarItemInfo.Description =
                    "Sec's See Time • No alarms scheduled";
            }

            _taskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
        }


        // =============================================================
        // PARTICLE SYSTEM
        // =============================================================

        private void CreateParticles()
        {
            ParticleCanvas.Children.Clear();

            _particles.Clear();

            AlarmTheme theme = ThemeService.Current.Theme;

            if (!theme.ParticlesEnabled || theme.ParticleCount <= 0)
            {
                _particleTimer.Stop();

                return;
            }

            double width = Math.Max(ActualWidth, 900);

            double height = Math.Max(ActualHeight, 600);

            Color particleColor = theme.AccentLight;

            for (int i = 0; i < theme.ParticleCount; i++)
            {
                double size = _random.NextDouble() * 2.5 + 0.5;

                Ellipse particle =
                    new Ellipse
                    {
                        Width = size,
                        Height = size,

                        Fill =
                            new SolidColorBrush(
                                Color.FromArgb(
                                    (byte)_random.Next(25, 90),
                                    particleColor.R,
                                    particleColor.G,
                                    particleColor.B)),

                        IsHitTestVisible = false
                    };

                Canvas.SetLeft(particle, _random.NextDouble() * width);

                Canvas.SetTop(particle, _random.NextDouble() * height);

                ParticleCanvas.Children.Add(particle);

                _particles.Add(
                    new Particle
                    {
                        Element = particle,

                        X = Canvas.GetLeft(particle),

                        Y = Canvas.GetTop(particle),

                        SpeedX = (_random.NextDouble() - 0.5) * 0.12,

                        SpeedY = (_random.NextDouble() - 0.5) * 0.12
                    });
            }

            _particleTimer.Start();
        }


        private void ParticleTimer_Tick(
            object? sender,
            EventArgs e)
        {
            double width = ParticleCanvas.ActualWidth;

            if (width <= 0)
                width = ActualWidth;

            double height = ParticleCanvas.ActualHeight;

            if (height <= 0)
                height = ActualHeight;

            foreach (Particle particle in _particles)
            {
                particle.X += particle.SpeedX;

                particle.Y += particle.SpeedY;

                if (particle.X < -5)
                    particle.X = width + 5;

                if (particle.X > width + 5)
                    particle.X = -5;

                if (particle.Y < -5)
                    particle.Y = height + 5;

                if (particle.Y > height + 5)
                    particle.Y = -5;

                Canvas.SetLeft(particle.Element, particle.X);

                Canvas.SetTop(particle.Element, particle.Y);
            }
        }


        // =============================================================
        // INTERNET STATUS
        // =============================================================

        private void UpdateInternetStatus()
        {
            bool connected =
                NetworkInterface.GetIsNetworkAvailable();

            AlarmTheme theme = ThemeService.Current.Theme;

            InternetIndicator.Foreground =
                new SolidColorBrush(
                    connected ? theme.Success : theme.Danger);

            InternetStatusText.Text =
                connected ? "ONLINE" : "OFFLINE";
        }


        // =============================================================
        // WINDOW DRAGGING
        // =============================================================

        private void TitleBar_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();

                return;
            }

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


        public void ShowFromTray()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(ShowFromTray));
                return;
            }

            Show();
            WindowState = WindowState.Normal;
            Activate();

            // Bring the window above other windows once
            Topmost = true;
            Topmost = false;
            Focus();
        }


        private void HideToTray()
        {
            Hide();
        }


        private void ExitApplication()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(ExitApplication));
                return;
            }

            _allowRealClose = true;
            Close();
        }


        private void OpenAlarmsFromTray()
        {
            ShowFromTray();
            AlarmsButton_Click(this, new RoutedEventArgs());
        }


        private void OpenWorldClockFromTray()
        {
            ShowFromTray();
            WorldClockButton_Click(this, new RoutedEventArgs());
        }


        private void OpenAppearanceFromTray()
        {
            ShowFromTray();
            ThemesButton_Click(this, new RoutedEventArgs());
        }


        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && _settings.MinimizeToTray)
                HideToTray();
        }


        // =============================================================
        // MINIMIZE
        // =============================================================

        private void MinimizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }


        // =============================================================
        // MAXIMIZE / RESTORE
        // =============================================================

        private void MaximizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ToggleMaximize();
        }


        private void ToggleMaximize()
        {
            if (!_isMaximized)
            {
                WindowState = WindowState.Maximized;

                _isMaximized = true;

                MaximizeIcon.Text = "❐";
            }
            else
            {
                WindowState = WindowState.Normal;

                _isMaximized = false;

                MaximizeIcon.Text = "□";
            }
        }


        // =============================================================
        // CLOSE
        // =============================================================

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_settings.MinimizeToTray)
            {
                HideToTray();
                return;
            }

            ExitApplication();
        }


        // =============================================================
        // ALARMS BUTTON
        // =============================================================

        private void AlarmsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AlarmManagerWindow alarmManager =
                new AlarmManagerWindow(
                    _alarmService,
                    _soundLibrary,
                    _audioPreview)
                {
                    Owner = this
                };

            alarmManager.ShowDialog();

            RefreshAlarmDisplays();
        }


        // =============================================================
        // WORLD CLOCK
        // =============================================================

        private void WorldClockButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            new WorldClockWindow(_settings, _storage) { Owner = this }.ShowDialog();

            // The dialog edits the same settings instance, so the rail
            // only needs rebuilding once it closes.
            RebuildWorldClocks();

            UpdateClock(DateTime.Now);
        }


        /// <summary>
        /// Rebuilds the secondary clock rail from the saved cities, and
        /// hides it entirely when there are none.
        /// </summary>
        private void RebuildWorldClocks()
        {
            _settings.WorldClocks ??= new List<WorldClockEntry>();

            _worldClocks = new List<WorldClockTile>();

            foreach (WorldClockEntry entry in _settings.WorldClocks)
                _worldClocks.Add(new WorldClockTile(entry));

            WorldClockStrip.ItemsSource = _worldClocks;

            ApplyWorldClockRail(ThemeService.Current.Theme);
        }


        /// <summary>
        /// The rail's theme-driven shape: which side it sits on, how
        /// solid it is, and whether it shows at all. Card typography is
        /// handled by the resources ThemeService pushes, because the
        /// cards live inside a DataTemplate.
        /// </summary>
        private void ApplyWorldClockRail(AlarmTheme theme)
        {
            WorldClockRail.Visibility =
                theme.WorldClockEnabled && _worldClocks.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            WorldClockRail.Opacity =
                Math.Clamp(theme.WorldClockOpacity, 0.2, 1.0);

            if (theme.WorldClockOnLeft)
            {
                ClockColumn.Width = new GridLength(1, GridUnitType.Auto);
                RailColumn.Width = new GridLength(1, GridUnitType.Star);

                Grid.SetColumn(WorldClockRail, 0);
                Grid.SetColumn(ClockStack, 1);

                WorldClockRail.Margin = new Thickness(28, 0, 0, 0);
            }
            else
            {
                ClockColumn.Width = new GridLength(1, GridUnitType.Star);
                RailColumn.Width = new GridLength(1, GridUnitType.Auto);

                Grid.SetColumn(ClockStack, 0);
                Grid.SetColumn(WorldClockRail, 1);

                WorldClockRail.Margin = new Thickness(0, 0, 28, 0);
            }
        }


        private void UpdateWorldClocks(AlarmTheme theme)
        {
            if (_worldClocks.Count == 0)
                return;

            DateTime utc = DateTime.UtcNow;

            foreach (WorldClockTile tile in _worldClocks)
                tile.Update(
                    utc,
                    TimeFormatService.Use24Hour(theme.Use24Hour),
                    theme.ShowSeconds);
        }


        // =============================================================
        // THEMES
        // =============================================================

        private void ThemesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            new AppearanceStudioWindow { Owner = this }.ShowDialog();
            ApplyTheme(ThemeService.Current.Theme);
        }


        // =============================================================
        // SETTINGS
        // =============================================================

        private void SettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            new SettingsWindow(
                _settings,
                _storage,
                onChanged: ApplySettings,
                sendTestNotification: () =>
                    _trayService?.ShowBalloon(
                        "Sec's See Time",
                        "Notifications are working.") == true)
            {
                Owner = this
            }
            .ShowDialog();
        }


        /// <summary>
        /// Pushes preferences into the live services. Called on every edit in
        /// the Settings window rather than once on close, because that dialog
        /// is modal and an alarm can fire while it is open.
        /// </summary>
        private void ApplySettings()
        {
            _alarmService.MissedAlarmGrace =
                TimeSpan.FromSeconds(_settings.MissedAlarmGraceSeconds);

            // The tray menu carries its own copies of these two toggles.
            _trayService?.SyncToggles(
                _settings.StartWithWindows,
                _settings.MinimizeToTray);

            TimeFormatService.Mode = _settings.TimeFormat;

            // Repaint everything that shows a time, rather than waiting for
            // the next tick, so the change reads as instant.
            DateTime now = DateTime.Now;

            UpdateClock(now);
            UpdateEngineStatus(now);   // also refreshes the taskbar text
            UpdateWorldClocks(ThemeService.Current.Theme);
            RefreshAlarmDisplays();
        }


        // =============================================================
        // WINDOW SIZE
        // =============================================================

        private void MainWindow_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (ParticleCanvas.ActualWidth <= 0 ||
                ParticleCanvas.ActualHeight <= 0)
            {
                return;
            }

            if (e.PreviousSize.Width > 0 &&
                Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 200)
            {
                CreateParticles();
            }
        }


        // =============================================================
        // CLEANUP
        // =============================================================

        private void MainWindow_Closing(
            object? sender,
            CancelEventArgs e)
        {
            // With minimize-to-tray enabled, closing the window means
            // "hide the UI" rather than "terminate the alarm engine".
            if (!_allowRealClose && _settings.MinimizeToTray)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }

            _particleTimer.Stop();

            _timeService.Dispose();

            ThemeService.Current.ThemeChanged -= ThemeService_ThemeChanged;

            /*
             * These services are disposed only on a real application exit.
             * Hiding the main window must leave the alarm scheduler alive.
             */
            _alarmPlaybackService.Dispose();

            _audioPreview.Dispose();

            _alarmService.Dispose();

            _trayService?.Dispose();

            _storage.SaveSettings(_settings);
        }


        // =============================================================
        // PARTICLE CLASS
        // =============================================================

        private sealed class Particle
        {
            public Ellipse Element { get; set; } = null!;

            public double X { get; set; }

            public double Y { get; set; }

            public double SpeedX { get; set; }

            public double SpeedY { get; set; }
        }
    }
}
