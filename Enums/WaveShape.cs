namespace SecSeeTime.Enums
{
    /// <summary>
    /// Oscillator shapes available to the synthesizer.
    /// </summary>
    public enum WaveShape
    {
        /// <summary>Pure and soft.</summary>
        Sine,

        /// <summary>Hard and buzzy. Classic alarm clock.</summary>
        Square,

        /// <summary>Hollow, between sine and square.</summary>
        Triangle,

        /// <summary>Bright and harsh. Horns and sirens.</summary>
        Saw,

        /// <summary>Unpitched. Rain, wind, static.</summary>
        Noise
    }


    /// <summary>
    /// Partial sets layered over the fundamental. This is what makes a
    /// bell sound like a bell rather than a beep.
    /// </summary>
    public enum Timbre
    {
        /// <summary>The fundamental alone.</summary>
        Pure,

        /// <summary>Inharmonic partials with a long decay.</summary>
        Bell,

        /// <summary>Stacked octaves and a fifth.</summary>
        Organ,

        /// <summary>Odd harmonics. Nasal and cutting.</summary>
        Reed,

        /// <summary>High, clangy, slightly detuned.</summary>
        Metallic
    }
}
