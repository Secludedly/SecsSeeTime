using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SecSeeTime.Services
{
    /// <summary>
    /// One selectable audio source: either a capture endpoint such as a
    /// microphone or webcam, or a render endpoint captured in loopback so we
    /// record whatever Windows is currently playing.
    /// </summary>
    public sealed record RecordingDeviceInfo(
        string Id,
        string Name,
        bool IsLoopback)
    {
        public override string ToString() => Name;
    }


    /// <summary>
    /// Recording to a WAV file, built on WASAPI.
    ///
    /// This replaced an implementation built on the MCI <c>waveaudio</c>
    /// device. MCI can only ever record from whatever Windows has set as the
    /// default input -- there is no MCI command to choose an endpoint -- it
    /// cannot capture playback at all, and it fails silently often enough on
    /// Windows 11 to be unusable. WASAPI addresses all three: any endpoint can
    /// be opened by ID, render endpoints can be opened in loopback mode, and
    /// errors surface as exceptions rather than a mute file.
    ///
    /// Samples are converted to 16-bit PCM on the way to disk. WASAPI hands us
    /// the endpoint's mix format, which is normally 32-bit float, and a plain
    /// 16-bit WAV is what the rest of the alarm playback path already expects.
    /// </summary>
    public sealed class AudioRecordingService : IDisposable
    {
        private readonly object _sync = new();

        private WasapiCapture? _capture;
        private WaveFileWriter? _writer;
        private WaveFormat? _sourceFormat;
        private ManualResetEventSlim? _stopped;
        private string? _targetPath;
        private string? _captureError;

        private DateTime _startedUtc;
        private volatile float _level;
        private bool _disposed;


        /// <summary>Where finished recordings are kept.</summary>
        public static string RecordingsFolder { get; } =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "SecsSeeTime",
                "Recordings");


        /// <summary>
        /// Endpoint chosen last time, so reopening the recorder does not send
        /// the user hunting for their webcam microphone again.
        /// </summary>
        public static string? LastDeviceId { get; set; }


        /// <summary>
        /// True when the given path is one of our own recordings, which
        /// is how the alarm editor knows to reopen in recording mode.
        /// </summary>
        public static bool IsInRecordingsFolder(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                string folder =
                    Path.GetDirectoryName(Path.GetFullPath(path)) ?? "";

                return string.Equals(
                    folder.TrimEnd(Path.DirectorySeparatorChar),
                    RecordingsFolder.TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }


        // =============================================================
        // DEVICES
        // =============================================================

        /// <summary>
        /// Every source the user can record from: active capture endpoints
        /// first, then each render endpoint offered as a loopback source.
        /// Never throws -- an empty list simply means nothing is available.
        /// </summary>
        public static List<RecordingDeviceInfo> GetDevices()
        {
            var devices = new List<RecordingDeviceInfo>();

            try
            {
                using var enumerator = new MMDeviceEnumerator();

                string defaultCaptureId = TryGetDefaultId(
                    enumerator,
                    DataFlow.Capture);

                foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(
                             DataFlow.Capture,
                             DeviceState.Active))
                {
                    using (device)
                    {
                        bool isDefault = string.Equals(
                            device.ID,
                            defaultCaptureId,
                            StringComparison.OrdinalIgnoreCase);

                        devices.Add(
                            new RecordingDeviceInfo(
                                device.ID,
                                isDefault
                                    ? device.FriendlyName + "  (default)"
                                    : device.FriendlyName,
                                IsLoopback: false));
                    }
                }

                foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(
                             DataFlow.Render,
                             DeviceState.Active))
                {
                    using (device)
                    {
                        devices.Add(
                            new RecordingDeviceInfo(
                                device.ID,
                                "System playback • " + device.FriendlyName,
                                IsLoopback: true));
                    }
                }
            }
            catch
            {
                // Fall through: the caller reports "no devices found".
            }

            return devices;
        }


        private static string TryGetDefaultId(
            MMDeviceEnumerator enumerator,
            DataFlow flow)
        {
            try
            {
                using MMDevice device =
                    enumerator.GetDefaultAudioEndpoint(flow, Role.Console);

                return device.ID;
            }
            catch
            {
                // No default endpoint of this kind.
                return "";
            }
        }


        // =============================================================
        // STATE
        // =============================================================

        public bool IsActive
        {
            get { lock (_sync) return _capture != null; }
        }

        public TimeSpan Elapsed =>
            IsActive ? DateTime.UtcNow - _startedUtc : TimeSpan.Zero;

        /// <summary>
        /// Peak level of the most recent buffer, 0 to 1. Drives the meter, so
        /// a silent input is visible before the user saves a mute take.
        /// </summary>
        public float Level => _level;


        // =============================================================
        // START
        // =============================================================

        /// <summary>
        /// Opens <paramref name="device"/> and starts writing 16-bit PCM WAV
        /// to <paramref name="path"/>. Returns false with a readable reason.
        /// </summary>
        public bool Start(
            RecordingDeviceInfo device,
            string path,
            out string error)
        {
            error = "";

            if (device == null)
            {
                error = "No recording device was selected.";
                return false;
            }

            lock (_sync)
            {
                if (_disposed)
                {
                    error = "The recorder has been closed.";
                    return false;
                }

                if (_capture != null)
                    return true;

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                    using var enumerator = new MMDeviceEnumerator();

                    MMDevice endpoint = enumerator.GetDevice(device.Id);

                    WasapiCapture capture = device.IsLoopback
                        ? new WasapiLoopbackCapture(endpoint)
                        : new WasapiCapture(endpoint);

                    // WASAPI reports its mix format as WaveFormatExtensible,
                    // whose Encoding is 'Extensible' rather than the actual
                    // sample type. Normalizing it lets the converter below
                    // switch on a real encoding.
                    WaveFormat source = capture.WaveFormat;

                    if (source is WaveFormatExtensible extensible)
                        source = extensible.ToStandardWaveFormat();

                    _sourceFormat = source;

                    _writer = new WaveFileWriter(
                        path,
                        new WaveFormat(
                            source.SampleRate,
                            16,
                            source.Channels));

                    _stopped = new ManualResetEventSlim(false);
                    _targetPath = path;
                    _captureError = null;
                    _level = 0f;

                    capture.DataAvailable += OnDataAvailable;
                    capture.RecordingStopped += OnRecordingStopped;

                    _capture = capture;

                    capture.StartRecording();

                    _startedUtc = DateTime.UtcNow;

                    return true;
                }
                catch (Exception ex)
                {
                    error = Describe(ex);

                    TeardownLocked(deleteFile: true);

                    return false;
                }
            }
        }


        // =============================================================
        // CAPTURE CALLBACKS  (background thread)
        // =============================================================

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            lock (_sync)
            {
                if (_writer == null || _sourceFormat == null)
                    return;

                try
                {
                    _level = WriteConverted(
                        _writer,
                        _sourceFormat,
                        e.Buffer,
                        e.BytesRecorded);
                }
                catch (Exception ex)
                {
                    // Remember it and stop; Stop() reports it to the user.
                    _captureError ??= Describe(ex);
                }
            }
        }


        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
                _captureError ??= Describe(e.Exception);

            _stopped?.Set();
        }


        /// <summary>
        /// Converts one buffer to 16-bit PCM, writes it, and returns the peak
        /// sample magnitude in that buffer.
        /// </summary>
        private static float WriteConverted(
            WaveFileWriter writer,
            WaveFormat format,
            byte[] buffer,
            int count)
        {
            float peak = 0f;

            void Emit(float sample)
            {
                float magnitude = Math.Abs(sample);

                if (magnitude > peak)
                    peak = magnitude;

                writer.WriteSample(sample);
            }

            int bytesPerSample = format.BitsPerSample / 8;

            if (bytesPerSample <= 0)
                return 0f;

            int usable = count - (count % bytesPerSample);

            if (format.Encoding == WaveFormatEncoding.IeeeFloat &&
                format.BitsPerSample == 32)
            {
                for (int i = 0; i + 4 <= usable; i += 4)
                    Emit(BitConverter.ToSingle(buffer, i));

                return peak;
            }

            if (format.Encoding == WaveFormatEncoding.Pcm)
            {
                switch (format.BitsPerSample)
                {
                    case 16:
                        for (int i = 0; i + 2 <= usable; i += 2)
                            Emit(BitConverter.ToInt16(buffer, i) / 32768f);

                        return peak;

                    case 24:
                        for (int i = 0; i + 3 <= usable; i += 3)
                        {
                            int value =
                                (buffer[i + 2] << 24) |
                                (buffer[i + 1] << 16) |
                                (buffer[i] << 8);

                            Emit(value / 2147483648f);
                        }

                        return peak;

                    case 32:
                        for (int i = 0; i + 4 <= usable; i += 4)
                            Emit(BitConverter.ToInt32(buffer, i) / 2147483648f);

                        return peak;
                }
            }

            throw new NotSupportedException(
                $"This device records {format.BitsPerSample}-bit " +
                $"{format.Encoding}, which is not supported.");
        }


        // =============================================================
        // STOP
        // =============================================================

        /// <summary>
        /// Stops recording and finalizes the WAV header. Returns false with a
        /// readable reason when the take is unusable.
        /// </summary>
        public bool Stop(out string error)
        {
            error = "";

            WasapiCapture? capture;
            ManualResetEventSlim? stopped;

            lock (_sync)
            {
                if (_capture == null)
                {
                    error = "Nothing is being recorded.";
                    return false;
                }

                capture = _capture;
                stopped = _stopped;
            }

            try
            {
                // StopRecording is asynchronous: the final buffers arrive on
                // the capture thread and RecordingStopped signals completion.
                // Finalizing the header before that would truncate the take.
                capture.StopRecording();

                stopped?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _captureError ??= Describe(ex);
            }

            lock (_sync)
            {
                string? path = _targetPath;
                string? failure = _captureError;

                // Bytes of audio, excluding the header. The header is not a
                // fixed 44 bytes -- WaveFileWriter emits an 18-byte fmt chunk
                // for some formats -- so file length cannot stand in for this.
                long dataBytes = 0;

                try
                {
                    dataBytes = _writer?.Length ?? 0;
                }
                catch
                {
                    // Treated as "nothing recorded" below.
                }

                TeardownLocked(deleteFile: failure != null);

                if (failure != null)
                {
                    error = failure;
                    return false;
                }

                if (path == null || !File.Exists(path))
                {
                    error = "The recording could not be written to disk.";
                    return false;
                }

                // A valid but empty WAV means the endpoint produced nothing.
                if (dataBytes <= 0)
                {
                    error =
                        "The device did not produce any audio. " +
                        "Check that the right source is selected and that it " +
                        "is not muted.";

                    TryDelete(path);

                    return false;
                }

                return true;
            }
        }


        /// <summary>Stops and discards the take.</summary>
        public void Cancel()
        {
            WasapiCapture? capture;
            ManualResetEventSlim? stopped;

            lock (_sync)
            {
                if (_capture == null)
                    return;

                capture = _capture;
                stopped = _stopped;
            }

            try
            {
                capture.StopRecording();

                stopped?.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Nothing useful to do while abandoning a take.
            }

            lock (_sync)
            {
                TeardownLocked(deleteFile: true);
            }
        }


        /// <summary>Releases the capture graph. Caller must hold <see cref="_sync"/>.</summary>
        private void TeardownLocked(bool deleteFile)
        {
            if (_capture != null)
            {
                _capture.DataAvailable -= OnDataAvailable;
                _capture.RecordingStopped -= OnRecordingStopped;

                try
                {
                    _capture.Dispose();
                }
                catch
                {
                    // A device removed mid-take can throw on release.
                }

                _capture = null;
            }

            if (_writer != null)
            {
                try
                {
                    _writer.Dispose();
                }
                catch
                {
                    // The header is already as good as it is going to get.
                }

                _writer = null;
            }

            _stopped?.Dispose();
            _stopped = null;

            _sourceFormat = null;
            _level = 0f;

            if (deleteFile && _targetPath != null)
                TryDelete(_targetPath);

            _targetPath = null;
            _captureError = null;
        }


        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // A leftover temp file is not worth surfacing.
            }
        }


        /// <summary>
        /// Turns the exceptions this path actually produces into something a
        /// user can act on. The COM errors from WASAPI are otherwise opaque.
        /// </summary>
        private static string Describe(Exception ex) =>
            ex switch
            {
                UnauthorizedAccessException =>
                    "Windows blocked access to the microphone. Allow it under " +
                    "Settings › Privacy & security › Microphone.",

                System.Runtime.InteropServices.COMException com =>
                    com.HResult switch
                    {
                        // AUDCLNT_E_DEVICE_IN_USE
                        unchecked((int)0x8889000A) =>
                            "That device is already in use by another " +
                            "application in exclusive mode.",

                        // AUDCLNT_E_DEVICE_INVALIDATED
                        unchecked((int)0x88890004) =>
                            "That device is no longer available. Refresh the " +
                            "list and try again.",

                        // E_ACCESSDENIED
                        unchecked((int)0x80070005) =>
                            "Windows blocked access to the microphone. Allow " +
                            "it under Settings › Privacy & security › Microphone.",

                        _ => com.Message
                    },

                _ => ex.Message
            };


        public void Dispose()
        {
            Cancel();

            lock (_sync)
            {
                _disposed = true;
            }
        }
    }
}
