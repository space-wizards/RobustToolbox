using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Server.Upload;

/// <summary>
/// Responsible for sending uploaded content to clients when they connect.
/// </summary>
internal sealed class UploadedContentManager
{
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly GamePrototypeLoadManager _prototypeLoadManager = default!;
    [Dependency] private readonly NetworkResourceManager _networkResourceManager = default!;

    public void Initialize()
    {
        _netManager.Connected += NetManagerOnConnected;
    }

    private void NetManagerOnConnected(object? sender, NetChannelArgs e)
    {
        // This just shells out to the other managers, ensuring they are ordered properly.
        // Resources must be done before prototypes.
        // Note: both net messages sent here are on the same group and are therefore ordered.
        _networkResourceManager.SendToNewUser(e.Channel);
        _prototypeLoadManager.SendToNewUser(e.Channel);
    }
}
