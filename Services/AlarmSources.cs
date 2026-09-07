using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using SecSeeTime.Controls;
using SecSeeTime.Models;

namespace SecSeeTime.Services
{
    /// <summary>
    /// One alarm's audio, whatever it happens to be.
    ///
    /// This is what makes a YouTube link and a FLAC file
    /// interchangeable: the alarm engine drives the schedule and only
    /// ever says start or stop.
    /// </summary>
    internal interface IAlarmSource
    {
        Task StartAsync(CancellationToken token);

        Task StopAsync();
    }


    /// <summary>
    /// Built-in tones and user audio files, via the Audio Engine.
    /// </summary>
    internal sealed class LocalAlarmSource : IAlarmSource
    {
        private readonly AudioService _audio;
        private readonly Alarm _alarm;

        public LocalAlarmSource(AudioService audio, Alarm alarm)
        {
            _audio = audio;
            _alarm = alarm;
        }


        public Task StartAsync(CancellationToken token)
        {
            // Playback initialization can block. It never runs on the
            // dispatcher; that is what used to freeze the whole app the
            // moment an alarm fired.
            return Task.Run(() => _audio.Play(_alarm), token);
        }


        public Task StopAsync()
        {
            return Task.Run(_audio.Stop);
        }
    }


    /// <summary>
    /// A YouTube video, hosted inside the alarm screen.
    /// </summary>
    internal sealed class YouTubeAlarmSource : IAlarmSource
    {
        private readonly YouTubePlayerControl _player;
        private readonly Alarm _alarm;

        private bool _loadRequested;

        public YouTubeAlarmSource(
            YouTubePlayerControl player,
            Alarm alarm)
        {
            _player = player;
            _alarm = alarm;
        }


        public async Task StartAsync(CancellationToken token)
        {
            if (!_loadRequested)
            {
                _loadRequested = true;

                string url = _alarm.YouTubeUrl ?? "";

                await OnUiAsync(
                    _player.Dispatcher,
                    () => _player.LoadAsync(url, _alarm.Volume))
                    .ConfigureAwait(false);

                // The embed autoplays from its own onReady callback, so
                // the first sound phase needs no further nudging.
                return;
            }

            await OnUiAsync(_player.Dispatcher, _player.PlayAsync)
                .ConfigureAwait(false);
        }


        public Task StopAsync()
        {
            return OnUiAsync(_player.Dispatcher, _player.PauseAsync);
        }


        /// <summary>
        /// Runs an asynchronous UI operation from the engine's
        /// background thread and waits for it to actually finish,
        /// rather than just for it to be queued.
        /// </summary>
        private static async Task OnUiAsync(
            Dispatcher dispatcher,
            Func<Task> action)
        {
            try
            {
                Task inner =
                    await dispatcher.InvokeAsync(action)
                        .Task
                        .ConfigureAwait(false);

                await inner.ConfigureAwait(false);
            }
            catch
            {
                // A player that will not respond must not stall or
                // crash the alarm session.
            }
        }
    }
}
