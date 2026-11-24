using System.Collections.Generic;
using System.Threading;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Upload;
using Timer = Robust.Shared.Timing.Timer;

namespace Robust.Server.Upload;

/// <summary>
///     Delivers uploaded content to clients and coordinates prototype dump acknowledgements.
/// </summary>
internal sealed partial class UploadedContentManager
{
    [Dependency] private IServerNetManager _netManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private GamePrototypeLoadManager _prototypeLoadManager = default!;
    [Dependency] private NetworkResourceManager _networkResourceManager = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private readonly Dictionary<INetChannel, DumpState> _pendingDumps = new();
    private int _nextDumpKey = NetworkResourceManager.AckInitial + 1;

    private ISawmill _sawmill = default!;

    private sealed class DumpState
    {
        public int AckKey;
        public List<string>? Deferred;
        public CancellationTokenSource? Timeout;
    }

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("netres");
        _netManager.Connected += NetManagerOnConnected;
        _netManager.Disconnect += OnDisconnected;
        _networkResourceManager.AckReceived += OnAckReceived;
        _prototypeLoadManager.PrototypeLoaded += OnPrototypeLoaded;
    }

    private void OnDisconnected(object? sender, NetDisconnectedArgs e)
    {
        RemoveChannelState(e.Channel);
    }

    private void OnAckReceived(INetChannel channel, int ack)
    {
        if (_pendingDumps.TryGetValue(channel, out var state) && state.AckKey == ack)
        {
            CompleteDump(channel, state);
            return;
        }

        if (ack == NetworkResourceManager.AckInitial)
            ResourcesReady(channel);
    }

    private void NetManagerOnConnected(object? sender, NetChannelArgs e)
    {
        // Resources must be sent before prototypes.
        if (!_networkResourceManager.SendToNewUser(e.Channel))
            ResourcesReady(e.Channel);
    }

    private void ResourcesReady(INetChannel channel)
    {
        if (_pendingDumps.ContainsKey(channel))
            return;

        if (!_playerManager.TryGetSessionByChannel(channel, out var session))
        {
            _sawmill.Error($"Resources ready for {channel.RemoteEndPoint} with no session; disconnecting.");
            channel.Disconnect("No session for resources.");
            return;
        }

        if (session is CommonSession { InitialResourcesDone: true })
            return;

        var ackKey = _nextDumpKey++;
        switch (_prototypeLoadManager.SendToNewUser(channel, ackKey))
        {
            case SendDumpResult.NoData:
                _playerManager.MarkPlayerResourcesSent(channel);
                break;
            case SendDumpResult.Sent:
                // Defer live uploads while the dump is in flight.
                var state = new DumpState { AckKey = ackKey };
                _pendingDumps[channel] = state;
                ArmAckTimeout(channel, state);
                break;
            case SendDumpResult.Failed:
                // The channel is already disconnecting.
                break;
        }
    }

    private void OnPrototypeLoaded(string data)
    {
        foreach (var channel in _netManager.Channels)
        {
            if (!channel.IsHandshakeComplete)
                continue;

            if (_pendingDumps.TryGetValue(channel, out var state))
            {
                state.Deferred ??= new List<string>();
                state.Deferred.Add(data);
                continue;
            }

            if (_playerManager.TryGetSessionByChannel(channel, out var session)
                && session is CommonSession { InitialResourcesDone: false })
            {
                // The initial dump has not started yet and will include this upload in its snapshot.
                continue;
            }

            channel.SendMessage(new GamePrototypeLoadMessage { PrototypeData = data });
        }
    }

    private void CompleteDump(INetChannel channel, DumpState state)
    {
        _pendingDumps.Remove(channel);
        CancelAckTimeout(state);

        if (!_playerManager.TryGetSessionByChannel(channel, out _))
        {
            _sawmill.Error($"Prototype ack received for {channel.RemoteEndPoint} with no session; disconnecting.");
            channel.Disconnect("No session for prototype ack.");
            return;
        }

        if (state.Deferred is { Count: > 0 } deferred)
        {
            // Send live uploads that arrived during the dump as a second dump and wait for its ack.
            var ackKey = _nextDumpKey++;
            switch (_prototypeLoadManager.SendDump(channel, deferred, ackKey))
            {
                case SendDumpResult.Sent:
                    var newState = new DumpState { AckKey = ackKey };
                    _pendingDumps[channel] = newState;
                    ArmAckTimeout(channel, newState);
                    return;
                case SendDumpResult.Failed:
                    _sawmill.Error($"Failed to send deferred prototype dump to {channel.RemoteEndPoint}.");
                    return;
            }
        }

        _playerManager.MarkPlayerResourcesSent(channel);
    }

    private void OnPrototypeAckTimeout(INetChannel channel, int ackKey)
    {
        if (!_pendingDumps.TryGetValue(channel, out var state) || state.AckKey != ackKey)
            return;

        _pendingDumps.Remove(channel);
        CancelAckTimeout(state);

        if (!channel.IsConnected)
            return;

        if (!_playerManager.TryGetSessionByChannel(channel, out _))
        {
            _sawmill.Error($"Prototype ack timed out for {channel.RemoteEndPoint} with no session; disconnecting.");
            channel.Disconnect("No session for prototype ack.");
            return;
        }

        // Don't let a client enter Connected without applying the dump.
        _sawmill.Warning(
            $"Client {channel.RemoteEndPoint} did not acknowledge the prototype dump in time; disconnecting.");

        channel.Disconnect("Prototype dump acknowledgement timed out.");
    }

    private void ArmAckTimeout(INetChannel channel, DumpState state)
    {
        CancelAckTimeout(state);

        var timeoutMs = GetPrototypeAckTimeoutMs(_cfg.GetCVar(CVars.NetPrototypeDumpAckTimeout));
        var cts = new CancellationTokenSource();
        state.Timeout = cts;

        var ackKey = state.AckKey;
        Timer.Spawn(timeoutMs, () => OnPrototypeAckTimeout(channel, ackKey), cts.Token);
    }

    private static void CancelAckTimeout(DumpState state)
    {
        if (state.Timeout is not { } cts)
            return;

        state.Timeout = null;
        cts.Cancel();
        cts.Dispose();
    }

    private void RemoveChannelState(INetChannel channel)
    {
        if (_pendingDumps.Remove(channel, out var state))
            CancelAckTimeout(state);
    }

    internal static int GetPrototypeAckTimeoutMs(float seconds)
    {
        if (!float.IsFinite(seconds) || seconds <= 0f)
            return 0;

        var ms = seconds * 1000.0;
        return ms >= int.MaxValue ? int.MaxValue : (int) ms;
    }
}
