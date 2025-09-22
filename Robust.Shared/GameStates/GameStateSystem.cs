using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Robust.Shared.GameStates;

public sealed class GameStateSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<NetUserId, GameTick> _lastRealTicks = new();

    public override void Initialize()
    {
        SubscribeNetworkEvent<ClientLastRealTickChanged>(OnClientLastRealTick);
    }

    private void OnClientLastRealTick(ClientLastRealTickChanged msg, EntitySessionEventArgs args)
    {
        SetLastRealTick(args.SenderSession.UserId, msg.Tick);
    }

    public GameTick GetLastRealTick(NetUserId? session)
    {
        if (_net.IsClient)
            return _timing.LastRealTick;

        return session == null ? _timing.CurTick : _lastRealTicks.GetValueOrDefault(session.Value, _timing.CurTick);
    }

    internal void SetLastRealTick(NetUserId session, GameTick tick)
    {
        if (_net.IsClient)
            return;

        _lastRealTicks[session] = tick;
    }
}
