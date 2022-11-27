using System;
using System.Collections.Generic;
using Robust.Shared.Collections;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Configuration
{
    /// <summary>
    /// A networked configuration manager that controls the replication of
    /// console variables between client and server.
    /// </summary>
    public interface INetConfigurationManager : IConfigurationManager
    {
        /// <summary>
        /// Sets up the networking for the config manager.
        /// </summary>
        void SetupNetworking();

        /// <summary>
        /// Get a replicated client CVar for a specific client.
        /// </summary>
        /// <typeparam name="T">CVar type.</typeparam>
        /// <param name="channel">channel of the connected client.</param>
        /// <param name="definition">The CVar.</param>
        /// <returns>Replicated CVar of the client.</returns>
        public T GetClientCVar<T>(INetChannel channel, CVarDef<T> definition) where T : notnull =>
            GetClientCVar<T>(channel, definition.Name);

        /// <summary>
        /// Get a replicated client CVar for a specific client.
        /// </summary>
        /// <typeparam name="T">CVar type.</typeparam>
        /// <param name="channel">channel of the connected client.</param>
        /// <param name="name">Name of the CVar.</param>
        /// <returns>Replicated CVar of the client.</returns>
        T GetClientCVar<T>(INetChannel channel, string name);

        /// <summary>
        /// Synchronize the CVars marked with <see cref="CVar.REPLICATED"/> with the client.
        /// This needs to be called once during the client connection.
        /// </summary>
        /// <param name="client">Client's NetChannel to sync replicated CVars with.</param>
        void SyncConnectingClient(INetChannel client);

        /// <summary>
        /// Synchronize the CVars marked with <see cref="CVar.REPLICATED"/> with the server.
        /// This needs to be called once when connecting.
        /// </summary>
        void SyncWithServer();

        /// <summary>
        /// Called every tick to process any incoming network messages.
        /// </summary>
        void TickProcessMessages();

        /// <summary>
        /// Flushes any NwCVar messages in the receive buffer.
        /// </summary>
        void FlushMessages();

        /// <summary>
        ///     Clears internal flag for <see cref="ReceivedInitialNwVars"/>.
        ///     Must be called upon disconnect.
        /// </summary>
        void ClearReceivedInitialNwVars();

        public event EventHandler ReceivedInitialNwVars;
    }

    internal interface INetConfigurationManagerInternal : INetConfigurationManager, IConfigurationManagerInternal
    {

    }

    /// <inheritdoc cref="INetConfigurationManager"/>
    internal sealed class NetConfigurationManager : ConfigurationManager, INetConfigurationManagerInternal
    {
        [Dependency] private readonly INetManager _netManager = null!;
        [Dependency] private readonly IGameTiming _timing = null!;

        private readonly Dictionary<INetChannel, Dictionary<string, object>> _replicatedCVars = new();
        private readonly List<MsgConVars> _netVarsMessages = new();

        private ISawmill _sawmill = default!;

        public event EventHandler? ReceivedInitialNwVars;
        private bool _receivedInitialNwVars;

        public override void Shutdown()
        {
            base.Shutdown();

            FlushMessages();
            _replicatedCVars.Clear();
            ReceivedInitialNwVars = null;
            _receivedInitialNwVars = false;
        }

        /// <inheritdoc />
        public void SetupNetworking()
        {
            _sawmill = Logger.GetSawmill("cfg");
            _sawmill.Level = LogLevel.Info;

            if(_isServer)
            {
                _netManager.Connected += PeerConnected;
                _netManager.Disconnect += PeerDisconnected;
            }

            _netManager.RegisterNetMessage<MsgConVars>(HandleNetVarMessage);
        }

        private void PeerConnected(object? sender, NetChannelArgs e)
        {
            _replicatedCVars.Add(e.Channel, new Dictionary<string, object>());
        }

        private void PeerDisconnected(object? sender, NetDisconnectedArgs e)
        {
            _replicatedCVars.Remove(e.Channel);
        }

        private void HandleNetVarMessage(MsgConVars message)
        {
            if (_netManager.IsClient && !_receivedInitialNwVars)
            {
                _receivedInitialNwVars = true;

                // apply the initial set immediately, so that they are available to
                // for the rest of connection building
                ApplyNetVarChange(message.MsgChannel, message.NetworkedVars, message.Tick);
                ReceivedInitialNwVars?.Invoke(this, EventArgs.Empty);
            }
            else
                _netVarsMessages.Add(message);
        }

        /// <inheritdoc />
        public void TickProcessMessages()
        {
            if (!_timing.InSimulation || _timing.InPrediction)
                return;

            // _netVarsMessages is not in any particular ordering.
            // Copy any messages to apply to a separate list so we can sort before going through it.

            ValueList<MsgConVars> toApply = default;
            for (var i = 0; i < _netVarsMessages.Count; i++)
            {
                var msg = _netVarsMessages[i];

                if (msg.Tick > _timing.CurTick)
                    continue;

                toApply.Add(msg);

                _netVarsMessages.RemoveSwap(i);
                i--;
            }

            if (toApply.Count == 0)
                return;

            toApply.Sort((a, b) => a.Tick.CompareTo(b.Tick));

            foreach (var msg in toApply)
            {
                ApplyNetVarChange(msg.MsgChannel, msg.NetworkedVars, msg.Tick);

                if(msg.Tick != default && msg.Tick < _timing.CurTick)
                    _sawmill.Warning($"{msg.MsgChannel}: Received late nwVar message ({msg.Tick} < {_timing.CurTick} ).");
            }
        }

        /// <inheritdoc />
        public void FlushMessages()
        {
            _netVarsMessages.Sort(((a, b) => a.Tick.Value.CompareTo(b.Tick.Value)));

            foreach (var msg in _netVarsMessages)
            {
                ApplyNetVarChange(msg.MsgChannel, msg.NetworkedVars, msg.Tick);
            }

            _netVarsMessages.Clear();
        }

        private void ApplyNetVarChange(
            INetChannel msgChannel,
            List<(string name, object value)> networkedVars,
            GameTick tick)
        {
            _sawmill.Debug($"{msgChannel} Handling replicated cvars...");

            if (_netManager.IsClient)
            {
                // Server sent us a CVar update.
                foreach (var (name, value) in networkedVars)
                {
                    if (!_configVars.TryGetValue(name, out var cVar))
                    {
                        _sawmill.Warning($"{msgChannel} tried to replicate an unknown CVar '{name}.'");
                        continue;
                    }

                    if ((cVar.Flags & CVar.CLIENT) != 0)
                        continue; // ignore the server specified value.

                    // Actually set the CVar
                    SetCVarInternal(name, value, tick);

                    _sawmill.Debug($"name={name}, val={value}");
                }

                return;
            }

            // Client sent us a CVar update
            if (!_replicatedCVars.TryGetValue(msgChannel, out var clientCVars))
            {
                _sawmill.Warning($"{msgChannel} tried to replicate CVars but is not in _replicatedCVars.");
                return;
            }

            using var _ = Lock.ReadGuard();

            foreach (var (name, value) in networkedVars)
            {
                if (!_configVars.TryGetValue(name, out var cVar))
                {
                    _sawmill.Warning($"{msgChannel} tried to replicate an unknown CVar '{name}.'");
                    continue;
                }

                if (!cVar.Registered)
                {
                    _sawmill.Warning($"{msgChannel} tried to replicate an unregistered CVar '{name}.'");
                    continue;
                }

                if((cVar.Flags & CVar.REPLICATED) != 0)
                {
                    clientCVars[name] = value;
                    _sawmill.Debug($"name={name}, val={value}");
                }
                else
                {
                    _sawmill.Warning($"{msgChannel} tried to replicate an un-replicated CVar '{name}.'");
                }
            }
        }

        /// <inheritdoc />
        public T GetClientCVar<T>(INetChannel channel, string name)
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
        public override void SetCVar(string name, object value)
        {
            CVar flags;
            using (Lock.ReadGuard())
            {
                if (_configVars.TryGetValue(name, out var cVar) && cVar.Registered)
                {
                    flags = cVar.Flags;
                    if (_netManager.IsClient)
                    {
                        if (_netManager.IsConnected)
                        {
                            if ((cVar.Flags & CVar.NOT_CONNECTED) != 0)
                            {
                                _sawmill.Warning($"'{name}' can only be changed when not connected to a server.");
                                return;
                            }
                        }

                        if ((cVar.Flags & CVar.SERVER) != 0)
                        {
                            _sawmill.Warning($"Only the server can change '{name}'.");
                            return;
                        }
                    }
                    else
                    {
                        if ((cVar.Flags & CVar.CLIENT) != 0)
                        {
                            _sawmill.Warning($"Only clients can change '{name}'.");
                            return;
                        }
                    }
                }
                else
                {
                    throw new InvalidConfigurationException($"Trying to set unregistered variable '{name}'");
                }
            }

            // Actually set the CVar
            base.SetCVar(name, value);

            if ((flags & CVar.REPLICATED) == 0)
                return;

            // replicate if needed
            if (_netManager.IsClient)
            {
                var msg = new MsgConVars();
                msg.Tick = _timing.CurTick;
                msg.NetworkedVars = new List<(string name, object value)>
                {
                    (name, value)
                };
                _netManager.ClientSendMessage(msg);
            }
            else // Server
            {
                var msg = new MsgConVars();
                msg.Tick = _timing.CurTick;
                msg.NetworkedVars = new List<(string name, object value)>
                {
                    (name, value)
                };
                _netManager.ServerSendToAll(msg);
            }
        }

        /// <inheritdoc />
        public void SyncConnectingClient(INetChannel client)
        {
            DebugTools.Assert(_netManager.IsConnected);
            DebugTools.Assert(_netManager.IsServer);

            _sawmill.Info($"{client}: Sending server info...");

            var msg = new MsgConVars();
            msg.Tick = _timing.CurTick;
            msg.NetworkedVars = GetReplicatedVars();
            _netManager.ServerSendMessage(msg, client);
        }

        /// <inheritdoc />
        public void SyncWithServer()
        {
            DebugTools.Assert(_netManager.IsConnected);
            DebugTools.Assert(_netManager.IsClient);

            _sawmill.Info("Sending client info...");

            var msg = new MsgConVars();
            msg.Tick = default;
            msg.NetworkedVars = GetReplicatedVars();
            _netManager.ClientSendMessage(msg);
        }

        public void ClearReceivedInitialNwVars()
        {
            _receivedInitialNwVars = false;
        }

        private List<(string name, object value)> GetReplicatedVars()
        {
            using var _ = Lock.ReadGuard();

            var nwVars = new List<(string name, object value)>();

            foreach (var cVar in _configVars.Values)
            {
                if (!cVar.Registered)
                    continue;

                if ((cVar.Flags & CVar.REPLICATED) == 0)
                    continue;

                if (_netManager.IsClient && (cVar.Flags & CVar.SERVER) != 0)
                    continue;

                nwVars.Add((cVar.Name, GetConfigVarValue(cVar)));

                _sawmill.Debug($"name={cVar.Name}, val={(cVar.Value ?? cVar.DefaultValue)}");
            }

            return nwVars;
        }
    }
}
