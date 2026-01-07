using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Robust.Shared.Asynchronous;
using Robust.Shared.Log;

namespace Robust.Shared.Network.Transfer;

internal abstract class BaseTransferManager
{
    private readonly NetMessageAccept _side;
    private readonly ITaskManager _taskManager;

    protected readonly Dictionary<string, RegisteredKey> RegisteredKeys = [];
    protected readonly ISawmill Sawmill;

    private protected BaseTransferManager(ILogManager logManager, NetMessageAccept side, ITaskManager taskManager)
    {
        _side = side;
        _taskManager = taskManager;
        Sawmill = logManager.GetSawmill("net.transfer");
    }

    public void RegisterTransferMessage(
        string key,
        Action<TransferReceivedEvent>? rxCallback = null,
        NetMessageAccept accept = NetMessageAccept.Both)
    {
        if ((accept & ~NetMessageAccept.Both) != 0)
            throw new ArgumentException("Invalid accept given: must be client, server, or both");

        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(RegisteredKeys, key, out var exists);
        if (exists)
            throw new InvalidOperationException($"Key '{key}' was already registered!");

        slot = new RegisteredKey();

        if ((accept & _side) > 0)
            slot.Callback = rxCallback;
    }

    internal void TransferReceived(string key, INetChannel channel, Stream stream)
    {
        if (!RegisteredKeys.TryGetValue(key, out var registered))
            throw new Exception($"Unknown key: {key}");

        if (registered.Callback == null)
            throw new Exception($"Key is send-only: {key}");

        _taskManager.RunOnMainThread(() =>
        {
            registered.Callback(new TransferReceivedEvent(key, channel, stream));
        });
    }

    protected sealed class RegisteredKey
    {
        public Action<TransferReceivedEvent>? Callback;
    }
}
