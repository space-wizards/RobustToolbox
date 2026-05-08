using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Network.Transfer;

#pragma warning disable RA0004 // Task.Result

internal abstract class TransferImplWebSocket : BaseTransferImpl
{
    internal const string KeyHeaderName = "RT-Key";
    internal const string UserIdHeaderName = "RT-UserId";

    internal const int RandomKeyBytes = 32;

    private readonly byte[] _headerBuffer = ArrayPool<byte>.Shared.Rent(MaxHeaderSize);

    private readonly CancellationTokenSource _readCancel = new();

    public WebSocket? WebSocket;

    protected TransferImplWebSocket(ISawmill sawmill, BaseTransferManager parent, INetChannel channel)
        : base(sawmill, parent, channel)
    {
    }

    protected override bool BoundedChannel => true;

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

                BaseTransferManager.ReceivedDataMetrics.Inc(receiveResult.Count);

                if (!receiveResult.EndOfMessage)
                    throw new ProtocolViolationException("Header did not fit in one receive");

                if (receiveResult.MessageType != WebSocketMessageType.Binary)
                    throw new ProtocolViolationException("Data must be binary!");

                // Parse received data.
                var receivedData = _headerBuffer.AsMemory(0, receiveResult.Count);
                HandleHeaderReceived(receivedData, out var flags, out var transferId, out var channel);

                if ((flags & TransferFlags.HasData) != 0)
                    await ReceiveTransferData(WebSocket, channel, cancel).ConfigureAwait(false);

                HandlePostData(flags, transferId, channel);
            }
        }
        catch (Exception e)
        {
            Sawmill.Error($"Error reading transfer socket: {e}");
            Channel.Disconnect("Error in transfer socket");
        }
    }

    private sealed class SendStream : ChunkedSendStream
    {
        public SendStream(TransferImplWebSocket parent, long id, string key) : base(parent, id, key)
        {
        }

        protected override async ValueTask SendChunkAsync(ArraySegment<byte> buffer, CancellationToken cancel)
        {
            var ws = ((TransferImplWebSocket)Parent).WebSocket!;

            BaseTransferManager.SentDataMetrics.Inc(buffer.Count);

            await ws.SendAsync(
                    buffer,
                    WebSocketMessageType.Binary,
                    endOfMessage: true,
                    cancel)
                .ConfigureAwait(false);
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

            BaseTransferManager.ReceivedDataMetrics.Inc(result.Count);

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

        ArrayPool<byte>.Shared.Return(_headerBuffer);
    }
}
