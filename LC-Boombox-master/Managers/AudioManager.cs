// AudioManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using CustomBoomboxTracks.Configuration;
using CustomBoomboxTracks.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace CustomBoomboxTracks.Managers
{
    internal static class AudioManager
    {
        public static event Action OnAllSongsLoaded;
        public static bool FinishedLoading => finishedLoading;

        static string[] allSongPaths = Array.Empty<string>();
        static readonly List<AudioClip> clips = new List<AudioClip>();
        static readonly List<IDisposable> disposables = new List<IDisposable>();
        static bool firstRun = true;
        static bool finishedLoading = false;

        // NEW: map each streaming clip -> its YouTube URL
        static readonly Dictionary<int, string> clipIdToYoutubeUrl = new Dictionary<int, string>();

        static readonly string directory = Path.Combine(Paths.BepInExRootPath, "Custom Songs", "Boombox Music");

        public static bool HasNoSongs => allSongPaths.Length == 0 && clips.Count == 0;

        public static void GenerateFolders()
        {
            Directory.CreateDirectory(directory);
            BoomboxPlugin.LogInfo($"Created directory at {directory}");
        }

        public static void Load()
        {
            if (!firstRun) return;

            firstRun = false;

            if (Config.UseYouTube)
            {
                SharedCoroutineStarter.StartCoroutine(LoadYouTubeClips());
                return;
            }

            // ----- Local files mode (unchanged) -----
            allSongPaths = Directory.GetFiles(directory);
            if (allSongPaths.Length == 0)
            {
                BoomboxPlugin.LogWarning("No songs found!");
                return;
            }

            BoomboxPlugin.LogInfo("Preparing to load AudioClips...");

            var coroutines = new List<Coroutine>();
            foreach (var track in allSongPaths)
            {
                var coroutine = SharedCoroutineStarter.StartCoroutine(LoadAudioClip(track));
                coroutines.Add(coroutine);
            }

            SharedCoroutineStarter.StartCoroutine(WaitForAllClips(coroutines));
        }

        private static IEnumerator LoadYouTubeClips()
        {
            BoomboxPlugin.LogInfo("Loading YouTube tracks (streaming, no caching)...");
            var entries = new List<(string title, string streamUrl, string displayUrl)>();

            try
            {
                // Playlist
                if (!string.IsNullOrWhiteSpace(Config.YouTubePlaylistUrl))
                {
                    foreach (var (title, url) in YtDlpResolver.ResolvePlaylist(Config.YouTubePlaylistUrl))
                        entries.Add((title ?? "YouTube", url, url));
                }

                // CSV single videos
                if (!string.IsNullOrWhiteSpace(Config.YouTubeVideoUrlsCsv))
                {
                    foreach (var raw in Config.YouTubeVideoUrlsCsv.Split(','))
                    {
                        var v = raw.Trim();
                        if (string.IsNullOrEmpty(v)) continue;
                        var stream = YtDlpResolver.ResolveBestAudioUrl(v);
                        if (!string.IsNullOrWhiteSpace(stream))
                            entries.Add(("YouTube", stream, v)); // show the original link in chat
                    }
                }
            }
            catch (Exception ex)
            {
                BoomboxPlugin.LogError($"YouTube resolution error: {ex}");
            }

            if (entries.Count == 0)
            {
                BoomboxPlugin.LogWarning("YouTube mode enabled but no valid videos/playlist found.");
                finishedLoading = true;
                OnAllSongsLoaded?.Invoke();
                OnAllSongsLoaded = null;
                yield break;
            }

            // Build a chat message that lists all URLs we’re about to add
            {
                var lines = new List<string>();
                lines.Add("<color=#8EE8FF>Boombox YouTube queue:</color>");
                int i = 1;
                foreach (var e in entries)
                {
                    lines.Add($"{i++}. {e.displayUrl}");
                }
                ChatUtil.PostToAll(string.Join("\n", lines));
            }

            // Create streaming clips via ffmpeg->PCM->ring buffer
            foreach (var (title, streamUrl, displayUrl) in entries)
            {
                FfmpegPcmStreamer streamer = null;
                try
                {
                    streamer = new FfmpegPcmStreamer(streamUrl, title);
                    disposables.Add(streamer);

                    var clip = streamer.Clip;
                    clip.name = title ?? "YouTube";
                    clips.Add(clip);

                    // remember which URL this clip corresponds to (by instance id)
                    clipIdToYoutubeUrl[clip.GetInstanceID()] = displayUrl;

                    BoomboxPlugin.LogInfo($"Added streaming clip: {clip.name} — {displayUrl}");
                }
                catch (Exception ex)
                {
                    BoomboxPlugin.LogError($"Failed to create streaming clip: {ex}");
                    streamer?.Dispose();
                }

                // Yield to keep main thread responsive while adding many items
                yield return null;
            }

            clips.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            finishedLoading = true;
            OnAllSongsLoaded?.Invoke();
            OnAllSongsLoaded = null;
        }

        private static IEnumerator LoadAudioClip(string filePath)
        {
            BoomboxPlugin.LogInfo($"Loading {filePath}!");

            var audioType = GetAudioType(filePath);
            if (audioType == AudioType.UNKNOWN)
            {
                BoomboxPlugin.LogError($"Failed to load AudioClip from {filePath}\nUnsupported file extension!");
                yield break;
            }

            var loader = UnityWebRequestMultimedia.GetAudioClip(filePath, audioType);
            if (Config.StreamFromDisk) (loader.downloadHandler as DownloadHandlerAudioClip).streamAudio = true;

            loader.SendWebRequest();
            while (!loader.isDone) yield return null;

            if (loader.error != null)
            {
                BoomboxPlugin.LogError($"Error loading clip from path: {filePath}\n{loader.error}");
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(loader);
            if (clip && clip.loadState == AudioDataLoadState.Loaded)
            {
                BoomboxPlugin.LogInfo($"Loaded {filePath}");
                clip.name = Path.GetFileName(filePath);
                clips.Add(clip);
                yield break;
            }

            BoomboxPlugin.LogError($"Failed to load clip at: {filePath}\nThis might be due to a mismatch between the audio codec and the file extension!");
        }

        private static IEnumerator WaitForAllClips(List<Coroutine> coroutines)
        {
            foreach (var c in coroutines) yield return c;

            clips.Sort((first, second) => first.name.CompareTo(second.name));

            finishedLoading = true;
            OnAllSongsLoaded?.Invoke();
            OnAllSongsLoaded = null;
        }

        public static void ApplyClips(BoomboxItem __instance)
        {
            BoomboxPlugin.LogInfo($"Applying clips!");

            if (Config.UseDefaultSongs)
                __instance.musicAudios = __instance.musicAudios.Concat(clips).ToArray();
            else
                __instance.musicAudios = clips.ToArray();

            BoomboxPlugin.LogInfo($"Total Clip Count: {__instance.musicAudios.Length}");
        }

        // NEW: for other patches to get the URL for a given clip
        public static bool TryGetYoutubeUrl(AudioClip clip, out string url)
        {
            url = null;
            if (clip == null) return false;
            return clipIdToYoutubeUrl.TryGetValue(clip.GetInstanceID(), out url);
        }

        private static AudioType GetAudioType(string path)
        {
            var extension = Path.GetExtension(path).ToLower();

            if (extension == ".wav") return AudioType.WAV;
            if (extension == ".ogg") return AudioType.OGGVORBIS;
            if (extension == ".mp3") return AudioType.MPEG;

            BoomboxPlugin.LogError($"Unsupported extension type: {extension}");
            return AudioType.UNKNOWN;
        }
    }
}
