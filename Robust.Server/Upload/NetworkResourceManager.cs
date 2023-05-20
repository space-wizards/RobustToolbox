using System;
using System.Collections.Generic;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Upload;
using Robust.Shared.ViewVariables;

namespace Robust.Server.Upload;

public sealed class NetworkResourceManager : SharedNetworkResourceManager
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerNetManager _serverNetManager = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IConGroupController _controller = default!;

    public event Action<IPlayerSession, NetworkResourceUploadMessage>? OnResourceUploaded;

    [ViewVariables] public bool Enabled { get; private set; } = true;
    [ViewVariables] public float SizeLimit { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        _serverNetManager.Connected += ServerNetManagerOnConnected;
        _cfgManager.OnValueChanged(CVars.ResourceUploadingEnabled, value => Enabled = value, true);
        _cfgManager.OnValueChanged(CVars.ResourceUploadingLimitMb, value => SizeLimit = value, true);
        _replay.OnRecordingStarted += OnStartReplayRecording;
    }

    private void OnStartReplayRecording((MappingDataNode, List<object>) initReplayData)
    {
        // replays will need information about currently loaded extra resources
        foreach (var (path, data) in ContentRoot.GetAllFiles())
        {
            initReplayData.Item2.Add(new ReplayResourceUploadMsg { RelativePath = path, Data = data });
        }
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

        ContentRoot.AddOrUpdateFile(msg.RelativePath, msg.Data);

        // Now we broadcast the message!
        foreach (var channel in _serverNetManager.Channels)
        {
            channel.SendMessage(msg);
        }

        _replay.QueueReplayMessage(new ReplayResourceUploadMsg { RelativePath = msg.RelativePath, Data = msg.Data });
        OnResourceUploaded?.Invoke(session, msg);
    }

    private void ServerNetManagerOnConnected(object? sender, NetChannelArgs e)
    {
        foreach (var (path, data) in ContentRoot.GetAllFiles())
        {
            var msg = new NetworkResourceUploadMessage();
            msg.RelativePath = path;
            msg.Data = data;
            e.Channel.SendMessage(msg);
        }
    }
}
