using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Upload;

namespace Robust.Server.Upload;

/// <summary>
///     Manages sending runtime-loaded prototypes from game staff to clients.
/// </summary>
public sealed class GamePrototypeLoadManager : SharedPrototypeLoadManager
{
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConGroupController _controller = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("adminbus");
    }

    public override void SendGamePrototype(string prototype)
    {
        var msg = new GamePrototypeLoadMessage { PrototypeData = prototype };
        base.LoadPrototypeData(msg);
        _netManager.ServerSendToAll(msg);
    }

    protected override void LoadPrototypeData(GamePrototypeLoadMessage message)
    {
        var player = _playerManager.GetSessionByChannel(message.MsgChannel);
        if (_controller.CanCommand(player, "loadprototype"))
        {
            base.LoadPrototypeData(message);
            _netManager.ServerSendToAll(message); // everyone load it up!
            _sawmill.Info($"Loaded adminbus prototype data from {player.Name}.");
        }
        else
        {
            message.MsgChannel.Disconnect("Sent prototype message without permission!");
        }
    }

    internal void SendToNewUser(INetChannel channel)
    {
        if (LoadedPrototypes.Count == 0)
            return;

        // Just dump all the prototypes on connect, before them missing could be an issue.
        var msg = new GamePrototypeLoadMessage
        {
            PrototypeData = string.Join("\n\n", LoadedPrototypes)
        };
        channel.SendMessage(msg);
    }
}
