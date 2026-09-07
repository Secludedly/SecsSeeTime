using System;
using SecSeeTime.Enums;

namespace SecSeeTime.Models
{
    /// <summary>
    /// An immutable snapshot of what the alarm engine is doing right now.
    ///
    /// The engine publishes one of these on every phase change. The UI
    /// derives its countdown and progress bar from the phase deadline
    /// instead of running its own competing timer, so the screen can
    /// never disagree with the audio.
    /// </summary>
    public sealed class AlarmSessionState
    {
        public Guid AlarmId { get; init; }

        public string AlarmName { get; init; } = "Alarm";

        public AlarmPhase Phase { get; init; } = AlarmPhase.Idle;

        /// <summary>Where this phase began, in UTC.</summary>
        public DateTime PhaseStartedUtc { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Where this phase ends, in UTC. <see cref="DateTime.MaxValue"/>
        /// for phases that have no scheduled end.
        /// </summary>
        public DateTime PhaseEndsUtc { get; init; } = DateTime.MaxValue;

        /// <summary>1-based index of the current sound cycle.</summary>
        public int CycleNumber { get; init; } = 1;

        /// <summary>Human-readable description of the audio source.</summary>
        public string SourceDescription { get; init; } = "";

        public bool SnoozeEnabled { get; init; }

        public int SnoozeMinutes { get; init; }

        /// <summary>True when no further sound cycle will follow this one.</summary>
        public bool IsFinalCycle { get; init; }


        // =============================================================
        // DERIVED VALUES
        // =============================================================

        public bool HasDeadline =>
            PhaseEndsUtc != DateTime.MaxValue;


        public TimeSpan TotalPhaseDuration =>
            HasDeadline
                ? PhaseEndsUtc - PhaseStartedUtc
                : TimeSpan.Zero;


        /// <summary>Time left in this phase, never negative.</summary>
        public TimeSpan RemainingAt(DateTime utcNow)
        {
            if (!HasDeadline)
                return TimeSpan.Zero;

            TimeSpan remaining = PhaseEndsUtc - utcNow;

            return remaining < TimeSpan.Zero
                ? TimeSpan.Zero
                : remaining;
        }


        /// <summary>
        /// How far through this phase we are, from 0.0 to 1.0.
        /// The bar fills up as the phase elapses.
        /// </summary>
        public double ProgressAt(DateTime utcNow)
        {
            TimeSpan total = TotalPhaseDuration;

            if (total <= TimeSpan.Zero)
                return 0.0;

            double elapsed =
                (utcNow - PhaseStartedUtc).TotalSeconds /
                total.TotalSeconds;

            return Math.Clamp(elapsed, 0.0, 1.0);
        }


        public DateTime? PhaseEndsLocal =>
            HasDeadline
                ? PhaseEndsUtc.ToLocalTime()
                : null;
    }
}
