using System.Collections.Generic;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Utility;

namespace Robust.Shared.Configuration
{
    public interface INetConfigurationManager : IConfigurationManager
    {
        /// <summary>
        /// Sets up the networking for the config manager.
        /// </summary>
        void SetupNetworking();
        T GetClientCVar<T>(INetChannel channel, string name);
        void SyncConnectingClient(INetChannel client);
        void SyncWithServer();
    }

    /// <inheritdoc cref="INetConfigurationManager"/>
    internal class NetConfigurationManager : ConfigurationManager, INetConfigurationManager
    {
        [Dependency] private readonly INetManager _netManager = null!;
        [Dependency] private readonly IGameTiming _timing = null!;

        private readonly Dictionary<INetChannel, Dictionary<string, object>> _replicatedCVars = new();

        /// <inheritdoc />
        public void SetupNetworking()
        {
            if(_netManager.IsServer)
            {
                _netManager.Connected += PeerConnected;
                _netManager.Disconnect += PeerDisconnected;
            }

            _netManager.RegisterNetMessage<MsgConVars>(MsgConVars.NAME, HandleNetVarChange);
        }

        private void PeerConnected(object? sender, NetChannelArgs e)
        {
            _replicatedCVars.Add(e.Channel, new Dictionary<string, object>());
        }

        private void PeerDisconnected(object? sender, NetDisconnectedArgs e)
        {
            _replicatedCVars.Remove(e.Channel);
        }

        private void HandleNetVarChange(MsgConVars message)
        {
            Logger.DebugS("cfg", "Handling replicated cvars...");

            foreach (var (name, value) in message.NetworkedVars)
            {
                if (_netManager.IsClient) // Server sent us a CVar update.
                {
                    // Actually set the CVar
                    base.SetCVar(name, value);
                    Logger.DebugS("cfg", $"name={name}, val={value}");
                }
                else // Client sent us a CVar update
                {
                    if (!_configVars.TryGetValue(name, out var cVar))
                    {
                        Logger.WarningS("cfg", $"{message.MsgChannel} tried to replicate an unknown CVar '{name}.'");
                        continue;
                    }

                    if (!cVar.Registered)
                    {
                        Logger.WarningS("cfg", $"{message.MsgChannel} tried to replicate an unregistered CVar '{name}.'");
                        continue;
                    }

                    if((cVar.Flags & CVar.REPLICATED) != 0)
                    {
                        var clientCVars = _replicatedCVars[message.MsgChannel];

                        if (clientCVars.ContainsKey(name))
                            clientCVars[name] = value;
                        else
                            clientCVars.Add(name, value);

                        Logger.DebugS("cfg", $"name={name}, val={value}");
                    }
                    else
                    {
                        Logger.WarningS("cfg", $"{message.MsgChannel} tried to replicate an unreplicated CVar '{name}.'");
                    }
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
            msg.Tick = _timing.CurTick;
            msg.NetworkedVars = GetReplicatedVars();
            _netManager.ClientSendMessage(msg);
        }

        private List<(string name, object value)> GetReplicatedVars()
        {
            var nwVars = new List<(string name, object value)>();

            foreach (var cVar in _configVars.Values)
            {
                if (!cVar.Registered)
                    return nwVars;

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
