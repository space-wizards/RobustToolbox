using System;
using System.Collections.Generic;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Upload;

namespace Robust.Server.Upload;

/// <summary>
///     Manages sending runtime-loaded prototypes from game staff to clients.
/// </summary>
public sealed class GamePrototypeLoadManager : IGamePrototypeLoadManager
{
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly IConGroupController _controller = default!;

    private readonly List<string> _loadedPrototypes = new();
    public IReadOnlyList<string> LoadedPrototypes => _loadedPrototypes;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<GamePrototypeLoadMessage>(ClientLoadsPrototype);
        _netManager.Connected += NetManagerOnConnected;
        _replay.OnRecordingStarted += OnStartReplayRecording;
    }

    private void OnStartReplayRecording((MappingDataNode, List<object>) initReplayData)
    {
        // replays will need information about currently loaded prototypes
        foreach (var prototype in _loadedPrototypes)
        {
            initReplayData.Item2.Add(new ReplayPrototypeUploadMsg { PrototypeData = prototype });
        }
    }

    public void SendGamePrototype(string prototype)
    {

    }

    private void ClientLoadsPrototype(GamePrototypeLoadMessage message)
    {
        var player = _playerManager.GetSessionByChannel(message.MsgChannel);
        if (_controller.CanCommand(player, "loadprototype"))
        {
            LoadPrototypeData(message.PrototypeData);
            Logger.InfoS("adminbus", $"Loaded adminbus prototype data from {player.Name}.");
        }
        else
        {
            message.MsgChannel.Disconnect("Sent prototype message without permission!");
        }
    }

    private void LoadPrototypeData(string prototypeData)
    {
        _loadedPrototypes.Add(prototypeData);

        _replay.QueueReplayMessage(new ReplayPrototypeUploadMsg { PrototypeData = prototypeData });

        var msg = new GamePrototypeLoadMessage
        {
            PrototypeData = prototypeData
        };
        _netManager.ServerSendToAll(msg); // everyone load it up!
        var changed = new Dictionary<Type, HashSet<string>>();
        _prototypeManager.LoadString(prototypeData, true, changed); // server needs it too.
        _prototypeManager.ResolveResults();
        _prototypeManager.ReloadPrototypes(changed);
        _localizationManager.ReloadLocalizations();
    }

    private void NetManagerOnConnected(object? sender, NetChannelArgs e)
    {
        // Just dump all the prototypes on connect, before them missing could be an issue.
        foreach (var prototype in _loadedPrototypes)
        {
            var msg = new GamePrototypeLoadMessage
            {
                PrototypeData = prototype
            };
            e.Channel.SendMessage(msg);
        }
    }
}
