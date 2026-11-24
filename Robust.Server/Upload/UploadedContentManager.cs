using System;
using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Upload;

namespace Robust.Server.Upload;

/// <summary>
/// Responsible for sending uploaded content to clients when they connect.
/// </summary>
internal sealed partial class UploadedContentManager
{
    [Dependency] private IServerNetManager _netManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private GamePrototypeLoadManager _prototypeLoadManager = default!;
    [Dependency] private NetworkResourceManager _networkResourceManager = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    /// <summary>
    ///     Channels that have been sent a prototype dump but have not yet acknowledged applying it.
    /// </summary>
    private readonly HashSet<INetChannel> _awaitingPrototypeAck = new();

    /// <summary>
    ///     Live prototype uploads that arrived while a channel was receiving a prototype dump.
    /// </summary>
    private readonly Dictionary<INetChannel, List<string>> _deferredPrototypes = new();

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("netres");
        _netManager.Connected += NetManagerOnConnected;
        _netManager.Disconnect += OnDisconnected;
        _networkResourceManager.AckReceived += OnAckReceived;
        _prototypeLoadManager.LoadComplete += OnPrototypeLoadComplete;
        _prototypeLoadManager.PrototypeLoaded += OnPrototypeLoaded;
    }

    private void OnDisconnected(object? sender, NetDisconnectedArgs e)
    {
        _awaitingPrototypeAck.Remove(e.Channel);
        _deferredPrototypes.Remove(e.Channel);
    }

    private void OnAckReceived(INetChannel channel, int ack)
    {
        if (ack != NetworkResourceManager.AckInitial)
            return;

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
        if (_awaitingPrototypeAck.Contains(channel))
            return;

        if (!_playerManager.TryGetSessionByChannel(channel, out var session))
        {
            _sawmill.Error($"Resources ready for {channel.RemoteEndPoint} with no session.");
            return;
        }

        if (session is CommonSession { InitialResourcesDone: true })
            return;

        if (!_prototypeLoadManager.SendToNewUser(channel))
        {
            _playerManager.MarkPlayerResourcesSent(channel);
            return;
        }

        // The dump transfer has started. Mark the channel as awaiting so live uploads that arrive
        // while it is in flight are deferred instead of racing it.
        _awaitingPrototypeAck.Add(channel);
        var timeoutMs = GetPrototypeAckTimeoutMs();
        Timer.Spawn(timeoutMs, () => OnPrototypeAckTimeout(channel, timeoutMs));
    }

    private void OnPrototypeLoaded(string data)
    {
        foreach (var channel in _netManager.Channels)
        {
            if (!channel.IsHandshakeComplete)
                continue;

            if (_awaitingPrototypeAck.Contains(channel))
            {
                if (!_deferredPrototypes.TryGetValue(channel, out var deferred))
                    _deferredPrototypes[channel] = deferred = new List<string>();

                deferred.Add(data);
            }
            else
            {
                channel.SendMessage(new GamePrototypeLoadMessage { PrototypeData = data });
            }
        }
    }

    private void OnPrototypeLoadComplete(INetChannel channel)
    {
        if (!_awaitingPrototypeAck.Remove(channel))
            return;

        if (!_playerManager.TryGetSessionByChannel(channel, out _))
        {
            _sawmill.Error($"Prototype ack received for {channel.RemoteEndPoint} with no session.");
            _deferredPrototypes.Remove(channel);
            return;
        }

        SendDeferredPrototypes(channel);
        _playerManager.MarkPlayerResourcesSent(channel);
    }

    private void OnPrototypeAckTimeout(INetChannel channel, int timeoutMs)
    {
        if (!_awaitingPrototypeAck.Remove(channel))
            return;

        if (!_playerManager.TryGetSessionByChannel(channel, out _))
        {
            _sawmill.Error($"Prototype ack timed out for {channel.RemoteEndPoint} with no session.");
            _deferredPrototypes.Remove(channel);
            return;
        }

        // a client that never acknowledges shouldn't be stuck in "connecting" forever.
        _sawmill.Warning(
            $"Client {channel.RemoteEndPoint} did not acknowledge the prototype dump within " +
            $"{timeoutMs} ms; marking resources sent without confirmation.");

        SendDeferredPrototypes(channel);
        _playerManager.MarkPlayerResourcesSent(channel);
    }

    private void SendDeferredPrototypes(INetChannel channel)
    {
        if (!_deferredPrototypes.Remove(channel, out var deferred))
            return;

        foreach (var prototype in deferred)
            channel.SendMessage(new GamePrototypeLoadMessage { PrototypeData = prototype });
    }

    private int GetPrototypeAckTimeoutMs()
    {
        var seconds = _cfg.GetCVar(CVars.NetPrototypeDumpAckTimeout);
        return (int) (Math.Max(0f, seconds) * 1000f);
    }
}
