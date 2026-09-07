using System.Collections.Generic;
using SecSeeTime.Enums;

namespace SecSeeTime.Models
{
    /// <summary>
    /// The instructions for synthesizing one built-in sound.
    ///
    /// A recipe describes a single seamless loop. The alarm engine
    /// repeats it for as long as the sound phase lasts, so
    /// <see cref="LoopSeconds"/> is the length of the pattern, not the
    /// length of the alarm.
    /// </summary>
    public sealed class SoundRecipe
    {
        /// <summary>Length of one loop, in seconds.</summary>
        public double LoopSeconds { get; init; } = 2.0;


        /// <summary>Discrete tone events: beeps, bell strikes, chirps.</summary>
        public List<SoundStep> Steps { get; init; } = new();


        /// <summary>An optional continuous noise layer underneath.</summary>
        public NoiseBed? Bed { get; init; }


        /// <summary>
        /// Overall level before the alarm's own volume is applied.
        /// Used to even out loudness across the library.
        /// </summary>
        public double MasterGain { get; init; } = 1.0;
    }


    /// <summary>
    /// One pitched event inside a recipe.
    /// </summary>
    public sealed class SoundStep
    {
        /// <summary>Offset from the start of the loop, in seconds.</summary>
        public double Start { get; init; }

        public double Duration { get; init; } = 0.25;

        public double Frequency { get; init; } = 880.0;

        /// <summary>
        /// Target frequency for a glide. Zero means hold
        /// <see cref="Frequency"/>.
        /// </summary>
        public double EndFrequency { get; init; }

        public WaveShape Shape { get; init; } = WaveShape.Sine;

        public Timbre Timbre { get; init; } = Timbre.Pure;

        public double Gain { get; init; } = 1.0;


        // -------------------------------------------------------------
        // ENVELOPE
        // -------------------------------------------------------------

        public double Attack { get; init; } = 0.008;

        public double Release { get; init; } = 0.04;

        /// <summary>
        /// Level the tone decays toward, 0 to 1. Zero gives a
        /// percussive hit; one holds flat.
        /// </summary>
        public double Sustain { get; init; } = 1.0;

        /// <summary>How fast it decays toward sustain.</summary>
        public double DecayRate { get; init; } = 0.0;


        // -------------------------------------------------------------
        // MODULATION
        // -------------------------------------------------------------

        public double TremoloHz { get; init; }

        public double TremoloDepth { get; init; }
    }


    /// <summary>
    /// A continuous filtered-noise layer. Rain, ocean, wind, static.
    /// </summary>
    public sealed class NoiseBed
    {
        public double Gain { get; init; } = 0.3;

        /// <summary>One-pole low-pass corner. Zero disables it.</summary>
        public double LowpassHz { get; init; }

        /// <summary>One-pole high-pass corner. Zero disables it.</summary>
        public double HighpassHz { get; init; }

        /// <summary>Slow amplitude swell, in cycles per loop.</summary>
        public double SwellCycles { get; init; }

        /// <summary>How deep the swell cuts, 0 to 1.</summary>
        public double SwellDepth { get; init; }
    }
}
