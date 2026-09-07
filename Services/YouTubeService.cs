using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SecSeeTime.Services
{
    /// <summary>
    /// Everything the app knows about YouTube URLs.
    ///
    /// This used to live inside the playback window, which meant the
    /// alarm editor could accept a link the player would later reject.
    /// Both now validate through the same code.
    /// </summary>
    public static partial class YouTubeService
    {
        // =============================================================
        // HOST CHECK
        // =============================================================

        public static bool LooksLikeYouTubeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!Uri.TryCreate(
                    url.Trim(),
                    UriKind.Absolute,
                    out Uri? uri))
            {
                return false;
            }

            string host = uri.Host.ToLowerInvariant();

            return host.Contains("youtube.com") ||
                   host == "youtu.be" ||
                   host.EndsWith(".youtu.be");
        }


        // =============================================================
        // VIDEO ID EXTRACTION
        // =============================================================

        public static bool TryGetVideoId(
            string? url,
            out string videoId)
        {
            videoId = ExtractVideoId(url) ?? "";

            return videoId.Length > 0;
        }


        public static string? ExtractVideoId(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            url = url.Trim();

            if (Uri.TryCreate(
                    url,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                string host = uri.Host.ToLowerInvariant();


                // -----------------------------------------------------
                // youtu.be/VIDEO_ID
                // -----------------------------------------------------

                if (host == "youtu.be" ||
                    host.EndsWith(".youtu.be"))
                {
                    string id =
                        uri.AbsolutePath
                            .Trim('/')
                            .Split(
                                '/',
                                StringSplitOptions.RemoveEmptyEntries)
                            .FirstOrDefault()
                        ?? "";

                    return CleanVideoId(id);
                }


                // -----------------------------------------------------
                // youtube.com
                // -----------------------------------------------------

                if (host.Contains("youtube.com"))
                {
                    // Standard watch URL.
                    string query = uri.Query.TrimStart('?');

                    foreach (string part in
                             query.Split(
                                 '&',
                                 StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] pair = part.Split('=', 2);

                        if (pair.Length == 2 &&
                            pair[0].Equals(
                                "v",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            return CleanVideoId(
                                Uri.UnescapeDataString(pair[1]));
                        }
                    }


                    // /shorts/ID, /embed/ID, /live/ID
                    string[] segments =
                        uri.AbsolutePath
                            .Trim('/')
                            .Split(
                                '/',
                                StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < segments.Length - 1; i++)
                    {
                        if (segments[i].Equals("shorts", StringComparison.OrdinalIgnoreCase) ||
                            segments[i].Equals("embed", StringComparison.OrdinalIgnoreCase) ||
                            segments[i].Equals("live", StringComparison.OrdinalIgnoreCase))
                        {
                            return CleanVideoId(segments[i + 1]);
                        }
                    }
                }
            }


            /*
             * Final fallback for links pasted with surrounding text
             * or unusual formatting.
             */

            Match match =
                LooseIdPattern().Match(url);

            return match.Success
                ? CleanVideoId(match.Groups[1].Value)
                : null;
        }


        // =============================================================
        // CLEAN
        // =============================================================

        private static string? CleanVideoId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string cleaned = value.Trim();

            // Strip anything accidentally attached to the id.
            foreach (char terminator in new[] { '?', '&', '/', '#' })
            {
                int index = cleaned.IndexOf(terminator);

                if (index >= 0)
                    cleaned = cleaned[..index];
            }

            return StrictIdPattern().IsMatch(cleaned)
                ? cleaned
                : null;
        }


        [GeneratedRegex(
            @"(?:v=|youtu\.be/|shorts/|embed/|live/)([A-Za-z0-9_-]{6,})",
            RegexOptions.IgnoreCase)]
        private static partial Regex LooseIdPattern();


        [GeneratedRegex(@"^[A-Za-z0-9_-]{6,}$")]
        private static partial Regex StrictIdPattern();
    }
}
