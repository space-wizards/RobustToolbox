using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Replays;

namespace Robust.Server.Configuration;

internal sealed class ServerNetConfigurationManager : NetConfigurationManager, IServerNetConfigurationManager
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;

    private readonly Dictionary<INetChannel, Dictionary<string, object>> _replicatedCVars = new();

    public override void SetupNetworking()
    {
        base.SetupNetworking();
        NetManager.Connected += PeerConnected;
        NetManager.Disconnect += PeerDisconnected;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _replicatedCVars.Clear();
    }

    private void PeerConnected(object? sender, NetChannelArgs e)
    {
        _replicatedCVars.Add(e.Channel, new Dictionary<string, object>());
    }

    private void PeerDisconnected(object? sender, NetDisconnectedArgs e)
    {
        _replicatedCVars.Remove(e.Channel);
    }

    /// <inheritdoc />
    public override T GetClientCVar<T>(INetChannel channel, string name)
    {
        using var _ = Lock.ReadGuard();

        if (!_configVars.TryGetValue(name, out var cVar) || !cVar.Registered)
            throw new InvalidConfigurationException($"Trying to get unregistered variable '{name}'");

        if (_replicatedCVars.TryGetValue(channel, out var clientCVars) && clientCVars.TryGetValue(name, out var value))
        {
            return (T)value;
        }

        return (T)(cVar.DefaultValue!);
    }

    /// <inheritdoc />
    public override void SetCVar(string name, object value, bool force)
    {
        CVar flags;
        using (Lock.ReadGuard())
        {
            if (!_configVars.TryGetValue(name, out var cVar) || !cVar.Registered)
                throw new InvalidConfigurationException($"Trying to set unregistered variable '{name}'");

            flags = cVar.Flags;
        }

        if (!force && (flags & CVar.CLIENT) != 0)
        {
            Sawmill.Warning($"Only clients can change the '{name}' cvar.");
            return;
        }

        // Actually set the CVar
        base.SetCVar(name, value, force);

        if ((flags & CVar.REPLICATED) == 0)
            return;

        var msg = new MsgConVars();
        msg.Tick = Timing.CurTick;
        msg.NetworkedVars = new List<(string name, object value)>{ (name, value) };

        NetManager.ServerSendToAll(msg);

        _replayRecording.RecordServerMessage(new ReplayMessage.CvarChangeMsg()
        {
            ReplicatedCvars = msg.NetworkedVars,
            TimeBase = _timing.TimeBase
        });
    }

    protected override void ApplyNetVarChange(
        INetChannel msgChannel,
        List<(string name, object value)> networkedVars,
        GameTick tick)
    {
        Sawmill.Debug($"{msgChannel} Handling replicated cvars...");

        // Client sent us a CVar update
        if (!_replicatedCVars.TryGetValue(msgChannel, out var clientCVars))
        {
            Sawmill.Warning($"{msgChannel} tried to replicate CVars but is not in _replicatedCVars.");
            return;
        }

        using var _ = Lock.ReadGuard();

        foreach (var (name, value) in networkedVars)
        {
            if (!_configVars.TryGetValue(name, out var cVar))
            {
                Sawmill.Warning($"{msgChannel} tried to replicate an unknown CVar '{name}.'");
                continue;
            }

            if (!cVar.Registered)
            {
                Sawmill.Warning($"{msgChannel} tried to replicate an unregistered CVar '{name}.'");
                continue;
            }

            if ((cVar.Flags & CVar.REPLICATED) != 0)
            {
                clientCVars[name] = value;
                Sawmill.Debug($"name={name}, val={value}");
            }
            else
            {
                Sawmill.Warning($"{msgChannel} tried to replicate an un-replicated CVar '{name}.'");
            }
        }
    }
}
