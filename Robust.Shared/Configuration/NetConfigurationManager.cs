using System;
using System.Collections.Generic;
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

    /// <inheritdoc cref="INetConfigurationManager"/>
    internal class NetConfigurationManager : ConfigurationManager, INetConfigurationManager
    {
        [Dependency] private readonly INetManager _netManager = null!;
        [Dependency] private readonly IGameTiming _timing = null!;

        private readonly Dictionary<INetChannel, Dictionary<string, object>> _replicatedCVars = new();
        private readonly List<MsgConVars> _netVarsMessages = new();

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
                ApplyNetVarChange(message.MsgChannel, message.NetworkedVars);
                ReceivedInitialNwVars?.Invoke(this, EventArgs.Empty);
            }
            else
                _netVarsMessages.Add(message);
        }

        /// <inheritdoc />
        public void TickProcessMessages()
        {
            if(!_timing.InSimulation || _timing.InPrediction)
                return;

            for (var i = 0; i < _netVarsMessages.Count; i++)
            {
                var msg = _netVarsMessages[i];

                if (msg.Tick > _timing.LastRealTick)
                    continue;

                ApplyNetVarChange(msg.MsgChannel, msg.NetworkedVars);

                if(msg.Tick != default && msg.Tick < _timing.LastRealTick)
                    Logger.WarningS("cfg", $"{msg.MsgChannel}: Received late nwVar message ({msg.Tick} < {_timing.LastRealTick} ).");

                _netVarsMessages.RemoveSwap(i);
                i--;
            }
        }

        /// <inheritdoc />
        public void FlushMessages()
        {
            _netVarsMessages.Sort(((a, b) => a.Tick.Value.CompareTo(b.Tick.Value)));

            foreach (var msg in _netVarsMessages)
            {
                ApplyNetVarChange(msg.MsgChannel, msg.NetworkedVars);
            }

            _netVarsMessages.Clear();
        }

        private void ApplyNetVarChange(INetChannel msgChannel, List<(string name, object value)> networkedVars)
        {
            Logger.DebugS("cfg", $"{msgChannel} Handling replicated cvars...");

            if (_netManager.IsClient)
            {
                // Server sent us a CVar update.
                foreach (var (name, value) in networkedVars)
                {
                    // Actually set the CVar
                    base.SetCVar(name, value);
                    Logger.DebugS("cfg", $"name={name}, val={value}");
                }

                return;
            }

            // Client sent us a CVar update
            if (!_replicatedCVars.TryGetValue(msgChannel, out var clientCVars))
            {
                Logger.WarningS("cfg", $"{msgChannel} tried to replicate CVars but is not in _replicatedCVars.");
                return;
            }

            foreach (var (name, value) in networkedVars)
            {
                if (!_configVars.TryGetValue(name, out var cVar))
                {
                    Logger.WarningS("cfg", $"{msgChannel} tried to replicate an unknown CVar '{name}.'");
                    continue;
                }

                if (!cVar.Registered)
                {
                    Logger.WarningS("cfg", $"{msgChannel} tried to replicate an unregistered CVar '{name}.'");
                    continue;
                }

                if((cVar.Flags & CVar.REPLICATED) != 0)
                {
                    clientCVars[name] = value;
                    Logger.DebugS("cfg", $"name={name}, val={value}");
                }
                else
                {
                    Logger.WarningS("cfg", $"{msgChannel} tried to replicate an un-replicated CVar '{name}.'");
                }
            }
        }

        /// <inheritdoc />
        public T GetClientCVar<T>(INetChannel channel, string name)
        {
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
            if (_configVars.TryGetValue(name, out var cVar) && cVar.Registered)
            {
                if (_netManager.IsClient)
                {
                    if (_netManager.IsConnected)
                    {
                        if ((cVar.Flags & CVar.NOT_CONNECTED) != 0)
                        {
                            Logger.WarningS("cfg", $"'{name}' can only be changed when not connected to a server.");
                            return;
                        }
                    }

                    if ((cVar.Flags & CVar.SERVER) != 0)
                    {
                        Logger.WarningS("cfg", $"Only the server can change '{name}'.");
                        return;
                    }
                }
            }
            else
            {
                throw new InvalidConfigurationException($"Trying to set unregistered variable '{name}'");
            }

            // Actually set the CVar
            base.SetCVar(name, value);

            var cvar = _configVars[name];

            // replicate if needed
            if (_netManager.IsClient)
            {
                if ((cvar.Flags & CVar.REPLICATED) == 0)
                    return;

                var msg = _netManager.CreateNetMessage<MsgConVars>();
                msg.Tick = _timing.CurTick;
                msg.NetworkedVars = new List<(string name, object value)>
                {
                    (name, value)
                };
                _netManager.ClientSendMessage(msg);
            }
            else // Server
            {
                if ((cvar.Flags & CVar.REPLICATED) == 0)
                    return;

                var msg = _netManager.CreateNetMessage<MsgConVars>();
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

            Logger.InfoS("cfg", $"{client}: Sending server info...");

            var msg = _netManager.CreateNetMessage<MsgConVars>();
            msg.Tick = _timing.CurTick;
            msg.NetworkedVars = GetReplicatedVars();
            _netManager.ServerSendMessage(msg, client);
        }

        /// <inheritdoc />
        public void SyncWithServer()
        {
            DebugTools.Assert(_netManager.IsConnected);
            DebugTools.Assert(_netManager.IsClient);

            Logger.InfoS("cfg", "Sending client info...");

            var msg = _netManager.CreateNetMessage<MsgConVars>();
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
            var nwVars = new List<(string name, object value)>();

            foreach (var cVar in _configVars.Values)
            {
                if (!cVar.Registered)
                    continue;

                if ((cVar.Flags & CVar.REPLICATED) == 0)
                    continue;

                if (_netManager.IsClient && (cVar.Flags & CVar.SERVER) != 0)
                    continue;

                nwVars.Add((cVar.Name, cVar.Value ?? cVar.DefaultValue));

                Logger.DebugS("cfg", $"name={cVar.Name}, val={(cVar.Value ?? cVar.DefaultValue)}");
            }

            return nwVars;
        }
    }
}
