using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SecSeeTime.Controls;
using SecSeeTime.Enums;
using SecSeeTime.Helpers;
using SecSeeTime.Models;
using SecSeeTime.Views;

namespace SecSeeTime.Services
{
    /// <summary>
    /// The Alarm Engine.
    ///
    /// It owns the PLAYING / SILENCE state machine, the alarm screen,
    /// and any pending snooze. Everything about a firing alarm can be
    /// killed from one place, which is the whole point:
    ///
    ///     STOP  ->  audio stopped
    ///               YouTube stopped
    ///               session canceled
    ///               snooze canceled
    ///               window closed
    ///               timers canceled
    ///
    /// Two safeguards keep zombie alarms out. Snooze has its own
    /// cancellation source, separate from the sounding session, so
    /// stopping the alarm or closing the app also kills the pending
    /// re-trigger. And every session carries a generation number, so a
    /// loop that is already unwinding can never touch the audio or the
    /// screen belonging to a newer one.
    /// </summary>
    public sealed class AlarmPlaybackService : IDisposable
    {
        private readonly AudioService _audioService;
        private readonly object _sync = new();

        private CancellationTokenSource? _sessionCts;
        private CancellationTokenSource? _snoozeCts;

        private int _generation;

        private AlarmWindow? _window;
        private AlarmSessionState? _state;

        private Alarm? _snoozeAlarm;
        private DateTime? _snoozeEndsUtc;

        private bool _disposed;


        public AlarmPlaybackService(AudioService audioService)
        {
            _audioService = audioService;
        }


        // =============================================================
        // OBSERVABLE STATE
        // =============================================================

        /// <summary>
        /// Raised whenever the engine's state changes. The payload is
        /// null once nothing is happening.
        /// </summary>
        public event EventHandler<AlarmSessionState?>? StateChanged;


        public AlarmSessionState? CurrentState
        {
            get
            {
                lock (_sync)
                {
                    return _state;
                }
            }
        }


        public bool IsAlarmSounding
        {
            get
            {
                lock (_sync)
                {
                    return _state?.Phase is
                        AlarmPhase.Playing or AlarmPhase.Silence;
                }
            }
        }


        public bool HasPendingSnooze
        {
            get
            {
                lock (_sync)
                {
                    return _snoozeCts != null;
                }
            }
        }


        public DateTime? SnoozeEndsLocal
        {
            get
            {
                lock (_sync)
                {
                    return _snoozeEndsUtc?.ToLocalTime();
                }
            }
        }


        public string? PendingSnoozeAlarmName
        {
            get
            {
                lock (_sync)
                {
                    return _snoozeAlarm?.Name;
                }
            }
        }


        // =============================================================
        // START
        // =============================================================

        public void Start(Alarm alarm)
        {
            if (_disposed || alarm == null)
                return;

            /*
             * A fresh trigger always supersedes whatever is on screen.
             * It also clears a pending snooze for the same alarm, so an
             * old countdown cannot fire a second copy a minute later.
             * A snooze belonging to a different alarm is left alone.
             */

            CancelSession(closeWindow: true);

            CancelSnoozeFor(alarm.Id);

            int generation;
            CancellationToken token;

            lock (_sync)
            {
                _generation++;
                generation = _generation;

                _sessionCts = new CancellationTokenSource();
                token = _sessionCts.Token;
            }

            // The session loop must never run on the dispatcher.
            _ = Task.Run(
                () => RunSessionAsync(alarm, generation, token));
        }


        // =============================================================
        // SESSION LOOP
        // =============================================================

        private async Task RunSessionAsync(
            Alarm alarm,
            int generation,
            CancellationToken token)
        {
            IAlarmSource? source = null;

            try
            {
                AlarmWindow? window =
                    await CreateWindowAsync(alarm, generation)
                        .ConfigureAwait(false);

                if (window == null || IsStale(generation))
                    return;

                source =
                    await CreateSourceAsync(alarm, window, generation)
                        .ConfigureAwait(false);

                if (source == null || IsStale(generation))
                    return;

                int soundSeconds =
                    Math.Max(5, alarm.SoundDurationSeconds);

                int silenceSeconds =
                    Math.Max(5, alarm.SilenceDurationSeconds);

                int cycle = 1;

                while (!token.IsCancellationRequested)
                {
                    // ==========================================
                    // PLAYING
                    // ==========================================

                    PublishPhase(
                        alarm,
                        generation,
                        AlarmPhase.Playing,
                        soundSeconds,
                        cycle,
                        isFinalCycle: !alarm.RepeatSoundCycle);

                    await source.StartAsync(token).ConfigureAwait(false);

                    if (!await WaitAsync(soundSeconds, token).ConfigureAwait(false))
                        return;

                    await source.StopAsync().ConfigureAwait(false);


                    // ==========================================
                    // A ONE-SHOT ALARM ENDS HERE
                    // ==========================================

                    if (!alarm.RepeatSoundCycle)
                    {
                        CompleteSession(generation);
                        return;
                    }


                    // ==========================================
                    // SILENCE
                    // ==========================================

                    PublishPhase(
                        alarm,
                        generation,
                        AlarmPhase.Silence,
                        silenceSeconds,
                        cycle,
                        isFinalCycle: false);

                    if (!await WaitAsync(silenceSeconds, token).ConfigureAwait(false))
                        return;

                    cycle++;
                }
            }
            catch (OperationCanceledException)
            {
                // Normal Stop / Snooze behavior.
            }
            catch
            {
                // An alarm must never take the application down with it.
                CancelSession(closeWindow: true);
            }
            finally
            {
                if (source != null)
                {
                    try
                    {
                        await source.StopAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        // Nothing left to do; the audio engine has
                        // already been stopped by CancelSession.
                    }
                }
            }
        }


        /// <summary>
        /// Waits out one phase against a wall-clock deadline, checking
        /// often enough that Stop feels instant.
        /// </summary>
        private static async Task<bool> WaitAsync(
            int seconds,
            CancellationToken token)
        {
            DateTime deadline =
                DateTime.UtcNow.AddSeconds(seconds);

            try
            {
                while (true)
                {
                    TimeSpan remaining =
                        deadline - DateTime.UtcNow;

                    if (remaining <= TimeSpan.Zero)
                        return true;

                    if (remaining > TimeSpan.FromMilliseconds(200))
                        remaining = TimeSpan.FromMilliseconds(200);

                    await Task.Delay(remaining, token)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }


        // =============================================================
        // ALARM SCREEN
        // =============================================================

        private async Task<AlarmWindow?> CreateWindowAsync(
            Alarm alarm,
            int generation)
        {
            Application? app = Application.Current;

            if (app == null)
                return null;

            return await app.Dispatcher.InvokeAsync(() =>
            {
                if (IsStale(generation))
                    return null;

                try
                {
                    AlarmWindow window = new AlarmWindow(alarm);

                    window.StopRequested += AlarmWindow_StopRequested;

                    window.SnoozeRequested += minutes =>
                        Snooze(alarm, minutes);

                    lock (_sync)
                    {
                        _window = window;
                    }

                    window.Show();

                    WindowHelper.ForceToForeground(window);

                    return window;
                }
                catch
                {
                    // A UI failure must not silence the alarm.
                    return null;
                }
            });
        }


        private async Task<IAlarmSource?> CreateSourceAsync(
            Alarm alarm,
            AlarmWindow window,
            int generation)
        {
            if (alarm.SoundType == AlarmSoundType.YouTube &&
                !string.IsNullOrWhiteSpace(alarm.YouTubeUrl))
            {
                YouTubePlayerControl? player =
                    await window.Dispatcher.InvokeAsync(() =>
                    {
                        if (IsStale(generation))
                            return null;

                        try
                        {
                            return window.AttachYouTubePlayer();
                        }
                        catch
                        {
                            return null;
                        }
                    });

                if (player != null)
                    return new YouTubeAlarmSource(player, alarm);

                // Fall through: a broken player is still better than
                // an alarm that makes no attempt to wake you.
            }

            return new LocalAlarmSource(_audioService, alarm);
        }


        // =============================================================
        // PHASE PUBLICATION
        // =============================================================

        private void PublishPhase(
            Alarm alarm,
            int generation,
            AlarmPhase phase,
            int durationSeconds,
            int cycle,
            bool isFinalCycle)
        {
            DateTime start = DateTime.UtcNow;

            AlarmSessionState state =
                new AlarmSessionState
                {
                    AlarmId = alarm.Id,

                    AlarmName = string.IsNullOrWhiteSpace(alarm.Name)
                        ? "Alarm"
                        : alarm.Name,

                    Phase = phase,
                    PhaseStartedUtc = start,
                    PhaseEndsUtc = start.AddSeconds(durationSeconds),
                    CycleNumber = cycle,
                    SourceDescription = alarm.DescribeSource(),
                    SnoozeEnabled = alarm.SnoozeEnabled,
                    SnoozeMinutes = alarm.SnoozeMinutes,
                    IsFinalCycle = isFinalCycle
                };

            lock (_sync)
            {
                if (_generation != generation)
                    return;

                _state = state;
            }

            PushState(state);
        }


        private void PushState(AlarmSessionState? state)
        {
            Application? app = Application.Current;

            if (app == null)
                return;

            app.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(() =>
                {
                    AlarmWindow? window;

                    lock (_sync)
                    {
                        window = _window;
                    }

                    if (state != null)
                    {
                        try
                        {
                            window?.ApplyState(state);
                        }
                        catch
                        {
                            // The window may be closing.
                        }
                    }

                    StateChanged?.Invoke(this, state);
                }));
        }


        // =============================================================
        // STOP — everything, definitively
        // =============================================================

        public void Stop()
        {
            CancelSession(closeWindow: true);

            CancelSnooze();

            PushState(null);
        }


        private void AlarmWindow_StopRequested(
            object? sender,
            EventArgs e)
        {
            Stop();
        }


        /// <summary>
        /// Ends the current sounding session. Cancels the phase loop,
        /// stops the audio engine immediately, and closes the screen.
        /// Bumping the generation means anything still unwinding in the
        /// background is now inert.
        /// </summary>
        private void CancelSession(bool closeWindow)
        {
            CancellationTokenSource? cts;
            AlarmWindow? window;

            lock (_sync)
            {
                cts = _sessionCts;
                _sessionCts = null;

                _state = null;

                window = _window;

                if (closeWindow)
                    _window = null;

                _generation++;
            }

            /*
             * Canceled but not disposed on purpose. The session loop
             * may still be sitting inside Task.Delay with this token,
             * and disposing underneath it throws. The source holds no
             * unmanaged resources, so letting it be collected is safe.
             */
            try
            {
                cts?.Cancel();
            }
            catch
            {
                // Ignore cancellation errors.
            }

            // Kill the sound here and now rather than waiting for the
            // loop to notice it has been canceled.
            try
            {
                _audioService.Stop();
            }
            catch
            {
                // Audio shutdown must never break the engine.
            }

            if (closeWindow && window != null)
                CloseWindow(window);
        }


        private void CompleteSession(int generation)
        {
            if (IsStale(generation))
                return;

            CancelSession(closeWindow: true);

            PushState(null);
        }


        private static void CloseWindow(AlarmWindow window)
        {
            Application? app = Application.Current;

            if (app == null)
                return;

            app.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(() =>
                {
                    try
                    {
                        window.CloseFromEngine();
                    }
                    catch
                    {
                        // Ignore window shutdown errors.
                    }
                }));
        }


        // =============================================================
        // SNOOZE
        // =============================================================

        private void Snooze(Alarm alarm, int minutes)
        {
            CancelSession(closeWindow: true);

            if (_disposed || minutes <= 0)
            {
                PushState(null);
                return;
            }

            CancelSnooze();

            CancellationTokenSource cts = new CancellationTokenSource();

            DateTime start = DateTime.UtcNow;
            DateTime endsUtc = start.AddMinutes(minutes);

            AlarmSessionState state =
                new AlarmSessionState
                {
                    AlarmId = alarm.Id,

                    AlarmName = string.IsNullOrWhiteSpace(alarm.Name)
                        ? "Alarm"
                        : alarm.Name,

                    Phase = AlarmPhase.Snoozed,
                    PhaseStartedUtc = start,
                    PhaseEndsUtc = endsUtc,
                    SourceDescription = alarm.DescribeSource(),
                    SnoozeEnabled = alarm.SnoozeEnabled,
                    SnoozeMinutes = minutes
                };

            lock (_sync)
            {
                _snoozeCts = cts;
                _snoozeAlarm = alarm;
                _snoozeEndsUtc = endsUtc;
                _state = state;
            }

            PushState(state);

            _ = Task.Run(
                () => RunSnoozeAsync(alarm, cts, endsUtc));
        }


        private async Task RunSnoozeAsync(
            Alarm alarm,
            CancellationTokenSource cts,
            DateTime endsUtc)
        {
            bool completed;

            try
            {
                completed = await WaitUntilAsync(endsUtc, cts.Token)
                    .ConfigureAwait(false);
            }
            catch
            {
                completed = false;
            }

            lock (_sync)
            {
                // Superseded or canceled: whoever replaced us owns the
                // slot now, and has already cleared it.
                if (!ReferenceEquals(_snoozeCts, cts))
                    return;

                _snoozeCts = null;
                _snoozeAlarm = null;
                _snoozeEndsUtc = null;
            }

            try
            {
                cts.Dispose();
            }
            catch
            {
                // Ignore.
            }

            if (!completed || _disposed)
            {
                PushState(null);
                return;
            }

            Start(alarm);
        }


        private static async Task<bool> WaitUntilAsync(
            DateTime deadlineUtc,
            CancellationToken token)
        {
            try
            {
                while (true)
                {
                    TimeSpan remaining =
                        deadlineUtc - DateTime.UtcNow;

                    if (remaining <= TimeSpan.Zero)
                        return true;

                    if (remaining > TimeSpan.FromMilliseconds(500))
                        remaining = TimeSpan.FromMilliseconds(500);

                    await Task.Delay(remaining, token)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }


        /// <summary>
        /// Cancels any pending snooze. This is what stops a snoozed
        /// alarm from resurrecting itself after the user pressed STOP,
        /// or after Sec's See Time was closed.
        /// </summary>
        public void CancelSnooze()
        {
            CancellationTokenSource? cts;

            lock (_sync)
            {
                cts = _snoozeCts;

                _snoozeCts = null;
                _snoozeAlarm = null;
                _snoozeEndsUtc = null;
            }

            try
            {
                // Not disposed here; see CancelSession.
                cts?.Cancel();
            }
            catch
            {
                // Ignore cancellation errors.
            }
        }


        private void CancelSnoozeFor(Guid alarmId)
        {
            bool matches;

            lock (_sync)
            {
                matches = _snoozeAlarm?.Id == alarmId;
            }

            if (matches)
                CancelSnooze();
        }


        // =============================================================
        // HELPERS
        // =============================================================

        private bool IsStale(int generation)
        {
            lock (_sync)
            {
                return _generation != generation;
            }
        }


        // =============================================================
        // DISPOSE
        // =============================================================

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Order matters: kill the snooze first, so the session
            // teardown cannot race a re-trigger.
            CancelSnooze();

            CancelSession(closeWindow: true);

            _audioService.Dispose();
        }
    }
}
