# ---------- Custom Boombox Music (boombox.cfg) ----------

[Config]
# If true, the vanilla/default boombox songs are included in the rotation
Use Default Songs = false

# Only applies to LOCAL FILE playback (ignored for YouTube mode):
# If true, Unity streams from disk instead of loading whole clips in RAM.
# (Prevents playing the same local song twice at once.)
Stream Audio From Disk = false


[YouTube]
# Master switch: if true, the mod streams audio directly from YouTube (no caching to disk)
UseYouTube = true

# Optional: a YouTube playlist URL. Leave empty if you don’t want to use a playlist.
# Example: https://www.youtube.com/playlist?list=PLxxxxxxxxxxxxxxxx
PlaylistUrl = 

# Optional: a comma-separated list of individual YouTube video URLs.
# Example: https://youtu.be/AAA111, https://www.youtube.com/watch?v=BBB222
VideoUrlsCsv = 

# Path to yt-dlp. If yt-dlp is in your PATH, just leave "yt-dlp".
# Otherwise put an absolute path. Windows example:
# C:\Tools\yt-dlp.exe
YtDlpPath = yt-dlp

# Path to ffmpeg. If ffmpeg is in your PATH, just leave "ffmpeg".
# Otherwise put an absolute path. Windows example:
# C:\Tools\ffmpeg\bin\ffmpeg.exe
FfmpegPath = ffmpeg

