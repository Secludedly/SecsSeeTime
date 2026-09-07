using System;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using SecSeeTime.Enums;
using SecSeeTime.Models;

namespace SecSeeTime.Services
{
    /// <summary>
    /// Auditions a sound in the editor and the library browser.
    ///
    /// Deliberately separate from <see cref="AudioService"/> and owning
    /// its own players: previewing a sound while an alarm is going off
    /// must not stop the alarm, and stopping a preview must not stop
    /// the alarm either.
    /// </summary>
    public sealed class AudioPreviewService : IDisposable
    {
        /// <summary>
        /// Previews cut themselves off. Nobody wants to discover that
        /// Air Horn has been looping since they opened the window.
        /// </summary>
        private static readonly TimeSpan MaxPreview =
            TimeSpan.FromSeconds(8);

        private readonly SoundLibraryService _library;
        private readonly object _sync = new();

        private SoundPlayer? _soundPlayer;
        private LibVLC? _libVlc;
        private MediaPlayer? _mediaPlayer;
        private Media? _currentMedia;

        private CancellationTokenSource? _autoStopCts;

        private int _generation;
        private bool _disposed;


        public AudioPreviewService(SoundLibraryService library)
        {
            _library = library;
        }


        // =============================================================
        // STATE
        // =============================================================

        /// <summary>Raised on the dispatcher whenever playback starts or stops.</summary>
        public event EventHandler? PreviewChanged;


        public SoundDefinition? Current { get; private set; }


        public bool IsPlaying => Current != null;


        public bool IsPlayingId(string? id)
        {
            return Current != null &&
                   string.Equals(Current.Id, id, StringComparison.Ordinal);
        }


        // =============================================================
        // CONTROL
        // =============================================================

        /// <summary>
        /// Starts the sound, or stops it if it is already the one
        /// playing. Wired straight to a single play/stop button.
        /// </summary>
        public void Toggle(SoundDefinition sound, double volume)
        {
            if (IsPlayingId(sound.Id))
            {
                Stop();
                return;
            }

            Play(sound, volume);
        }


        public void Play(SoundDefinition sound, double volume)
        {
            if (_disposed)
                return;

            StopPlayers();

            int generation;

            lock (_sync)
            {
                _generation++;
                generation = _generation;
            }

            Current = sound;

            RaiseChanged();

            /*
             * A built-in sound may need rendering before it can play,
             * and an eight-second ambient bed takes long enough that
             * doing it inline would visibly stutter the editor.
             */
            _ = Task.Run(() => StartAsync(sound, volume, generation));
        }


        private async Task StartAsync(
            SoundDefinition sound,
            double volume,
            int generation)
        {
            string? file = null;

            try
            {
                file = _library.GetPlayableFile(sound, volume);
            }
            catch
            {
                // Fall through to the failure path below.
            }

            if (IsStale(generation))
                return;

            if (file == null || !File.Exists(file))
            {
                Current = null;

                RaiseChanged();

                return;
            }

            try
            {
                if (sound.Kind == SoundKind.BuiltIn)
                {
                    lock (_sync)
                    {
                        if (_generation != generation)
                            return;

                        _soundPlayer = new SoundPlayer(file);
                        _soundPlayer.Load();
                        _soundPlayer.PlayLooping();
                    }
                }
                else
                {
                    lock (_sync)
                    {
                        if (_generation != generation)
                            return;

                        EnsureVlc_NoLock();

                        _mediaPlayer!.Volume =
                            Math.Clamp((int)Math.Round(volume * 100), 0, 100);

                        _currentMedia =
                            new Media(_libVlc!, new Uri(file));

                        _mediaPlayer.Play(_currentMedia);
                    }
                }
            }
            catch
            {
                Current = null;

                RaiseChanged();

                return;
            }

            await AutoStopAsync(generation).ConfigureAwait(false);
        }


        private async Task AutoStopAsync(int generation)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            lock (_sync)
            {
                if (_generation != generation)
                    return;

                _autoStopCts = cts;
            }

            try
            {
                await Task.Delay(MaxPreview, cts.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (IsStale(generation))
                return;

            Stop();
        }


        public void Stop()
        {
            StopPlayers();

            if (Current == null)
                return;

            Current = null;

            RaiseChanged();
        }


        private void StopPlayers()
        {
            CancellationTokenSource? cts;

            lock (_sync)
            {
                _generation++;

                cts = _autoStopCts;
                _autoStopCts = null;
            }

            try
            {
                // Canceled but not disposed: the delay may still be
                // waiting on this token.
                cts?.Cancel();
            }
            catch
            {
                // Ignore.
            }

            lock (_sync)
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

                try
                {
                    _mediaPlayer?.Stop();
                }
                catch
                {
                    // Ignore audio shutdown errors.
                }

                _currentMedia?.Dispose();
                _currentMedia = null;
            }
        }


        private bool IsStale(int generation)
        {
            lock (_sync)
            {
                return _generation != generation;
            }
        }


        private void EnsureVlc_NoLock()
        {
            if (_mediaPlayer != null)
                return;

            Core.Initialize();
            _libVlc = new LibVLC("--no-video", "--quiet");
            _mediaPlayer = new MediaPlayer(_libVlc);
        }


        private void RaiseChanged()
        {
            Application? app = Application.Current;

            if (app == null)
            {
                PreviewChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            app.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(() =>
                    PreviewChanged?.Invoke(this, EventArgs.Empty)));
        }


        // =============================================================
        // DISPOSE
        // =============================================================

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            StopPlayers();

            Current = null;

            lock (_sync)
            {
                _mediaPlayer?.Dispose();
                _mediaPlayer = null;

                _libVlc?.Dispose();
                _libVlc = null;
            }
        }
    }
}
