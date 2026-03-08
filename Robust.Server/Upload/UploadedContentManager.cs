using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Server.Upload;

/// <summary>
/// Responsible for sending uploaded content to clients when they connect.
/// </summary>
internal sealed class UploadedContentManager
{
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly GamePrototypeLoadManager _prototypeLoadManager = default!;
    [Dependency] private readonly NetworkResourceManager _networkResourceManager = default!;

    public void Initialize()
    {
        _netManager.Connected += NetManagerOnConnected;
        _networkResourceManager.AckReceived += OnAckReceived;
    }

    private void OnAckReceived(INetChannel channel, int ack)
    {
        if (ack != NetworkResourceManager.AckInitial)
            return;

        ResourcesReady(channel);
    }

    private void NetManagerOnConnected(object? sender, NetChannelArgs e)
    {
        // This just shells out to the other managers, ensuring they are ordered properly.
        // Resources must be done before prototypes.
        var sentAny = _networkResourceManager.SendToNewUser(e.Channel);
        if (!sentAny)
            ResourcesReady(e.Channel);
    }

    private void ResourcesReady(INetChannel channel)
    {
        _prototypeLoadManager.SendToNewUser(channel);
        _playerManager.MarkPlayerResourcesSent(channel);
    }
}
