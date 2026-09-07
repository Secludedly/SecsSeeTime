using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using SecSeeTime.Enums;
using SecSeeTime.Models;

namespace SecSeeTime.Services
{
    /// <summary>
    /// The scheduler.
    ///
    /// It knows nothing about windows, buttons or audio. It watches the
    /// clock and says "this alarm is due"; what happens next is someone
    /// else's problem.
    /// </summary>
    public sealed class AlarmService : IDisposable
    {
        private readonly DispatcherTimer _timer;
        private readonly StorageService _storage;

        private readonly List<Alarm> _alarms = new();

        private readonly Dictionary<Guid, DateTime> _lastTriggered = new();

        private DateTime _lastCheck = DateTime.MinValue;

        private bool _disposed;


        /// <summary>
        /// How late an alarm may be and still fire. Without this, a
        /// laptop opened at noon would blast an alarm that was due at
        /// seven in the morning.
        /// </summary>
        public TimeSpan MissedAlarmGrace { get; set; } =
            TimeSpan.FromMinutes(5);


        // =============================================================
        // CONSTRUCTOR
        // =============================================================

        public AlarmService(StorageService storage)
        {
            _storage = storage;

            _alarms.AddRange(_storage.LoadAlarms());

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };

            _timer.Tick += Timer_Tick;
        }


        // =============================================================
        // EVENTS
        // =============================================================

        public event EventHandler<AlarmTriggeredEventArgs>? AlarmTriggered;

        /// <summary>Raised whenever the alarm collection changes.</summary>
        public event EventHandler? AlarmsChanged;


        // =============================================================
        // ALARM COLLECTION
        // =============================================================

        public IReadOnlyList<Alarm> Alarms => _alarms;


        public int ActiveAlarmCount =>
            _alarms.Count(alarm => alarm.IsEnabled);


        // =============================================================
        // START / STOP
        // =============================================================

        public void Start()
        {
            if (_disposed || _timer.IsEnabled)
                return;

            _lastCheck = DateTime.Now;

            _timer.Start();
        }


        public void Stop()
        {
            if (_timer.IsEnabled)
                _timer.Stop();
        }


        // =============================================================
        // MUTATIONS
        // =============================================================

        public void AddAlarm(Alarm alarm)
        {
            if (_alarms.Any(existing => existing.Id == alarm.Id))
                return;

            _alarms.Add(alarm);

            Commit();
        }


        public bool RemoveAlarm(Guid alarmId)
        {
            Alarm? alarm =
                _alarms.FirstOrDefault(existing => existing.Id == alarmId);

            if (alarm == null)
                return false;

            _alarms.Remove(alarm);

            _lastTriggered.Remove(alarmId);

            Commit();

            return true;
        }


        public bool SetAlarmEnabled(Guid alarmId, bool enabled)
        {
            Alarm? alarm =
                _alarms.FirstOrDefault(existing => existing.Id == alarmId);

            if (alarm == null)
                return false;

            alarm.IsEnabled = enabled;

            Commit();

            return true;
        }


        public Alarm? GetAlarm(Guid alarmId)
        {
            return _alarms.FirstOrDefault(alarm => alarm.Id == alarmId);
        }


        /// <summary>
        /// Persists the current alarms and notifies listeners. Call
        /// after editing an alarm in place.
        /// </summary>
        public void Commit()
        {
            _storage.SaveAlarms(_alarms);

            AlarmsChanged?.Invoke(this, EventArgs.Empty);
        }


        // =============================================================
        // NEXT ALARM
        // =============================================================

        /// <summary>
        /// The soonest enabled alarm, or null when nothing is scheduled.
        /// </summary>
        public (Alarm Alarm, DateTime DueAt)? GetNextAlarm(DateTime? from = null)
        {
            DateTime reference = from ?? DateTime.Now;

            (Alarm Alarm, DateTime DueAt)? best = null;

            foreach (Alarm alarm in _alarms)
            {
                if (!alarm.IsEnabled)
                    continue;

                DateTime? next = alarm.GetNextOccurrence(reference);

                if (next == null)
                    continue;

                if (best == null || next.Value < best.Value.DueAt)
                    best = (alarm, next.Value);
            }

            return best;
        }


        // =============================================================
        // TIMER
        // =============================================================

        private void Timer_Tick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            if (_lastCheck == DateTime.MinValue)
            {
                _lastCheck = now;
                return;
            }

            // Clock moved backward (DST, manual change, resume).
            if (now < _lastCheck)
            {
                _lastCheck = now;
                return;
            }

            CheckAlarms(_lastCheck, now);

            _lastCheck = now;
        }


        // =============================================================
        // CHECK
        // =============================================================

        /// <summary>
        /// Fires every alarm whose scheduled moment falls in the window
        /// that just elapsed.
        ///
        /// Matching a window rather than an exact second is what makes
        /// this reliable: a busy dispatcher, a garbage collection pause
        /// or a brief sleep can all swallow the one tick an
        /// equality check would have needed.
        /// </summary>
        private void CheckAlarms(DateTime windowStart, DateTime now)
        {
            DateTime earliestAllowed = now - MissedAlarmGrace;

            foreach (Alarm alarm in _alarms.ToList())
            {
                if (!alarm.IsEnabled)
                    continue;

                // Yesterday's slot as well, so a window that straddles
                // midnight does not lose an alarm.
                foreach (DateTime occurrence in Occurrences(alarm, now))
                {
                    if (occurrence <= windowStart || occurrence > now)
                        continue;

                    if (!alarm.ShouldRunOn(occurrence))
                        continue;

                    if (_lastTriggered.TryGetValue(alarm.Id, out DateTime previous) &&
                        previous == occurrence)
                    {
                        continue;
                    }

                    _lastTriggered[alarm.Id] = occurrence;

                    // Too stale to be useful, but recorded so it does
                    // not fire on the next tick either.
                    if (occurrence < earliestAllowed)
                        continue;

                    AlarmTriggered?.Invoke(
                        this,
                        new AlarmTriggeredEventArgs(alarm));

                    if (alarm.RepeatMode == AlarmRepeatMode.Once)
                    {
                        alarm.IsEnabled = false;
                        Commit();
                    }

                    break;
                }
            }
        }


        private static IEnumerable<DateTime> Occurrences(
            Alarm alarm,
            DateTime now)
        {
            yield return alarm.GetDateTime(now.Date.AddDays(-1));

            yield return alarm.GetDateTime(now.Date);
        }


        // =============================================================
        // DISPOSE
        // =============================================================

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            Stop();

            _storage.SaveAlarms(_alarms);
        }
    }
}
