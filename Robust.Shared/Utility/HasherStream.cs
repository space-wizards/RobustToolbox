using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Robust.Shared.Utility
{
    /// <summary>
    ///     Stream that passes through data to/from another stream while running a <see cref="IncrementalHash"/> on it.
    /// </summary>
    internal sealed class HasherStream : Stream
    {
        private readonly Stream _wrapping;
        private readonly IncrementalHash _hash;
        private readonly bool _leaveOpen;

        public HasherStream(Stream wrapping, IncrementalHash hash, bool leaveOpen=false)
        {
            _wrapping = wrapping;
            _hash = hash;
            _leaveOpen = leaveOpen;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_leaveOpen)
            {
                _wrapping.Dispose();
            }
        }

        public override ValueTask DisposeAsync()
        {
            if (!_leaveOpen)
            {
                return _wrapping.DisposeAsync();
            }

            return default;
        }

        public override void Flush()
        {
            _wrapping.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _wrapping.Read(buffer, offset, count);

            if (read > 0)
            {
                _hash.AppendData(buffer, offset, read);
            }

            return read;
        }

        public override int Read(Span<byte> buffer)
        {
            var read = _wrapping.Read(buffer);

            if (read > 0)
            {
                _hash.AppendData(buffer[..read]);
            }

            return read;
        }

        public override int ReadByte()
        {
            Span<byte> span = stackalloc byte[1];
            var read = Read(span);

            return read == 0 ? -1 : span[0];
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _hash.AppendData(buffer, offset, count);
            _wrapping.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _hash.AppendData(buffer);
            _wrapping.Write(buffer);
        }

        public override void WriteByte(byte value)
        {
            Span<byte> span = stackalloc byte[] {value};
            Write(span);
        }

        public override bool CanRead => _wrapping.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => _wrapping.CanWrite;
        public override long Length => _wrapping.Length;

        public override long Position
        {
            get => _wrapping.Position;
            set => throw new NotSupportedException();
        }
    }
}
