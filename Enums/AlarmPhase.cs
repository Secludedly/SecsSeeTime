namespace SecSeeTime.Enums
{
    /// <summary>
    /// The states an alarm session can be in.
    ///
    /// The alarm engine owns this value. Every UI surface reads it
    /// rather than trying to infer what the audio is doing.
    /// </summary>
    public enum AlarmPhase
    {
        /// <summary>No alarm session exists.</summary>
        Idle,

        /// <summary>The alarm sound is currently playing.</summary>
        Playing,

        /// <summary>The alarm is between sound cycles.</summary>
        Silence,

        /// <summary>The alarm was snoozed and is waiting to fire again.</summary>
        Snoozed,

        /// <summary>The alarm was stopped, or its cycle finished.</summary>
        Dismissed
    }
}
