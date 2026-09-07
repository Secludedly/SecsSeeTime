using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using SecSeeTime.Services;

namespace SecSeeTime.Controls
{
    /// <summary>
    /// The YouTube playback surface.
    ///
    /// This is the implementation that finally defeated Error 153, kept
    /// intact and moved out of its own window so the alarm screen can
    /// host it. That is what lets a YouTube alarm run through the same
    /// PLAYING / SILENCE state machine as a WAV file.
    /// </summary>
    public partial class YouTubePlayerControl : UserControl
    {
        /*
         * YouTube refuses to configure its player (Error 153) when the
         * embed is requested without a Referer header. Navigating the
         * WebView2 straight at youtube.com/embed/... sends no referrer,
         * so instead we serve a tiny local page over a virtual host and
         * let that page own the iframe. The embed then arrives with a
         * real origin and referrer, and the IFrame API becomes usable.
         */

        private const string VirtualHostName = "secseetime.invalid";

        private readonly TaskCompletionSource<bool> _ready =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private bool _initialized;
        private bool _loadStarted;
        private bool _failed;


        public YouTubePlayerControl()
        {
            InitializeComponent();
        }


        // =============================================================
        // EVENTS
        // =============================================================

        /// <summary>Raised when the video can't be played.</summary>
        public event EventHandler<string>? Failed;


        /// <summary>
        /// Completes once the embedded player is ready.
        /// On failure it completes with false and continues.
        /// </summary>
        public Task<bool> ReadyAsync => _ready.Task;


        // =============================================================
        // LOAD
        // =============================================================

        /// <summary>
        /// Initializes WebView2.
        /// </summary>
        public async Task LoadAsync(string url, double volume)
        {
            if (_loadStarted)
                return;

            _loadStarted = true;

            try
            {
                string appDataFolder =
                    Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.LocalApplicationData),
                        "SecsSeeTime");

                string userDataFolder =
                    Path.Combine(appDataFolder, "WebView2");

                var options =
                    new CoreWebView2EnvironmentOptions
                    {
                        AdditionalBrowserArguments =
                            "--autoplay-policy=no-user-gesture-required"
                    };

                CoreWebView2Environment environment =
                    await CoreWebView2Environment.CreateAsync(
                        null,
                        userDataFolder,
                        options);

                await Browser.EnsureCoreWebView2Async(environment);

                ConfigureBrowser();

                string? videoId =
                    YouTubeService.ExtractVideoId(url);

                if (string.IsNullOrWhiteSpace(videoId))
                {
                    Fail(
                        "The YouTube URL could not be recognized. " +
                        "Please check the link on this alarm.");

                    return;
                }

                string playerFolder =
                    Path.Combine(appDataFolder, "Player");

                Directory.CreateDirectory(playerFolder);

                // Rewrites every launch so updates to the page ship
                // without the user clearing their WebView2 profile.
                File.WriteAllText(
                    Path.Combine(playerFolder, "player.html"),
                    PlayerPageHtml);

                Browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    VirtualHostName,
                    playerFolder,
                    CoreWebView2HostResourceAccessKind.DenyCors);

                _initialized = true;

                /*
                 * The video id and volume travel in the fragment rather
                 * than the query string: the fragment never reaches the
                 * virtual host's file resolver, so player.html stays a
                 * plain static file.
                 */

                int startVolume =
                    Math.Clamp((int)Math.Round(volume * 100), 0, 100);

                Browser.CoreWebView2.Navigate(
                    $"https://{VirtualHostName}/player.html" +
                    $"#v={Uri.EscapeDataString(videoId)}" +
                    $"&vol={startVolume}");
            }
            catch (Exception ex)
            {
                Fail("YouTube could not be started.\n\n" + ex.Message);
            }
        }


        // =============================================================
        // BROWSER CONFIGURATION
        // =============================================================

        private void ConfigureBrowser()
        {
            if (Browser.CoreWebView2 == null)
                return;

            Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            Browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            Browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            Browser.CoreWebView2.Settings.IsZoomControlEnabled = false;
            Browser.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            Browser.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;

            Browser.CoreWebView2.NavigationCompleted +=
                Browser_NavigationCompleted;

            Browser.CoreWebView2.WebMessageReceived +=
                Browser_WebMessageReceived;
        }


        // =============================================================
        // PLAYER HOST PAGE
        // =============================================================

        /*
         * Deliberately a plain (non-interpolated) raw string: the page
         * is full of braces, and token replacement is not needed here
         * because the parameters arrive in the URL fragment.
         */

        private const string PlayerPageHtml =
            """
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <style>
              html, body { margin: 0; padding: 0; height: 100%; background: #000; overflow: hidden; }
              #player { width: 100%; height: 100%; border: 0; }
            </style>
            </head>
            <body>
            <div id="player"></div>
            <script>
              function post(message) {
                  try { window.chrome.webview.postMessage(message); } catch (e) { }
              }

              var params = new URLSearchParams(location.hash.slice(1));
              var videoId = params.get('v') || '';
              var startVolume = parseInt(params.get('vol') || '100', 10);

              window.onYouTubeIframeAPIReady = function () {
                  window.player = new YT.Player('player', {
                      videoId: videoId,
                      host: 'https://www.youtube.com',
                      playerVars: {
                          autoplay: 1,
                          controls: 1,
                          rel: 0,
                          playsinline: 1,
                          modestbranding: 1,
                          origin: location.origin
                      },
                      events: {
                          onReady: function (e) {
                              try {
                                  e.target.setVolume(startVolume);
                                  e.target.unMute();
                                  e.target.playVideo();
                              } catch (err) { }
                              post('ready');
                          },
                          onStateChange: function (e) {
                              // 0 = ended. Loop so a short video fills
                              // the whole sound phase.
                              if (e.data === 0) {
                                  try { e.target.seekTo(0); e.target.playVideo(); } catch (err) { }
                              }
                          },
                          onError: function (e) {
                              post('error:' + e.data);
                          }
                      }
                  });
              };

              var tag = document.createElement('script');
              tag.src = 'https://www.youtube.com/iframe_api';
              tag.onerror = function () { post('apiError'); };
              document.head.appendChild(tag);
            </script>
            </body>
            </html>
            """;


        // =============================================================
        // PLAYER MESSAGES
        // =============================================================

        private void Browser_WebMessageReceived(
            object? sender,
            CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message;

            try
            {
                message = e.TryGetWebMessageAsString();
            }
            catch
            {
                return;
            }

            if (message == "ready")
            {
                StatusOverlay.Visibility = Visibility.Collapsed;

                _ready.TrySetResult(true);

                return;
            }

            if (message == "apiError")
            {
                Fail(
                    "The YouTube player could not be loaded. " +
                    "Please check your internet connection.");

                return;
            }

            if (message.StartsWith("error:", StringComparison.Ordinal))
            {
                Fail(
                    "YouTube could not play this video. " +
                    DescribePlayerError(message["error:".Length..]));
            }
        }


        private static string DescribePlayerError(string code)
        {
            return code switch
            {
                "2" =>
                    "The video id in the alarm's URL is not valid.",

                "5" =>
                    "The video cannot be played in an embedded player.",

                "100" =>
                    "The video was removed, or it is private.",

                "101" or "150" =>
                    "The video's owner does not allow it to be played in " +
                    "embedded players. Please choose a different video.",

                _ =>
                    $"Player error code {code}."
            };
        }


        private void Browser_NavigationCompleted(
            object? sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!_initialized || Browser.CoreWebView2 == null)
                return;

            /*
             * Only the local host page reaches this handler; the embed
             * lives in an iframe and reports its own failures through
             * Browser_WebMessageReceived. Playback starts from the
             * player's onReady callback, so there is nothing to inject.
             */

            if (!e.IsSuccess)
            {
                Fail(
                    "The YouTube player page failed to load. " +
                    $"Navigation error: {e.WebErrorStatus}");
            }
        }


        // =============================================================
        // TRANSPORT
        // =============================================================

        public Task PlayAsync() =>
            RunPlayerScriptAsync("playVideo");


        public Task PauseAsync() =>
            RunPlayerScriptAsync("pauseVideo");


        public async Task SetVolumeAsync(double volume)
        {
            int percentage =
                Math.Clamp((int)Math.Round(volume * 100), 0, 100);

            await RunScriptAsync(
                $$"""
                (() => {
                    try {
                        if (window.player &&
                            typeof window.player.setVolume === 'function') {
                            window.player.setVolume({{percentage}});
                        }
                    }
                    catch {}
                })();
                """);
        }


        private Task RunPlayerScriptAsync(string method)
        {
            return RunScriptAsync(
                $$"""
                (() => {
                    try {
                        if (window.player &&
                            typeof window.player.{{method}} === 'function') {
                            window.player.{{method}}();
                        }
                    }
                    catch {}
                })();
                """);
        }


        private async Task RunScriptAsync(string script)
        {
            if (Browser.CoreWebView2 == null)
                return;

            try
            {
                await Browser.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch
            {
                // The player API may not be initialized yet, or the
                // control may be tearing down. Neither is fatal.
            }
        }


        // =============================================================
        // SHUTDOWN
        // =============================================================

        public void Shutdown()
        {
            try
            {
                _ready.TrySetResult(false);

                if (Browser.CoreWebView2 != null)
                {
                    Browser.CoreWebView2.NavigationCompleted -=
                        Browser_NavigationCompleted;

                    Browser.CoreWebView2.WebMessageReceived -=
                        Browser_WebMessageReceived;
                }

                Browser.Dispose();
            }
            catch
            {
                // Ignore teardown errors.
            }
        }


        // =============================================================
        // FAILURE
        // =============================================================

        private void Fail(string message)
        {
            if (_failed)
                return;

            _failed = true;

            StatusOverlay.Visibility = Visibility.Visible;
            StatusIcon.Text = "⚠";
            StatusText.Text = message.ToUpperInvariant();

            _ready.TrySetResult(false);

            Failed?.Invoke(this, message);
        }
    }
}
