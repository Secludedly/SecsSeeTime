using System;
using System.IO;
using System.Text;
using SecSeeTime.Enums;
using SecSeeTime.Models;

namespace SecSeeTime.Services
{
    /// <summary>
    /// Turns a <see cref="SoundRecipe"/> into a seamlessly looping WAV.
    ///
    /// Every built-in sound in Sec's See Time is generated here rather
    /// than shipped as an asset. Nothing to download, nothing to
    /// license, and a new sound costs a few lines of recipe.
    /// </summary>
    public static class ToneSynthesizer
    {
        public const int SampleRate = 44100;

        private const short Channels = 1;
        private const short BitsPerSample = 16;


        // =============================================================
        // PUBLIC ENTRY POINT
        // =============================================================

        /// <summary>
        /// Renders a recipe to a 16-bit mono WAV at the given volume.
        ///
        /// The volume is baked into the waveform because Windows
        /// SoundPlayer, which plays these back, has no volume control
        /// of its own.
        /// </summary>
        public static void RenderToWav(
            string path,
            SoundRecipe recipe,
            double volume)
        {
            double[] samples = Render(recipe);

            double amplitude = Math.Clamp(volume, 0.0, 1.0);

            WriteWav(path, samples, amplitude);
        }


        // =============================================================
        // RENDER
        // =============================================================

        private static double[] Render(SoundRecipe recipe)
        {
            int length =
                Math.Max(
                    SampleRate / 10,
                    (int)(recipe.LoopSeconds * SampleRate));

            /*
             * The loop is rendered with an overhanging tail, then the
             * tail is crossfaded back into the head. That is what stops
             * a repeating ambient bed from clicking every time it wraps.
             */
            int fade =
                Math.Min(
                    (int)(0.25 * SampleRate),
                    length / 4);

            double[] buffer = new double[length + fade];

            if (recipe.Bed != null)
                RenderBed(buffer, recipe.Bed, length);

            foreach (SoundStep step in recipe.Steps)
                RenderStep(buffer, step);

            FoldLoop(buffer, length, fade);

            double[] result = new double[length];

            Array.Copy(buffer, result, length);

            Normalize(result, recipe.MasterGain);

            return result;
        }


        private static void FoldLoop(double[] buffer, int length, int fade)
        {
            if (fade <= 0)
                return;

            for (int i = 0; i < fade; i++)
            {
                double t = i / (double)fade;

                buffer[i] = (buffer[i] * t) + (buffer[length + i] * (1.0 - t));
            }
        }


        // =============================================================
        // NOISE BED
        // =============================================================

        private static void RenderBed(
            double[] buffer,
            NoiseBed bed,
            int loopLength)
        {
            // Fixed seed: the same sound renders identically every time,
            // so a cached file and a fresh one are interchangeable.
            Random random = new Random(0x5EC5EE);

            double lowpassCoefficient = Coefficient(bed.LowpassHz);
            double highpassCoefficient = Coefficient(bed.HighpassHz);

            double lowpassState = 0;
            double highpassState = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                double sample = (random.NextDouble() * 2.0) - 1.0;

                if (lowpassCoefficient > 0)
                {
                    lowpassState += lowpassCoefficient * (sample - lowpassState);
                    sample = lowpassState;
                }

                if (highpassCoefficient > 0)
                {
                    highpassState += highpassCoefficient * (sample - highpassState);
                    sample -= highpassState;
                }

                double gain = bed.Gain;

                if (bed.SwellCycles > 0 && bed.SwellDepth > 0)
                {
                    double u = i / (double)loopLength;

                    double swell =
                        0.5 + (0.5 * Math.Cos(
                            2.0 * Math.PI * bed.SwellCycles * u));

                    gain *= 1.0 - (bed.SwellDepth * swell);
                }

                buffer[i] += sample * gain;
            }
        }


        private static double Coefficient(double cornerHz)
        {
            if (cornerHz <= 0)
                return 0;

            return 1.0 - Math.Exp(
                -2.0 * Math.PI * cornerHz / SampleRate);
        }


        // =============================================================
        // TONE STEPS
        // =============================================================

        private static void RenderStep(double[] buffer, SoundStep step)
        {
            int start = (int)(step.Start * SampleRate);

            int count = (int)(step.Duration * SampleRate);

            if (count <= 0 || start >= buffer.Length)
                return;

            (double Ratio, double Gain)[] partials =
                GetPartials(step.Timbre);

            double[] phases = new double[partials.Length];

            Random random = new Random(0x1CE);

            for (int i = 0; i < count; i++)
            {
                int index = start + i;

                if (index >= buffer.Length)
                    break;

                double t = i / (double)SampleRate;

                double progress = i / (double)count;


                // -----------------------------------------------------
                // FREQUENCY
                // -----------------------------------------------------

                double frequency =
                    step.EndFrequency > 0
                        ? step.Frequency +
                          ((step.EndFrequency - step.Frequency) * progress)
                        : step.Frequency;


                // -----------------------------------------------------
                // ENVELOPE
                // -----------------------------------------------------

                double envelope =
                    Envelope(t, step.Duration, step);

                if (step.TremoloHz > 0 && step.TremoloDepth > 0)
                {
                    double tremolo =
                        0.5 + (0.5 * Math.Sin(
                            2.0 * Math.PI * step.TremoloHz * t));

                    envelope *= 1.0 - (step.TremoloDepth * (1.0 - tremolo));
                }


                // -----------------------------------------------------
                // OSCILLATORS
                // -----------------------------------------------------

                double value = 0;

                for (int p = 0; p < partials.Length; p++)
                {
                    double partialFrequency = frequency * partials[p].Ratio;

                    phases[p] +=
                        2.0 * Math.PI * partialFrequency / SampleRate;

                    if (phases[p] > 2.0 * Math.PI)
                        phases[p] -= 2.0 * Math.PI;

                    /*
                     * Higher partials of a struck object die away first.
                     * Without this a bell sounds like an organ chord.
                     */
                    double partialDecay =
                        step.Timbre is Timbre.Bell or Timbre.Metallic
                            ? Math.Exp(-partials[p].Ratio * 0.7 * t)
                            : 1.0;

                    value +=
                        Oscillator(step.Shape, phases[p], random) *
                        partials[p].Gain *
                        partialDecay;
                }

                buffer[index] += value * envelope * step.Gain;
            }
        }


        private static double Envelope(
            double t,
            double duration,
            SoundStep step)
        {
            double envelope = 1.0;

            if (step.Attack > 0 && t < step.Attack)
                envelope = t / step.Attack;

            if (step.DecayRate > 0)
            {
                double decayed =
                    Math.Exp(
                        -step.DecayRate *
                        Math.Max(0, t - step.Attack));

                envelope *=
                    step.Sustain + ((1.0 - step.Sustain) * decayed);
            }

            double releaseStart = duration - step.Release;

            if (step.Release > 0 && t > releaseStart)
                envelope *= Math.Max(0, (duration - t) / step.Release);

            return envelope;
        }


        private static double Oscillator(
            WaveShape shape,
            double phase,
            Random random)
        {
            switch (shape)
            {
                case WaveShape.Square:
                    return Math.Sin(phase) >= 0 ? 0.7 : -0.7;

                case WaveShape.Triangle:
                    return 2.0 / Math.PI * Math.Asin(Math.Sin(phase));

                case WaveShape.Saw:
                    double normalized = phase / (2.0 * Math.PI);
                    return 2.0 * (normalized - Math.Floor(normalized + 0.5)) * 0.8;

                case WaveShape.Noise:
                    return (random.NextDouble() * 2.0) - 1.0;

                default:
                    return Math.Sin(phase);
            }
        }


        private static (double Ratio, double Gain)[] GetPartials(Timbre timbre)
        {
            return timbre switch
            {
                Timbre.Bell => new[]
                {
                    (1.00, 1.00),
                    (2.00, 0.48),
                    (2.76, 0.30),
                    (4.07, 0.18),
                    (5.43, 0.10)
                },

                Timbre.Organ => new[]
                {
                    (1.00, 1.00),
                    (2.00, 0.50),
                    (3.00, 0.26),
                    (4.00, 0.14)
                },

                Timbre.Reed => new[]
                {
                    (1.00, 1.00),
                    (3.00, 0.40),
                    (5.00, 0.22),
                    (7.00, 0.12)
                },

                Timbre.Metallic => new[]
                {
                    (1.00, 1.00),
                    (2.01, 0.60),
                    (3.03, 0.40),
                    (4.97, 0.28)
                },

                _ => new[] { (1.00, 1.00) }
            };
        }


        // =============================================================
        // LEVELS
        // =============================================================

        /// <summary>
        /// Loudness target, as RMS.
        ///
        /// Peak normalization alone is not enough here. A ringing bell
        /// and a solid square wave can share a peak while the bell is
        /// a fifth as loud to the ear, which would mean picking
        /// "Morning Bells" quietly sabotaged your alarm.
        /// </summary>
        private const double TargetRms = 0.32;

        private const double Ceiling = 0.95;


        private static void Normalize(double[] samples, double masterGain)
        {
            if (samples.Length == 0)
                return;

            double peak = 0;
            double sumSquares = 0;

            foreach (double sample in samples)
            {
                peak = Math.Max(peak, Math.Abs(sample));
                sumSquares += sample * sample;
            }

            if (peak <= 0)
                return;

            double rms = Math.Sqrt(sumSquares / samples.Length);

            /*
             * Aim for a consistent average level, but only within a
             * limited range: a sparse sound pushed too hard stops
             * sounding like the thing it is meant to be.
             */
            double gain =
                rms > 1e-9
                    ? Math.Clamp(TargetRms / rms, 0.4, 4.0)
                    : Ceiling / peak;

            gain *= Math.Clamp(masterGain, 0.0, 1.0);

            for (int i = 0; i < samples.Length; i++)
            {
                // Soft saturation rather than hard clipping, so the
                // transient of a bell strike rounds off instead of
                // turning into a square edge.
                samples[i] = Ceiling * Math.Tanh(samples[i] * gain / Ceiling);
            }
        }


        // =============================================================
        // WAV OUTPUT
        // =============================================================

        private static void WriteWav(
            string path,
            double[] samples,
            double amplitude)
        {
            const short blockAlign = Channels * (BitsPerSample / 8);
            const int byteRate = SampleRate * blockAlign;

            int dataSize = samples.Length * blockAlign;

            string temporary = path + ".tmp";

            using (FileStream stream = File.Create(temporary))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write(Channels);
                writer.Write(SampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write(BitsPerSample);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);

                foreach (double sample in samples)
                {
                    double scaled =
                        Math.Clamp(sample * amplitude, -1.0, 1.0);

                    writer.Write((short)(scaled * short.MaxValue));
                }
            }

            // Written aside and moved so a half-rendered file can never
            // be picked up from the cache.
            File.Move(temporary, path, overwrite: true);
        }
    }
}
