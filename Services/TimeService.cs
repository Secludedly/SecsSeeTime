using System;
using System.Windows.Threading;
using SecSeeTime.Helpers;

namespace SecSeeTime.Services
{
    /// <summary>
    /// The Time Engine.
    ///
    /// One authoritative clock tick for the whole application, so every
    /// surface that shows the time updates on the same beat rather than
    /// each view spinning up its own timer.
    /// </summary>
    public sealed class TimeService : IDisposable
    {
        private readonly DispatcherTimer _timer;

        private bool _disposed;


        public TimeService(int intervalMilliseconds = 100)
        {
            _timer = new DispatcherTimer
            {
                Interval =
                    TimeSpan.FromMilliseconds(
                        Math.Clamp(intervalMilliseconds, 16, 1000))
            };

            _timer.Tick += (_, _) => Tick?.Invoke(this, Now);
        }


        // =============================================================
        // EVENTS
        // =============================================================

        public event EventHandler<DateTime>? Tick;


        // =============================================================
        // TIME
        // =============================================================

        public DateTime Now => DateTime.Now;

        public DateTime UtcNow => DateTime.UtcNow;

        public TimeZoneInfo LocalZone => TimeZoneInfo.Local;


        public string ZoneLabel =>
            TimeHelper.CurrentZoneLabel(LocalZone, Now);


        // =============================================================
        // LIFETIME
        // =============================================================

        public void Start()
        {
            if (_disposed || _timer.IsEnabled)
                return;

            _timer.Start();
        }


        public void Stop()
        {
            if (_timer.IsEnabled)
                _timer.Stop();
        }


        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            Stop();
        }
    }
}
