using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lidgren.Network;

namespace Robust.Shared.Network;

public sealed partial class NetManager
{
    // Encryption is relatively expensive, so we want to not do it on the main thread.
    // We can't *just* thread pool everything though, because most messages still require strict ordering guarantees.
    // For this reason, we create an "encryption channel" per player and use that to do encryption of ordered messages.

    private void SetupEncryptionChannel(NetChannel netChannel)
    {
        if (!_config.GetCVar(CVars.NetEncryptionThread))
            return;

        // We create the encryption channel even if the channel isn't encrypted.
        // This is to ensure consistency of behavior between local testing and production scenarios.

        var channel = Channel.CreateBounded<EncryptChannelItem>(
            new BoundedChannelOptions(_config.GetCVar(CVars.NetEncryptionThreadChannelSize))
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

        netChannel.EncryptionChannel = channel.Writer;
        netChannel.EncryptionChannelTask = Task.Run(async () =>
        {
            await EncryptionThread(channel.Reader, netChannel);
        });
    }

    private async Task EncryptionThread(ChannelReader<EncryptChannelItem> itemChannel, NetChannel netChannel)
    {
        await foreach (var item in itemChannel.ReadAllAsync())
        {
            try
            {
                CoreEncryptSendMessage(netChannel, item);
            }
            catch (Exception e)
            {
                _logger.Error($"Error while encrypting message for send on channel {netChannel}: {e}");
            }
        }
    }

    private void CoreSendMessage(
        NetChannel channel,
        NetMessage message)
    {
        if (!channel.IsConnected)
        {
            _logger.Error(
                $"Tried to send message \"{message}\" to disconnected channel {channel}\n{Environment.StackTrace}");
            return;
        }

        var packet = BuildMessage(message, channel.Connection.Peer);
        var method = message.DeliveryMethod;
        var seqChannel = message.SequenceChannel;

        LogSend(message, method, packet);

        var item = new EncryptChannelItem
        {
            Message = packet,
            Method = method,
            SequenceChannel = seqChannel,
            Owner = this,
            RobustMessage = message,
        };

        // If the message is ordered, we have to send it to the encryption channel.
        if (method is NetDeliveryMethod.ReliableOrdered
            or NetDeliveryMethod.ReliableSequenced
            or NetDeliveryMethod.UnreliableSequenced)
        {
            if (channel.EncryptionChannel is { } encryptionChannel)
            {
                var task = encryptionChannel.WriteAsync(item);
                if (!task.IsCompletedSuccessfully)
                    task.AsTask().Wait();
            }
            else
            {
                CoreEncryptSendMessage(channel, item);
            }
        }
        else
        {
            if (Environment.CurrentManagedThreadId == _mainThreadId)
            {
                ThreadPool.UnsafeQueueUserWorkItem(
                    static state => CoreEncryptSendMessage(state.channel, state.item),
                    new
                    {
                        channel, item
                    },
                    preferLocal: true);
            }
            else
            {
                CoreEncryptSendMessage(channel, item);
            }
        }
    }

    private static void CoreEncryptSendMessage(NetChannel channel, EncryptChannelItem item)
    {
        channel.Encryption?.Encrypt(item.Message);

        var result = channel.Connection.Peer.SendMessage(item.Message, channel.Connection, item.Method, item.SequenceChannel);
        if (result is not (NetSendResult.Sent or NetSendResult.Queued))
        {
            // Logging stack trace here won't be useful as it'll likely be thread pooled on production scenarios.
            item.Owner._logger.Warning(
                $"Failed to send message {item.RobustMessage} to {channel} via Lidgren: {result}");
        }
    }

    private struct EncryptChannelItem
    {
        public required NetOutgoingMessage Message;
        public required NetDeliveryMethod Method;
        public required int SequenceChannel;
        public required NetMessage RobustMessage;
        public required NetManager Owner;
    }
}
