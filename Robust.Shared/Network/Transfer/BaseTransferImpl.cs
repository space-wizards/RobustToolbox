using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Network.Transfer;

internal abstract class BaseTransferImpl(ISawmill sawmill, BaseTransferManager parent, INetChannel channel) : IDisposable
{
    // Custom framing format is as follows.
    // <header message>
    //   uint8 opcode
    //   uint8 flags
    //   int64 transfer ID
    //   [if start message]:
    //     uint8 key length
    //     byte[] key
    // <data message>
    //    just the fucking data lol

    internal const int BufferSize = 16384;
    internal const int MaxKeySize = 96;
    internal const int MaxHeaderSize = 128;

    protected readonly INetChannel Channel = channel;
    protected readonly ISawmill Sawmill = sawmill;

    protected long OutgoingIdCounter;

    private readonly Dictionary<long, ChannelWriter<ArraySegment<byte>>> _receivingChannels = [];

    private readonly SemaphoreSlim _socketSemaphore = new(1, 1);
    internal readonly BaseTransferManager Parent = parent;

    public abstract Task ServerInit();
    public abstract Task ClientInit(CancellationToken cancel);
    public abstract Stream StartTransfer(TransferStartInfo startInfo);

    protected abstract bool BoundedChannel { get; }

    protected void TransferReceived(string key, ChannelReader<ArraySegment<byte>> reader)
    {
        var stream = new ReceiveStream(reader);
        Parent.TransferReceived(key, Channel, stream);
    }

    protected void HandleHeaderReceived(
        ReadOnlyMemory<byte> data,
        out TransferFlags flags,
        out long transferId,
        out ChannelWriter<ArraySegment<byte>> channel)
    {
        ParseHeader(data.Span, out flags, out transferId, out var key);

        if (!_receivingChannels.TryGetValue(transferId, out channel!))
        {
            if ((flags & TransferFlags.Start) == 0)
                throw new ProtocolViolationException($"Received data for unknown transfer {transferId}");

            DebugTools.Assert(key != null);

            Sawmill.Verbose($"Starting transfer stream {transferId} with key {key}");

            var fullChannel = BoundedChannel
                ? System.Threading.Channels.Channel.CreateBounded<ArraySegment<byte>>(
                    new BoundedChannelOptions(4)
                    {
                        SingleReader = true,
                        SingleWriter = true
                    })
                : System.Threading.Channels.Channel.CreateUnbounded<ArraySegment<byte>>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = true
                });

            channel = fullChannel.Writer;
            _receivingChannels.Add(transferId, channel);

            TransferReceived(key, fullChannel.Reader);
        }
    }

    protected void HandlePostData(TransferFlags flags, long transferId, ChannelWriter<ArraySegment<byte>> channel)
    {
        if ((flags & TransferFlags.Finish) != 0)
        {
            Sawmill.Verbose($"Finishing transfer stream {transferId}");

            channel.Complete();
            _receivingChannels.Remove(transferId);
        }
    }

    private static void ParseHeader(
        ReadOnlySpan<byte> buf,
        out TransferFlags flags,
        out long transferId,
        out string? key)
    {
        flags = (TransferFlags)buf[1];
        transferId = BinaryPrimitives.ReadInt64LittleEndian(buf[2..10]);

        if ((flags & TransferFlags.Start) != 0)
        {
            var keyLength = buf[10];
            key = Encoding.UTF8.GetString(buf.Slice(11, keyLength));
        }
        else
        {
            key = null;
        }
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

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
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
                        if (read > 0 || !await ReadNewBufferAsync())
                            return read;
                    }
                }

                DebugTools.Assert(_currentBuffer.Array != null);

                var remainingBuffer = _currentBuffer.Count;
                var thisRead = Math.Min(remainingSpan.Length, remainingBuffer);

                _currentBuffer.AsMemory(0, thisRead).CopyTo(remainingSpan);
                remainingSpan = remainingSpan[thisRead..];
                _currentBuffer = _currentBuffer[thisRead..];
                read += thisRead;
            }

            return read;
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

        private async Task<bool> ReadNewBufferAsync()
        {
            DebugTools.Assert(_currentBuffer.Array == null);

            var waitToRead = await _bufferChannel.WaitToReadAsync();
            if (!waitToRead)
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

    protected abstract class ChunkedSendStream : SaneStream
    {
        protected readonly BaseTransferImpl Parent;
        private readonly long _id;
        private readonly string _key;

        private readonly byte[] _headerBuffer;
        private readonly byte[] _dataBuffer;
        private bool _isFirstTransmission = true;
        private int _bufferPos;

        public override bool CanWrite => true;

        public ChunkedSendStream(BaseTransferImpl parent, long id, string key)
        {
            // This just has to be < buffer size & < ushort.MaxValue
            // (when accounting for UTF-8 possibly being more code units than UTF-16)
            if (Encoding.UTF8.GetByteCount(key) > MaxKeySize)
                throw new ArgumentException("Key too long");

            Parent = parent;
            _id = id;
            _key = key;

            _headerBuffer = ArrayPool<byte>.Shared.Rent(MaxHeaderSize);
            _dataBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            while (buffer.Length > 0)
            {
                var remainingBufferSpace = _dataBuffer.AsSpan(_bufferPos);
                var thisChunk = Math.Min(remainingBufferSpace.Length, buffer.Length);
                var thisSpan = buffer[..thisChunk];

                thisSpan.CopyTo(remainingBufferSpace);
                _bufferPos += thisChunk;

                if (_bufferPos == _dataBuffer.Length)
                    Flush();

                buffer = buffer[thisChunk..];
            }
        }

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            while (buffer.Length > 0)
            {
                var remainingBufferSpace = _dataBuffer.AsSpan(_bufferPos);
                var thisChunk = Math.Min(remainingBufferSpace.Length, buffer.Length);
                var thisSpan = buffer[..thisChunk];

                thisSpan.Span.CopyTo(remainingBufferSpace);
                _bufferPos += thisChunk;

                if (_bufferPos == _dataBuffer.Length)
                    await FlushAsync(cancellationToken).ConfigureAwait(false);

                buffer = buffer[thisChunk..];
            }
        }

        public override void Flush()
        {
            FlushAsync().Wait();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await FlushAsync(finish: false, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask FlushAsync(bool finish, CancellationToken cancel = default)
        {
            var headerLength = 10;

            var opcode = Opcode.Transfer;
            var flags = TransferFlags.None;
            if (_isFirstTransmission)
                flags |= TransferFlags.Start;
            if (_bufferPos > 0)
                flags |= TransferFlags.HasData;
            if (finish)
                flags |= TransferFlags.Finish;

            if (flags == TransferFlags.None)
            {
                // Nothing to flush, whatsoever.
                return;
            }

            _headerBuffer[0] = (byte)opcode;
            _headerBuffer[1] = (byte)flags;
            BinaryPrimitives.WriteInt64LittleEndian(_headerBuffer.AsSpan(2..10), _id);

            if (_isFirstTransmission)
            {
                var written = Encoding.UTF8.GetBytes(_key, _headerBuffer.AsSpan(11..));
                DebugTools.Assert(written < byte.MaxValue);
                _headerBuffer[10] = (byte)written;

                headerLength += 1;
                headerLength += written;
            }

            // Send.
            using (await Parent._socketSemaphore.WaitGuardAsync().ConfigureAwait(false))
            {
                await SendChunkAsync(
                        new ArraySegment<byte>(_headerBuffer, 0, headerLength),
                        cancel)
                    .ConfigureAwait(false);

                if (_bufferPos > 0)
                {
                    await SendChunkAsync(
                            new ArraySegment<byte>(_dataBuffer, 0, _bufferPos),
                            cancel)
                        .ConfigureAwait(false);

                    _bufferPos = 0;
                }
            }

            _isFirstTransmission = false;
        }

        protected abstract ValueTask SendChunkAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken);

        protected override void Dispose(bool disposing)
        {
            FlushAsync(finish: true).AsTask().Wait();
            DisposeCore();
        }

        public override async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            await FlushAsync(finish: true).ConfigureAwait(false);
            DisposeCore();
        }

        private void DisposeCore()
        {
            ArrayPool<byte>.Shared.Return(_dataBuffer);
            ArrayPool<byte>.Shared.Return(_headerBuffer);
        }

        ~ChunkedSendStream()
        {
            // Have to do this so the stream isn't permanently hanging on the receiving side.
            FlushAsync(finish: true).AsTask().Wait();
        }
    }

    public virtual void Dispose()
    {
        foreach (var channel in _receivingChannels.Values)
        {
            channel.Complete();
        }
    }

    protected enum Opcode : byte
    {
        Transfer = 0,
    }

    [Flags]
    protected enum TransferFlags : byte
    {
        None = 0,
        Start = 1 << 0,
        Finish = 1 << 1,
        HasData = 1 << 2,
    }
}
