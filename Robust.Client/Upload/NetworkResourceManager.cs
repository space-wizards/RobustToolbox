using Robust.Shared.IoC;
using Robust.Shared.Upload;

namespace Robust.Client.Upload;

public sealed class NetworkResourceManager : SharedNetworkResourceManager
{
    [Dependency] private readonly IBaseClient _client = default!;

    public override void Initialize()
    {
        base.Initialize();
        _client.RunLevelChanged += OnLevelChanged;
    }

    private void OnLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        // Clear networked resources when disconnecting from a multiplayer game.
        if (e.OldLevel == ClientRunLevel.InGame)
            ClearResources();
    }

    /// <summary>
    ///     Callback for when the server sends a new resource.
    /// </summary>
    /// <param name="msg">The network message containing the data.</param>
    protected override void ResourceUploadMsg(NetworkResourceUploadMessage msg)
    {
        ContentRoot.AddOrUpdateFile(msg.RelativePath, msg.Data);
    }

    /// <summary>
    ///     Clears all the networked resources. If used while connected to a server, this will probably cause issues.
    /// </summary>
    public void ClearResources()
    {
        ContentRoot.Clear();
    }
}
