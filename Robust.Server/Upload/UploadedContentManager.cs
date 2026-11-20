using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

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

    /// <summary>
    ///     Channels that have been sent a prototype dump but have not yet acknowledged applying it.
    /// </summary>
    private readonly HashSet<INetChannel> _awaitingPrototypeAck = new();

    /// <summary>
    ///     How long to wait for a client to acknowledge a prototype dump before marking it ready anyway.
    /// </summary>
    private const int PrototypeAckTimeoutMs = 120000; //2 minutes

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("netres");
        _netManager.Connected += NetManagerOnConnected;
        _netManager.Disconnect += OnDisconnected;
        _networkResourceManager.AckReceived += OnAckReceived;
        _prototypeLoadManager.LoadComplete += OnPrototypeLoadComplete;
    }

    private void OnDisconnected(object? sender, NetDisconnectedArgs e)
    {
        _awaitingPrototypeAck.Remove(e.Channel);
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

        /*Wait for the client to confirm it has applied the prototypes before marking the session ready.
        Otherwise the client could reach InGame and receive game states that reference prototypes it does not have yet.*/
        _awaitingPrototypeAck.Add(channel);
        Timer.Spawn(PrototypeAckTimeoutMs, () => OnPrototypeAckTimeout(channel));
    }

    private void OnPrototypeLoadComplete(INetChannel channel)
    {
        if (!_awaitingPrototypeAck.Remove(channel))
            return;

        if (!_playerManager.TryGetSessionByChannel(channel, out _))
        {
            _sawmill.Error($"Prototype ack received for {channel.RemoteEndPoint} with no session.");
            return;
        }

        _playerManager.MarkPlayerResourcesSent(channel);
    }

    private void OnPrototypeAckTimeout(INetChannel channel)
    {
        if (!_awaitingPrototypeAck.Remove(channel))
            return;

        if (!channel.IsConnected)
            return;

        if (!_playerManager.TryGetSessionByChannel(channel, out _))
        {
            _sawmill.Error($"Prototype ack timed out for {channel.RemoteEndPoint} with no session.");
            return;
        }

        // a client that never acknowledges shouldn't be stuck in "connecting" forever.
        _sawmill.Warning(
            $"Client {channel.RemoteEndPoint} did not acknowledge the prototype dump within " +
            $"{PrototypeAckTimeoutMs} ms; marking resources sent without confirmation.");
        _playerManager.MarkPlayerResourcesSent(channel);
    }
}
