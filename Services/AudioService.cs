using System;
using System.IO;
using System.Media;
using LibVLCSharp.Shared;
using SecSeeTime.Enums;
using SecSeeTime.Models;

namespace SecSeeTime.Services
{
    /// <summary>
    /// The Audio Engine.
    ///
    /// Its entire contract is "play this" and "stop this". It knows
    /// nothing about phases, cycles or snooze -- the alarm engine owns
    /// the schedule, and the sound library owns what the audio is.
    ///
    /// Built-in sounds arrive as synthesized WAVs and go through
    /// Windows SoundPlayer, which starts instantly and never touches
    /// the WPF dispatcher. Imported files go through VLC for broad
    /// format support.
    /// </summary>
    public sealed class AudioService : IDisposable
    {
        private readonly SoundLibraryService _library;
        private readonly object _sync = new();

        private LibVLC? _libVlc;
        private MediaPlayer? _mediaPlayer;
        private Media? _currentMedia;
        private SoundPlayer? _soundPlayer;
        private bool _disposed;


        public AudioService(SoundLibraryService library)
        {
            _library = library;
        }


        // =============================================================
        // PLAY
        // =============================================================

        public void Play(Alarm alarm)
        {
            if (_disposed)
                return;

            // YouTube alarms play through the alarm screen's embedded
            // player, not through here.
            if (alarm.SoundType == AlarmSoundType.YouTube)
                return;

            SoundDefinition sound = _library.Resolve(alarm);

            Play(sound, alarm.Volume);
        }


        public void Play(SoundDefinition sound, double volume)
        {
            if (_disposed)
                return;

            string? file = _library.GetPlayableFile(sound, volume);

            if (file == null)
                return;

            if (sound.Kind == SoundKind.BuiltIn)
            {
                PlayLoopingWav(file);
                return;
            }

            PlayFile(file, volume);
        }


        /// <summary>
        /// Built-in sounds are short seamless loops with the volume
        /// already baked into the waveform.
        /// </summary>
        private void PlayLoopingWav(string path)
        {
            lock (_sync)
            {
                StopBuiltIn_NoLock();

                _soundPlayer = new SoundPlayer(path);
                _soundPlayer.Load();
                _soundPlayer.PlayLooping();
            }
        }


        public void PlayFile(string path, double volume)
        {
            if (_disposed || !File.Exists(path))
                return;

            lock (_sync)
            {
                Stop_NoLock();
                EnsureVlc_NoLock();

                _mediaPlayer!.Volume =
                    Math.Clamp((int)Math.Round(volume * 100), 0, 100);

                _currentMedia = new Media(_libVlc!, new Uri(path));

                /*
                 * A 20-second ringtone must not leave 100 seconds of
                 * silence inside a two-minute sound phase, so the file
                 * repeats until the alarm engine stops it.
                 */
                _currentMedia.AddOption(":input-repeat=65535");

                _mediaPlayer.Play(_currentMedia);
            }
        }


        // =============================================================
        // STOP
        // =============================================================

        public void Stop()
        {
            if (_disposed)
                return;

            lock (_sync)
            {
                Stop_NoLock();
            }
        }


        private void Stop_NoLock()
        {
            StopBuiltIn_NoLock();

            try
            {
                _mediaPlayer?.Stop();
            }
            catch
            {
                // Audio shutdown must never be allowed to freeze the application.
            }

            _currentMedia?.Dispose();
            _currentMedia = null;
        }


        private void StopBuiltIn_NoLock()
        {
            try
            {
                _soundPlayer?.Stop();
            }
            catch
            {
                // Ignore audio shutdown errors.
            }

            _soundPlayer?.Dispose();
            _soundPlayer = null;
        }


        private void EnsureVlc_NoLock()
        {
            if (_mediaPlayer != null)
                return;

            Core.Initialize();
            _libVlc = new LibVLC("--no-video", "--quiet");
            _mediaPlayer = new MediaPlayer(_libVlc);
        }


        // =============================================================
        // DISPOSE
        // =============================================================

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_sync)
            {
                _disposed = true;
                Stop_NoLock();

                _mediaPlayer?.Dispose();
                _mediaPlayer = null;

                _libVlc?.Dispose();
                _libVlc = null;
            }
        }
    }
}
