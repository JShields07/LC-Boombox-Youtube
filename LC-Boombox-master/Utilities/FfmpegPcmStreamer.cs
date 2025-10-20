FfmpegPcmStreamer// Utilities/FfmpegPcmStreamer.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using CustomBoomboxTracks.Configuration;

namespace CustomBoomboxTracks.Utilities
{
    internal class FfmpegPcmStreamer : IDisposable
    {
        const int Channels = 2;
        const int SampleRate = 48000;
        const int BytesPerSample = 2; // s16le

        private readonly string _sourceUrl;
        private readonly string _title;
        private readonly PcmRingBuffer _ring;
        private Process _proc;
        private Thread _readerThread;
        private volatile bool _stopping;

        public string Title => _title;
        public AudioClip Clip { get; private set; }

        public FfmpegPcmStreamer(string sourceUrl, string title)
        {
            _sourceUrl = sourceUrl;
            _title = title;
            _ring = new PcmRingBuffer(SampleRate * Channels * 30); // ~30s buffer
            Start();
        }

        private void Start()
        {
            var psi = new ProcessStartInfo
            {
                FileName = Config.FfmpegPath,
                Arguments = $"-hide_banner -loglevel error -i \"{_sourceUrl}\" -f s16le -ac {Channels} -ar {SampleRate} pipe:1",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _proc = new Process { StartInfo = psi };
            _proc.Start();

            _readerThread = new Thread(ReadLoop) { IsBackground = true };
            _readerThread.Start();

            // A very long streaming clip; Unity pulls via PCM callback
            Clip = AudioClip.Create(_title ?? "YouTube", SampleRate * 3600, Channels, SampleRate, true, OnAudioRead);
        }

        private void ReadLoop()
        {
            Stream stdout = _proc.StandardOutput.BaseStream;
            byte[] buf = new byte[BytesPerSample * Channels * 1024];
            short[] s16 = new short[buf.Length / 2];
            float[] f32 = new float[s16.Length];

            try
            {
                while (!_stopping)
                {
                    int got = stdout.Read(buf, 0, buf.Length);
                    if (got <= 0) break;

                    Buffer.BlockCopy(buf, 0, s16, 0, got);
                    int samples = got / 2;
                    for (int i = 0; i < samples; i++)
                        f32[i] = s16[i] / 32768f;

                    _ring.Write(f32, samples);
                }
            }
            catch (Exception ex)
            {
                BoomboxPlugin.LogError($"FFmpeg read loop error: {ex}");
            }
        }

        private void OnAudioRead(float[] data)
        {
            _ring.Read(data, data.Length);
        }

        public void Dispose()
        {
            _stopping = true;
            try { if (!_proc.HasExited) _proc.Kill(); } catch { }
            try { _proc?.Dispose(); } catch { }
        }
    }
}
