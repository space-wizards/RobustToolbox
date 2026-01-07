using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Network.Transfer;

#pragma warning disable RA0004 // Task.Result

internal abstract class TransferImplWebSocket : BaseTransferImpl
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

    internal const string KeyHeaderName = "RT-Key";
    internal const string UserIdHeaderName = "RT-UserId";

    internal const int BufferSize = 16384;
    internal const int MaxKeySize = 96;
    internal const int MaxHeaderSize = 128;
    internal const int RandomKeyBytes = 32;

    private readonly byte[] _headerBuffer = ArrayPool<byte>.Shared.Rent(MaxHeaderSize);

    private readonly SemaphoreSlim _socketSemaphore = new(1, 1);
    private readonly CancellationTokenSource _readCancel = new();

    private readonly Dictionary<long, ChannelWriter<ArraySegment<byte>>> _receivingChannels = [];

    public WebSocket? WebSocket;

    protected TransferImplWebSocket(ISawmill sawmill, BaseTransferManager parent, INetChannel channel)
        : base(sawmill, parent, channel)
    {
    }

    public override Stream StartTransfer(TransferStartInfo startInfo)
    {
        if (WebSocket == null)
            throw new InvalidOperationException("Player not connected yet");

        var id = Interlocked.Increment(ref OutgoingIdCounter);

        return new SendStream(this, id, startInfo.MessageKey);
    }

    protected async void ReadThread()
    {
        DebugTools.Assert(WebSocket != null);

        try
        {
            var cancel = _readCancel.Token;
            while (!cancel.IsCancellationRequested)
            {
                var receiveResult = await WebSocket
                    .ReceiveAsync(_headerBuffer.AsMemory(), cancel)
                    .ConfigureAwait(false);

                if (!receiveResult.EndOfMessage)
                    throw new ProtocolViolationException("Header did not fit in one receive");

                if (receiveResult.MessageType != WebSocketMessageType.Binary)
                    throw new ProtocolViolationException("Data must be binary!");

                // Parse received data.
                var receivedData = _headerBuffer.AsMemory(0, receiveResult.Count);
                ParseHeader(receivedData.Span, out var flags, out var transferId, out var key);

                if (!_receivingChannels.TryGetValue(transferId, out var channel))
                {
                    if ((flags & TransferFlags.Start) == 0)
                        throw new ProtocolViolationException($"Received data for unknown transfer {transferId}");

                    DebugTools.Assert(key != null);

                    Sawmill.Verbose($"Starting transfer stream {transferId} with key {key}");

                    var fullChannel = System.Threading.Channels.Channel.CreateBounded<ArraySegment<byte>>(
                        new BoundedChannelOptions(4)
                        {
                            SingleReader = true,
                            SingleWriter = true
                        });

                    channel = fullChannel.Writer;
                    _receivingChannels.Add(transferId, channel);

                    TransferReceived(key, fullChannel.Reader);
                }

                if ((flags & TransferFlags.HasData) != 0)
                    await ReceiveTransferData(WebSocket, channel, cancel).ConfigureAwait(false);

                if ((flags & TransferFlags.Finish) != 0)
                {
                    Sawmill.Verbose($"Finishing transfer stream {transferId}");

                    channel.Complete();
                    _receivingChannels.Remove(transferId);
                }
            }
        }
        catch (Exception e)
        {
            Sawmill.Error($"Error reading transfer socket: {e}");
            Channel.Disconnect("Error in transfer socket");
        }
    }

    private sealed class SendStream : SaneStream
    {
        private readonly TransferImplWebSocket _parent;
        private readonly long _id;
        private readonly string _key;

        private readonly byte[] _headerBuffer;
        private readonly byte[] _dataBuffer;
        private bool _isFirstTransmission = true;
        private int _bufferPos;

        public override bool CanWrite => true;

        public SendStream(TransferImplWebSocket parent, long id, string key)
        {
            // This just has to be < buffer size & < ushort.MaxValue
            // (when accounting for UTF-8 possibly being more code units than UTF-16)
            if (Encoding.UTF8.GetByteCount(key) > MaxKeySize)
                throw new ArgumentException("Key too long");

            _parent = parent;
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
            var ws = _parent.WebSocket!;

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
            using (await _parent._socketSemaphore.WaitGuardAsync().ConfigureAwait(false))
            {
                await ws.SendAsync(
                        _headerBuffer.AsMemory(0, headerLength),
                        WebSocketMessageType.Binary,
                        endOfMessage: true,
                        cancel)
                    .ConfigureAwait(false);

                if (_bufferPos > 0)
                {
                    await ws.SendAsync(
                            _dataBuffer.AsMemory(0, _bufferPos),
                            WebSocketMessageType.Binary,
                            endOfMessage: true,
                            cancel)
                        .ConfigureAwait(false);

                    _bufferPos = 0;
                }
            }

            _isFirstTransmission = false;
        }

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

        ~SendStream()
        {
            // Have to do this so the stream isn't permanently hanging on the receiving side.
            FlushAsync(finish: true).AsTask().Wait();
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

    private static async ValueTask ReceiveTransferData(
        WebSocket ws,
        ChannelWriter<ArraySegment<byte>> channel,
        CancellationToken cancel)
    {
        while (!cancel.IsCancellationRequested)
        {
            var buf = ArrayPool<byte>.Shared.Rent(BufferSize);
            var result = await ws.ReceiveAsync(buf.AsMemory(), cancel).ConfigureAwait(false);

            if (result.MessageType != WebSocketMessageType.Binary)
                throw new ProtocolViolationException("Data must be binary!");

            await channel.WriteAsync(new ArraySegment<byte>(buf, 0, result.Count), cancel).ConfigureAwait(false);

            if (result.EndOfMessage)
                break;
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        WebSocket?.Dispose();
        _readCancel.Cancel();

        foreach (var channel in _receivingChannels.Values)
        {
            channel.Complete();
        }

        ArrayPool<byte>.Shared.Return(_headerBuffer);
    }

    private enum Opcode : byte
    {
        Transfer = 0,
    }

    [Flags]
    private enum TransferFlags : byte
    {
        None = 0,
        Start = 1 << 0,
        Finish = 1 << 1,
        HasData = 1 << 2,
    }
}
