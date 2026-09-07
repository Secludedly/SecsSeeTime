using System;
using System.Collections.Generic;
using SecSeeTime.Enums;
using SecSeeTime.Models;

namespace SecSeeTime.Services
{
    /// <summary>
    /// The free premade sound library.
    ///
    /// Every sound here is a recipe, not an audio file. They cost
    /// nothing to ship, carry no license, and adding one is a few
    /// lines rather than a download.
    /// </summary>
    public static class BuiltInSounds
    {
        /*
         * Bump this when a recipe changes. It is part of the cache file
         * name, so an edited sound is re-rendered instead of the stale
         * version being played forever.
         */
        public const int RecipeVersion = 1;


        public const string DefaultSoundName = "Classic Alarm";


        // Equal temperament, A4 = 440.
        private const double C5 = 523.25;
        private const double D5 = 587.33;
        private const double E5 = 659.25;
        private const double G5 = 783.99;
        private const double A5 = 880.00;
        private const double B5 = 987.77;
        private const double C6 = 1046.50;
        private const double D6 = 1174.66;
        private const double E6 = 1318.51;
        private const double G6 = 1567.98;
        private const double A6 = 1760.00;
        private const double C7 = 2093.00;


        public static IReadOnlyList<SoundDefinition> All { get; } =
            Build();


        private static List<SoundDefinition> Build()
        {
            return new List<SoundDefinition>
            {
                // =================================================
                // ALARMS
                // =================================================

                Sound(
                    "Classic Alarm",
                    SoundCategory.Alarms,
                    "The one everybody knows. Hard square wave.",
                    Pattern(
                        WaveShape.Square,
                        tone: 0.30,
                        gap: 0.13,
                        gain: 0.9,
                        A5, A5, E5, A5)),

                Sound(
                    "Digital Beep",
                    SoundCategory.Alarms,
                    "Clean two-tone beep. Wristwatch energy.",
                    Pattern(
                        WaveShape.Sine,
                        tone: 0.22,
                        gap: 0.12,
                        gain: 1.0,
                        A5, D6, A5)),

                Sound(
                    "Electronic Pulse",
                    SoundCategory.Alarms,
                    "Throbbing low-high pattern with tremolo.",
                    PatternTremolo(
                        WaveShape.Square,
                        tone: 0.18,
                        gap: 0.10,
                        gain: 0.85,
                        tremoloHz: 18,
                        tremoloDepth: 0.5,
                        440, 440, 660, 660)),

                Sound(
                    "Rapid Beep",
                    SoundCategory.Alarms,
                    "Fast and urgent. Very hard to sleep through.",
                    Pattern(
                        WaveShape.Sine,
                        tone: 0.08,
                        gap: 0.08,
                        gain: 1.0,
                        1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000)),

                Sound(
                    "Siren",
                    SoundCategory.Alarms,
                    "Continuous rising and falling wail.",
                    new SoundRecipe
                    {
                        LoopSeconds = 2.0,
                        MasterGain = 0.85,
                        Steps =
                        {
                            new SoundStep
                            {
                                Start = 0,
                                Duration = 1.0,
                                Frequency = 500,
                                EndFrequency = 1200,
                                Shape = WaveShape.Saw,
                                Timbre = Timbre.Pure,
                                Attack = 0.02,
                                Release = 0.0
                            },
                            new SoundStep
                            {
                                Start = 1.0,
                                Duration = 1.0,
                                Frequency = 1200,
                                EndFrequency = 500,
                                Shape = WaveShape.Saw,
                                Timbre = Timbre.Pure,
                                Attack = 0.0,
                                Release = 0.02
                            }
                        }
                    }),

                Sound(
                    "Klaxon",
                    SoundCategory.Alarms,
                    "Harsh reedy blast. Submarine dive alarm.",
                    PatternTimbre(
                        WaveShape.Saw,
                        tone: 0.40,
                        gap: 0.20,
                        gain: 0.8,
                        timbre: Timbre.Reed,
                        220, 220)),

                Sound(
                    "Air Horn",
                    SoundCategory.Alarms,
                    "Long, flat, obnoxious. Use responsibly.",
                    new SoundRecipe
                    {
                        LoopSeconds = 1.6,
                        MasterGain = 0.8,
                        Steps =
                        {
                            Blast(0, 1.1, 180),
                            Blast(0, 1.1, 240),
                            Blast(0, 1.1, 359)
                        }
                    }),


                // =================================================
                // CHIMES
                // =================================================

                Sound(
                    "Morning Bells",
                    SoundCategory.Chimes,
                    "Bright ascending bells. The friendly default.",
                    Struck(
                        loop: 2.9,
                        spacing: 0.55,
                        duration: 1.2,
                        decay: 2.4,
                        Timbre.Bell,
                        E5, G5, B5, G5)),

                Sound(
                    "Gentle Wake",
                    SoundCategory.Chimes,
                    "Slow swelling tones. Eases you out of sleep.",
                    new SoundRecipe
                    {
                        LoopSeconds = 4.2,
                        MasterGain = 0.75,
                        Steps =
                        {
                            Swell(0.0, 1.8, C5),
                            Swell(1.2, 1.8, E5),
                            Swell(2.4, 1.8, G5)
                        }
                    }),

                Sound(
                    "Music Box",
                    SoundCategory.Chimes,
                    "Delicate high bells. Childhood in a tin.",
                    Struck(
                        loop: 2.4,
                        spacing: 0.30,
                        duration: 0.9,
                        decay: 4.5,
                        Timbre.Bell,
                        C6, E6, G6, C7, G6, E6)),

                Sound(
                    "Temple Bell",
                    SoundCategory.Chimes,
                    "One deep strike, left to ring out.",
                    new SoundRecipe
                    {
                        LoopSeconds = 5.0,
                        MasterGain = 0.9,
                        Steps =
                        {
                            new SoundStep
                            {
                                Start = 0,
                                Duration = 4.6,
                                Frequency = 196,
                                Shape = WaveShape.Sine,
                                Timbre = Timbre.Bell,
                                Attack = 0.004,
                                Release = 0.5,
                                Sustain = 0.0,
                                DecayRate = 0.85
                            }
                        }
                    }),

                Sound(
                    "Xylophone",
                    SoundCategory.Chimes,
                    "Wooden pentatonic run. Cheerful, not shrill.",
                    Struck(
                        loop: 1.9,
                        spacing: 0.22,
                        duration: 0.55,
                        decay: 7.0,
                        Timbre.Metallic,
                        C5, D5, E5, G5, A5, G5, E5)),

                Sound(
                    "Harp Rise",
                    SoundCategory.Chimes,
                    "Soft arpeggio climbing an octave.",
                    Struck(
                        loop: 3.0,
                        spacing: 0.26,
                        duration: 1.4,
                        decay: 2.0,
                        Timbre.Organ,
                        C5, E5, G5, C6, E6, G6)),


                // =================================================
                // ELECTRONIC
                // =================================================

                Sound(
                    "Synth Riser",
                    SoundCategory.Electronic,
                    "Sweeping build that never quite lands.",
                    new SoundRecipe
                    {
                        LoopSeconds = 2.2,
                        MasterGain = 0.8,
                        Steps =
                        {
                            new SoundStep
                            {
                                Start = 0,
                                Duration = 2.0,
                                Frequency = 200,
                                EndFrequency = 1600,
                                Shape = WaveShape.Saw,
                                Timbre = Timbre.Pure,
                                Attack = 0.2,
                                Release = 0.25,
                                TremoloHz = 12,
                                TremoloDepth = 0.35
                            }
                        }
                    }),

                Sound(
                    "Arcade",
                    SoundCategory.Electronic,
                    "Chiptune blips. Insert coin.",
                    Pattern(
                        WaveShape.Square,
                        tone: 0.10,
                        gap: 0.06,
                        gain: 0.75,
                        C6, E6, G6, C7, G6, E6)),

                Sound(
                    "Data Alert",
                    SoundCategory.Electronic,
                    "Two-tone notification with a pause between.",
                    new SoundRecipe
                    {
                        // The pattern is only a third of a second, so
                        // the loop is padded: without the rest it runs
                        // together into one continuous trill.
                        LoopSeconds = 1.5,
                        MasterGain = 0.9,
                        Steps =
                        {
                            Blip(0.00, 1200),
                            Blip(0.18, 1600),
                            Blip(0.50, 1200),
                            Blip(0.68, 1600)
                        }
                    }),

                Sound(
                    "Sonar Ping",
                    SoundCategory.Electronic,
                    "Single ping fading into the dark.",
                    new SoundRecipe
                    {
                        LoopSeconds = 3.0,
                        MasterGain = 0.85,
                        Steps =
                        {
                            new SoundStep
                            {
                                Start = 0,
                                Duration = 2.6,
                                Frequency = 900,
                                Shape = WaveShape.Sine,
                                Timbre = Timbre.Pure,
                                Attack = 0.003,
                                Release = 0.4,
                                Sustain = 0.0,
                                DecayRate = 2.2
                            }
                        }
                    }),


                // =================================================
                // AMBIENT
                // =================================================

                Sound(
                    "Rain",
                    SoundCategory.Ambient,
                    "Steady rainfall with occasional drips.",
                    new SoundRecipe
                    {
                        LoopSeconds = 6.0,
                        MasterGain = 0.7,
                        Bed = new NoiseBed
                        {
                            Gain = 0.55,
                            LowpassHz = 3200,
                            HighpassHz = 480
                        },
                        Steps = Droplets(6.0)
                    }),

                Sound(
                    "Ocean Waves",
                    SoundCategory.Ambient,
                    "Slow swells rolling in and back out.",
                    new SoundRecipe
                    {
                        LoopSeconds = 9.0,
                        MasterGain = 0.75,
                        Bed = new NoiseBed
                        {
                            Gain = 0.6,
                            LowpassHz = 750,
                            SwellCycles = 2,
                            SwellDepth = 0.8
                        }
                    }),

                Sound(
                    "Wind",
                    SoundCategory.Ambient,
                    "Gusts through a gap in the window.",
                    new SoundRecipe
                    {
                        LoopSeconds = 7.0,
                        MasterGain = 0.65,
                        Bed = new NoiseBed
                        {
                            Gain = 0.5,
                            LowpassHz = 1300,
                            HighpassHz = 260,
                            SwellCycles = 3,
                            SwellDepth = 0.6
                        }
                    }),

                Sound(
                    "White Noise",
                    SoundCategory.Ambient,
                    "Flat static. Masks everything.",
                    new SoundRecipe
                    {
                        LoopSeconds = 4.0,
                        MasterGain = 0.55,
                        Bed = new NoiseBed { Gain = 0.6 }
                    }),

                Sound(
                    "Brown Noise",
                    SoundCategory.Ambient,
                    "Deep rumble. Easier on the ears than white.",
                    new SoundRecipe
                    {
                        LoopSeconds = 4.0,
                        MasterGain = 0.8,
                        Bed = new NoiseBed
                        {
                            Gain = 0.6,
                            LowpassHz = 190
                        }
                    }),

                Sound(
                    "Crickets",
                    SoundCategory.Ambient,
                    "Summer night outside the window.",
                    new SoundRecipe
                    {
                        LoopSeconds = 4.0,
                        MasterGain = 0.6,
                        Bed = new NoiseBed
                        {
                            Gain = 0.05,
                            LowpassHz = 900
                        },
                        Steps = Chirps(4.0)
                    })
            };
        }


        // =============================================================
        // BUILDERS
        // =============================================================

        private static SoundDefinition Sound(
            string name,
            SoundCategory category,
            string description,
            SoundRecipe recipe)
        {
            return new SoundDefinition
            {
                Id = name,
                Name = name,
                Category = category,
                Description = description,
                Kind = SoundKind.BuiltIn,
                Recipe = recipe
            };
        }


        /*
         * Distinct names rather than overloads: with a params double[]
         * tail, `Pattern(shape, t, g, gain, 440, 880)` could bind the
         * frequencies to a tremolo parameter instead, and the compiler
         * would happily pick the wrong one.
         */

        /// <summary>
        /// A sequence of equal-length tones separated by equal gaps.
        /// Covers most of the alarm-shaped sounds.
        /// </summary>
        private static SoundRecipe Pattern(
            WaveShape shape,
            double tone,
            double gap,
            double gain,
            params double[] frequencies)
        {
            return BuildPattern(
                shape, tone, gap, gain, Timbre.Pure, 0, 0, frequencies);
        }


        private static SoundRecipe PatternTremolo(
            WaveShape shape,
            double tone,
            double gap,
            double gain,
            double tremoloHz,
            double tremoloDepth,
            params double[] frequencies)
        {
            return BuildPattern(
                shape, tone, gap, gain, Timbre.Pure,
                tremoloHz, tremoloDepth, frequencies);
        }


        private static SoundRecipe PatternTimbre(
            WaveShape shape,
            double tone,
            double gap,
            double gain,
            Timbre timbre,
            params double[] frequencies)
        {
            return BuildPattern(
                shape, tone, gap, gain, timbre, 0, 0, frequencies);
        }


        private static SoundRecipe BuildPattern(
            WaveShape shape,
            double tone,
            double gap,
            double gain,
            Timbre timbre,
            double tremoloHz,
            double tremoloDepth,
            double[] frequencies)
        {
            List<SoundStep> steps = new();

            double cursor = 0;

            foreach (double frequency in frequencies)
            {
                steps.Add(
                    new SoundStep
                    {
                        Start = cursor,
                        Duration = tone,
                        Frequency = frequency,
                        Shape = shape,
                        Timbre = timbre,
                        Gain = gain,
                        Attack = 0.010,
                        Release = 0.025,
                        TremoloHz = tremoloHz,
                        TremoloDepth = tremoloDepth
                    });

                cursor += tone + gap;
            }

            return new SoundRecipe
            {
                LoopSeconds = cursor,
                MasterGain = 0.9,
                Steps = steps
            };
        }


        /// <summary>
        /// Struck-and-ringing notes: bells, mallets, plucked strings.
        /// </summary>
        private static SoundRecipe Struck(
            double loop,
            double spacing,
            double duration,
            double decay,
            Timbre timbre,
            params double[] frequencies)
        {
            List<SoundStep> steps = new();

            double cursor = 0;

            foreach (double frequency in frequencies)
            {
                steps.Add(
                    new SoundStep
                    {
                        Start = cursor,
                        Duration = duration,
                        Frequency = frequency,
                        Shape = WaveShape.Sine,
                        Timbre = timbre,
                        Attack = 0.004,
                        Release = 0.12,
                        Sustain = 0.0,
                        DecayRate = decay
                    });

                cursor += spacing;
            }

            return new SoundRecipe
            {
                LoopSeconds = loop,
                MasterGain = 0.9,
                Steps = steps
            };
        }


        private static SoundStep Swell(
            double start,
            double duration,
            double frequency)
        {
            return new SoundStep
            {
                Start = start,
                Duration = duration,
                Frequency = frequency,
                Shape = WaveShape.Sine,
                Timbre = Timbre.Organ,
                Attack = duration * 0.45,
                Release = duration * 0.45,
                Gain = 0.9
            };
        }


        private static SoundStep Blip(
            double start,
            double frequency)
        {
            return new SoundStep
            {
                Start = start,
                Duration = 0.11,
                Frequency = frequency,
                Shape = WaveShape.Triangle,
                Timbre = Timbre.Pure,
                Attack = 0.008,
                Release = 0.03,
                Gain = 0.9
            };
        }


        private static SoundStep Blast(
            double start,
            double duration,
            double frequency)
        {
            return new SoundStep
            {
                Start = start,
                Duration = duration,
                Frequency = frequency,
                Shape = WaveShape.Saw,
                Timbre = Timbre.Reed,
                Attack = 0.03,
                Release = 0.06,
                Gain = 0.45
            };
        }


        /// <summary>
        /// Sparse high transients scattered over the rain bed.
        /// Seeded so the sound is identical every render.
        /// </summary>
        private static List<SoundStep> Droplets(double loop)
        {
            List<SoundStep> steps = new();

            Random random = new Random(4242);

            // Kept clear of the loop end so the crossfade does not
            // clip a drip in half.
            double usable = loop - 0.4;

            for (int i = 0; i < 14; i++)
            {
                steps.Add(
                    new SoundStep
                    {
                        Start = random.NextDouble() * usable,
                        Duration = 0.09,
                        Frequency = 1400 + (random.NextDouble() * 2200),
                        Shape = WaveShape.Sine,
                        Timbre = Timbre.Pure,
                        Attack = 0.001,
                        Release = 0.05,
                        Sustain = 0.0,
                        DecayRate = 40,
                        Gain = 0.12 + (random.NextDouble() * 0.10)
                    });
            }

            return steps;
        }


        /// <summary>
        /// Cricket chirps: short bursts in groups, with a pause.
        /// </summary>
        private static List<SoundStep> Chirps(double loop)
        {
            List<SoundStep> steps = new();

            Random random = new Random(1337);

            double cursor = 0.2;

            while (cursor < loop - 0.6)
            {
                int burst = 3 + random.Next(3);

                for (int i = 0; i < burst; i++)
                {
                    steps.Add(
                        new SoundStep
                        {
                            Start = cursor,
                            Duration = 0.016,
                            Frequency = 4000 + (random.NextDouble() * 500),
                            Shape = WaveShape.Square,
                            Timbre = Timbre.Pure,
                            Attack = 0.002,
                            Release = 0.006,
                            Gain = 0.30
                        });

                    cursor += 0.032;
                }

                cursor += 0.35 + (random.NextDouble() * 0.5);
            }

            return steps;
        }
    }
}
