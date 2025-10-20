// Utilities/PcmRingBuffer.cs
using System;

namespace CustomBoomboxTracks.Utilities
{
    internal class PcmRingBuffer
    {
        private readonly float[] _buf;
        private int _writePos, _readPos;
        private int _count;
        private readonly object _lock = new object();

        public PcmRingBuffer(int capacitySamples)
        {
            _buf = new float[capacitySamples];
        }

        public void Write(float[] src, int count)
        {
            lock (_lock)
            {
                int written = 0;
                while (written < count)
                {
                    int n = Math.Min(count - written, _buf.Length - _writePos);
                    Array.Copy(src, written, _buf, _writePos, n);
                    _writePos = (_writePos + n) % _buf.Length;
                    written += n;

                    // advance count; if overflow, drop oldest
                    _count = Math.Min(_count + n, _buf.Length);
                    if (_count == _buf.Length) _readPos = _writePos;
                }
            }
        }

        public int Read(float[] dst, int count)
        {
            lock (_lock)
            {
                int toRead = Math.Min(count, _count);
                int read = 0;
                while (read < toRead)
                {
                    int n = Math.Min(toRead - read, _buf.Length - _readPos);
                    Array.Copy(_buf, _readPos, dst, read, n);
                    _readPos = (_readPos + n) % _buf.Length;
                    read += n;
                    _count -= n;
                }
                // zero remainder if underflow
                for (int i = read; i < count; i++) dst[i] = 0f;
                return read;
            }
        }
    }
}
