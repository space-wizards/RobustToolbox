using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Collections;
using Robust.Shared.Network.Messages.Transfer;

namespace Robust.Shared.Network.Transfer;

internal abstract partial class BaseTransferManager
{
    private readonly Lock _waitingSendChannelLock = new();
    private readonly Dictionary<INetChannel, TaskCompletionSource> _waitingSendChannels = [];
    private ValueList<(INetChannel, TaskCompletionSource)> _sendChannelQueue;

    public void FrameUpdate()
    {
        lock (_waitingSendChannelLock)
        {
            foreach (var (channel, tcs) in _waitingSendChannels)
            {
                if (!channel.IsConnected || SendCheck(channel))
                    _sendChannelQueue.Add((channel, tcs));
            }

            // Remove BEFORE dispatching any TCSes, so we don't try to add to the list from a callback.
            foreach (var (channel, _) in _sendChannelQueue)
            {
                _waitingSendChannels.Remove(channel);
            }
        }

        foreach (var (channel, tcs) in _sendChannelQueue)
        {
            if (!channel.IsConnected)
                tcs.TrySetException(new NetChannelClosedException("Channel closed"));
            else
                tcs.TrySetResult();
        }

        _sendChannelQueue.Clear();
    }

    public async ValueTask WaitToSend(INetChannel channel)
    {
        if (SendCheck(channel))
            return;

        TaskCompletionSource tcs;
        lock (_waitingSendChannelLock)
        {
            ref var tcsSlot = ref CollectionsMarshal.GetValueRefOrAddDefault(_waitingSendChannels, channel, out _);
            tcsSlot ??= new TaskCompletionSource();
            tcs = tcsSlot;
        }

        await tcs.Task;
    }

    private static bool SendCheck(INetChannel channel)
    {
        return channel.CanSendImmediately(MsgTransferData.Method, MsgTransferData.Channel);
    }

    private sealed class NetChannelClosedException(string message) : Exception(message);
}
