using System;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Upload;

namespace Robust.Server.Upload;

/// <summary>
///     Manages sending runtime-loaded prototypes from game staff to clients.
/// </summary>
public sealed partial class GamePrototypeLoadManager : SharedPrototypeLoadManager
{
    [Dependency] private IServerNetManager _netManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConGroupController _controller = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    public override void SendGamePrototype(string prototype)
    {
        var msg = new GamePrototypeLoadMessage { PrototypeData = prototype };
        if (TryLoadPrototypeData(prototype))
            _netManager.ServerSendToAll(msg);
    }

    protected override void LoadPrototypeData(GamePrototypeLoadMessage message)
    {
        var player = _playerManager.GetSessionByChannel(message.MsgChannel);
        if (_controller.CanCommand(player, "loadprototype"))
        {
            if (TryLoadPrototypeData(message.PrototypeData))
            {
                var broadcast = new GamePrototypeLoadMessage { PrototypeData = message.PrototypeData };
                _netManager.ServerSendToAll(broadcast); // everyone load it up!
                Sawmill.Info($"Loaded adminbus prototype data from {player.Name}.");
            }
        }
        else
        {
            message.MsgChannel.Disconnect("Sent prototype message without permission!");
        }
    }

    /// <returns>Whether any data was sent, and thus an ack is expected.</returns>
    internal bool SendToNewUser(INetChannel channel)
    {
        if (LoadedPrototypes.Count == 0)
            return false;

        var maxPartBytes = Math.Clamp(_cfg.GetCVar(CVars.NetPrototypeDumpPartBytes), 1024, MaxPrototypePartBytes);

        var parts = SplitPrototypeDump(LoadedPrototypes, Sawmill, maxPartBytes);

        for (var i = 0; i < parts.Count; i++)
        {
            channel.SendMessage(new GamePrototypeLoadMessage
            {
                PrototypeData = parts[i],
                IsDump = true,
                PartCount = parts.Count,
                PartIndex = i,
            });
        }

        return parts.Count > 0;
    }
}
