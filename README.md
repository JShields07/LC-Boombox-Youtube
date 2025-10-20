# Custom Boombox Music (YouTube Streaming Edition)

A BepInEx mod for **Lethal Company** that lets the in-game boombox stream audio
directly from YouTube — **no caching, no local files required**.

When enabled, the mod uses `yt-dlp` and `ffmpeg` to live-decode YouTube audio
and feed it directly into Unity’s audio system as a PCM stream.

---

## ✨ Features
- Stream songs and playlists straight from YouTube
- No temporary or cached files
- Optional fallback to local MP3/OGG/WAV files
- Displays all loaded YouTube links in the in-game chat
- Posts “Now playing” messages with clickable YouTube URLs
