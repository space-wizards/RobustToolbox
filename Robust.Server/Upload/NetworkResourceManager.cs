using System;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Upload;
using Robust.Shared.ViewVariables;

namespace Robust.Server.Upload;

public sealed class NetworkResourceManager : SharedNetworkResourceManager
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerNetManager _serverNetManager = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IConGroupController _controller = default!;

    public event Action<ICommonSession, NetworkResourceUploadMessage>? OnResourceUploaded;

    [ViewVariables] public bool Enabled { get; private set; } = true;
    [ViewVariables] public float SizeLimit { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        _cfgManager.OnValueChanged(CVars.ResourceUploadingEnabled, value => Enabled = value, true);
        _cfgManager.OnValueChanged(CVars.ResourceUploadingLimitMb, value => SizeLimit = value, true);
    }

    /// <summary>
    ///     Callback for when a client attempts to upload a resource.
    /// </summary>
    /// <param name="msg"></param>
    /// <exception cref="NotImplementedException"></exception>
    protected override void ResourceUploadMsg(NetworkResourceUploadMessage msg)
    {
        // Do not allow uploading any new resources if it has been disabled.
        // Note: Any resources uploaded before being disabled will still be kept and sent.
        if (!Enabled)
            return;

        if (!_playerManager.TryGetSessionByChannel(msg.MsgChannel, out var session))
            return;

        if (!_controller.CanCommand(session, "uploadfile"))
            return;

        // Ensure the data is under the current size limit, if it's currently enabled.
        if (SizeLimit > 0f && msg.Data.Length * BytesToMegabytes > SizeLimit)
            return;

        base.ResourceUploadMsg(msg);

        // Now we broadcast the message!
        foreach (var channel in _serverNetManager.Channels)
        {
            channel.SendMessage(msg);
        }

        OnResourceUploaded?.Invoke(session, msg);
    }

    internal void SendToNewUser(INetChannel channel)
    {
        foreach (var (path, data) in ContentRoot.GetAllFiles())
        {
            var msg = new NetworkResourceUploadMessage();
            msg.RelativePath = path;
            msg.Data = data;
            channel.SendMessage(msg);
        }
    }
}
