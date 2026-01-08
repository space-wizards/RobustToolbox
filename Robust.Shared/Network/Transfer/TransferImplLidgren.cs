using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Robust.Shared.Log;
using Robust.Shared.Network.Messages.Transfer;
using Robust.Shared.Utility;

namespace Robust.Shared.Network.Transfer;

internal sealed class TransferImplLidgren(
    ISawmill sawmill,
    INetChannel channel,
    BaseTransferManager transferManager,
    INetManager netManager) : BaseTransferImpl(sawmill, transferManager, channel)
{
    private TaskCompletionSource? _serverInitTcs;

    private (TransferFlags Flags, long TransferId, ChannelWriter<ArraySegment<byte>> Channel)? _parsedHeader;

    public override Task ServerInit()
    {
        var initMsg = new MsgTransferInit();

        netManager.ServerSendMessage(initMsg, Channel);

        _serverInitTcs = new TaskCompletionSource();
        return _serverInitTcs.Task;
    }

    public override Task ClientInit(CancellationToken cancel)
    {
        var initMsg = new MsgTransferAckInit();

        netManager.ClientSendMessage(initMsg);

        return Task.CompletedTask;
    }

    public override Stream StartTransfer(TransferStartInfo startInfo)
    {
        var id = Interlocked.Increment(ref OutgoingIdCounter);

        return new SendStream(Channel, this, id, startInfo.MessageKey);
    }

    // We can't meaningfully communicate backpressure into Lidgren so this is our only option.
    protected override bool BoundedChannel => false;

    public void ReceiveInitAck()
    {
        _serverInitTcs?.TrySetResult();
    }

    public void ReceiveData(MsgTransferData data)
    {
        DebugTools.Assert(data.Data.Array != null);

        BaseTransferManager.ReceivedDataMetrics.Inc(data.Data.Count);

        // Header message
        if (!_parsedHeader.HasValue)
        {
            HandleHeaderReceived(data.Data, out var flags, out var transferId, out var channel);
            ArrayPool<byte>.Shared.Return(data.Data.Array);

            if ((flags & TransferFlags.HasData) == 0)
                HandlePostData(flags, transferId, channel);
            else
                _parsedHeader = (flags, transferId, channel);

            return;
        }

        // Data message

        {
            var (flags, transferId, channel) = _parsedHeader.Value;

            _parsedHeader = null;

            channel.WriteAsync(data.Data).AsTask().Wait();

            HandlePostData(flags, transferId, channel);
        }
    }

    private sealed class SendStream : ChunkedSendStream
    {
        private readonly INetChannel _channel;

        public SendStream(INetChannel channel, TransferImplLidgren parent, long id, string key) : base(parent, id, key)
        {
            _channel = channel;
        }

        protected override async ValueTask SendChunkAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            if (!_channel.IsConnected)
                throw new InvalidOperationException("Channel is disconnected");

            BaseTransferManager.SentDataMetrics.Inc(buffer.Count);

            await Parent.Parent.WaitToSend(_channel);

            var msgData = new MsgTransferData
            {
                Data = buffer
            };

            _channel.SendMessage(msgData);
        }
    }
}
