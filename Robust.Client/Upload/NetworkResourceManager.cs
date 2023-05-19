using Robust.Shared.Upload;

namespace Robust.Client.Upload;

public sealed class NetworkResourceManager : SharedNetworkResourceManager
{
    /// <summary>
    ///     Callback for when the server sends a new resource.
    /// </summary>
    /// <param name="msg">The network message containing the data.</param>
    protected override void ResourceUploadMsg(NetworkResourceUploadMessage msg)
    {
        ContentRoot.AddOrUpdateFile(msg.RelativePath, msg.Data);
    }
}
