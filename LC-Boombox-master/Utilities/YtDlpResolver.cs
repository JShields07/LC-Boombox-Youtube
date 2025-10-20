// Utilities/YtDlpResolver.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using CustomBoomboxTracks.Configuration;

namespace CustomBoomboxTracks.Utilities
{
    internal static class YtDlpResolver
    {
        // Resolve direct best-audio URL for a single YouTube video
        public static string ResolveBestAudioUrl(string videoUrl)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Config.YtDlpPath,
                Arguments = $"-f ba -g \"{videoUrl}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            string url = p.StandardOutput.ReadLine();
            p.WaitForExit();
            return url?.Trim();
        }

        // Enumerate playlist entries: (title, direct stream url)
        public static IEnumerable<(string title, string url)> ResolvePlaylist(string playlistUrl)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Config.YtDlpPath,
                Arguments = $"--flat-playlist -i -J \"{playlistUrl}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            string json = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("entries", out var entries)) yield break;

            foreach (var e in entries.EnumerateArray())
            {
                if (!e.TryGetProperty("url", out var idProp)) continue;
                string id = idProp.GetString();
                if (string.IsNullOrWhiteSpace(id)) continue;

                string title = e.TryGetProperty("title", out var t) ? t.GetString() : "YouTube";
                string full = $"https://www.youtube.com/watch?v={id}";
                string stream = ResolveBestAudioUrl(full);
                if (!string.IsNullOrWhiteSpace(stream))
                    yield return (title, stream);
            }
        }
    }
}
