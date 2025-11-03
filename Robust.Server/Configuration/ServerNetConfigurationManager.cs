using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Replays;
using System;
using Robust.Shared.Player;
using Robust.Shared.Collections;
using Robust.Server.Player;

namespace Robust.Server.Configuration;

internal sealed class ServerNetConfigurationManager : NetConfigurationManager, IServerNetConfigurationManager
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly Dictionary<INetChannel, Dictionary<string, object>> _replicatedCVars = new();

    private readonly Dictionary<string, InvokeList<ClientValueChangedDelegate>> _replicatedInvoke = new();

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

        var cVarChanges = new List<CVarChangeInfo>();
        using (Lock.ReadGuard())
        {
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
                    clientCVars.TryGetValue(name, out var oldValue);
                    cVarChanges.Add(new(name, tick, value, oldValue ?? value));
                    clientCVars[name] = value;
                    Sawmill.Debug($"name={name}, val={value}");
                }
                else
                {
                    Sawmill.Warning($"{msgChannel} tried to replicate an un-replicated CVar '{name}.'");
                }
            }
        }

        foreach (var info in cVarChanges)
        {
            InvokeClientCvarChange(info, msgChannel);
        }
    }

    private void InvokeClientCvarChange(CVarChangeInfo info, INetChannel msgChannel)
    {
        if (!_playerManager.TryGetSessionByChannel(msgChannel, out var session))
        {
            Sawmill.Error($"Got client cvar change for NetChannel {msgChannel.UserId} without session!");
            return;
        }

        if (!_replicatedInvoke.TryGetValue(info.Name, out var invokeList))
            return;

        foreach (var entry in invokeList.Entries)
        {
            try
            {
                entry.Value!.Invoke(info.NewValue, session, in info);
            }
            catch (Exception e)
            {
                Sawmill.Error($"Error while running InvokeClientCvarChange callback: {e}");
            }
        }
    }

    /// <inheritdoc />
    public override void OnClientCVarChanges<T>(string name, Action<T, ICommonSession> onValueChanged)
    {
        using (Lock.WriteGuard())
        {
            if (!_replicatedInvoke.TryGetValue(name, out var invoke))
            {
                InvokeList<ClientValueChangedDelegate> invokeList = new();
                invokeList.AddInPlace((object value, ICommonSession session, in CVarChangeInfo _) => onValueChanged((T)value, session), onValueChanged);

                _replicatedInvoke.Add(name, invokeList);
            }
            else
            {
                invoke.AddInPlace((object value, ICommonSession session, in CVarChangeInfo _) => onValueChanged((T)value, session),onValueChanged);
            }
        }
    }

    /// <inheritdoc />
    public override void UnsubClientCVarChanges<T>(string name, Action<T, ICommonSession> onValueChanged)
    {
        using (Lock.WriteGuard())
        {
            if (!_replicatedInvoke.TryGetValue(name, out var invoke))
            {
                Sawmill.Error($"Trying to unsubscribe for cvar {name} changes that dont have any subscriptions at all!");
                return;
            }

            invoke.RemoveInPlace(onValueChanged);
        }
    }
}
