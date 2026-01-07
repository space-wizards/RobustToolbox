using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Network.Transfer;

internal abstract class BaseTransferImpl(ISawmill sawmill, BaseTransferManager parent, INetChannel channel) : IDisposable
{
    protected readonly INetChannel Channel = channel;
    protected readonly ISawmill Sawmill = sawmill;

    protected long OutgoingIdCounter;

    public abstract Task ServerInit();
    public abstract Task ClientInit(CancellationToken cancel);
    public abstract Stream StartTransfer(TransferStartInfo startInfo);

    protected void TransferReceived(string key, ChannelReader<ArraySegment<byte>> reader)
    {
        var stream = new ReceiveStream(reader);
        parent.TransferReceived(key, Channel, stream);
    }

    private sealed class ReceiveStream : SaneStream
    {
        private readonly ChannelReader<ArraySegment<byte>> _bufferChannel;

        private ArraySegment<byte> _currentBuffer;

        public override bool CanRead => true;

        public ReceiveStream(ChannelReader<ArraySegment<byte>> bufferChannel)
        {
            _bufferChannel = bufferChannel;
        }

        public override int Read(Span<byte> buffer)
        {
            var read = 0;
            var remainingSpan = buffer;

            while (remainingSpan.Length > 0)
            {
                if (_currentBuffer.Array == null || _currentBuffer.Count <= 0)
                {
                    if (_currentBuffer.Array != null)
                    {
                        ArrayPool<byte>.Shared.Return(_currentBuffer.Array);
                        _currentBuffer = default;
                    }

                    if (!_bufferChannel.TryRead(out _currentBuffer))
                    {
                        // Only block if we haven't read any bytes yet.
                        if (read > 0 || !ReadNewBufferSync())
                            return read;
                    }
                }

                DebugTools.Assert(_currentBuffer.Array != null);

                var remainingBuffer = _currentBuffer.Count;
                var thisRead = Math.Min(remainingSpan.Length, remainingBuffer);

                _currentBuffer.AsSpan(0, thisRead).CopyTo(remainingSpan);
                remainingSpan = remainingSpan[thisRead..];
                _currentBuffer = _currentBuffer[thisRead..];
                read += thisRead;
            }

            return read;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            // TODO: Async impl.
            return base.ReadAsync(buffer, cancellationToken);
        }

        private bool ReadNewBufferSync()
        {
            DebugTools.Assert(_currentBuffer.Array == null);

            var waitToRead = _bufferChannel.WaitToReadAsync();
#pragma warning disable RA0004
            var waitToReadResult = waitToRead.AsTask().Result;
#pragma warning restore RA0004
            if (!waitToReadResult)
                return false;

            return _bufferChannel.TryRead(out _currentBuffer);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && _currentBuffer.Array != null)
                ArrayPool<byte>.Shared.Return(_currentBuffer.Array);
        }
    }

    public virtual void Dispose()
    {
    }
}
