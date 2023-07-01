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
        /// Gets the list of networked cvars that need to be sent to when connecting to server or client.
        /// </summary>
        /// <param name="all">If true, includes all replicated cvars. I.e., clients would include cvars that were
        /// received from the server, instead of only the ones that need to be sent to the server.</param>
        /// <returns></returns>
        List<(string name, object value)> GetReplicatedVars(bool all = false);

        /// <summary>
        /// Called every tick to process any incoming network messages.
        /// </summary>
        void TickProcessMessages();

        /// <summary>
        /// Flushes any NwCVar messages in the receive buffer.
        /// </summary>
        void FlushMessages();

        /// <summary>
        /// Get a replicated client CVar for a specific client. When used client-side, this simply returns the local cvar.
        /// </summary>
        /// <typeparam name="T">CVar type.</typeparam>
        /// <param name="channel">channel of the connected client.</param>
        /// <param name="name">Name of the CVar.</param>
        /// <returns>Replicated CVar of the client.</returns>
        T GetClientCVar<T>(INetChannel channel, string name);

        /// <summary>
        /// Get a replicated client CVar for a specific client.
        /// </summary>
        /// <typeparam name="T">CVar type.</typeparam>
        /// <param name="channel">channel of the connected client.</param>
        /// <param name="definition">The CVar.</param>
        /// <returns>Replicated CVar of the client.</returns>
        T GetClientCVar<T>(INetChannel channel, CVarDef<T> definition) where T : notnull
         => GetClientCVar<T>(channel, definition.Name);
    }

    internal interface INetConfigurationManagerInternal : INetConfigurationManager, IConfigurationManagerInternal
    {

    }

    /// <inheritdoc cref="INetConfigurationManager"/>
    internal abstract class NetConfigurationManager : ConfigurationManager, INetConfigurationManagerInternal
    {
        [Dependency] protected readonly INetManager NetManager = null!;
        [Dependency] protected readonly IGameTiming Timing = null!;

        private readonly List<MsgConVars> _netVarsMessages = new();

        protected ISawmill Sawmill = default!;

        public override void Shutdown()
        {
            base.Shutdown();
            FlushMessages();
        }

        /// <inheritdoc />
        public virtual void SetupNetworking()
        {
            Sawmill = Logger.GetSawmill("cfg");
            Sawmill.Level = LogLevel.Info;
            NetManager.RegisterNetMessage<MsgConVars>(HandleNetVarMessage);
        }

        protected virtual void HandleNetVarMessage(MsgConVars message)
        {
            _netVarsMessages.Add(message);
        }

        /// <inheritdoc />
        public void TickProcessMessages()
        {
            if (!Timing.InSimulation || Timing.InPrediction)
                return;

            // _netVarsMessages is not in any particular ordering.
            // Copy any messages to apply to a separate list so we can sort before going through it.

            ValueList<MsgConVars> toApply = default;
            for (var i = 0; i < _netVarsMessages.Count; i++)
            {
                var msg = _netVarsMessages[i];

                if (msg.Tick > Timing.CurTick)
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

                if(msg.Tick != default && msg.Tick < Timing.CurTick)
                    Sawmill.Warning($"{msg.MsgChannel}: Received late nwVar message ({msg.Tick} < {Timing.CurTick} ).");
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

        protected abstract void ApplyNetVarChange(
            INetChannel msgChannel,
            List<(string name, object value)> networkedVars,
            GameTick tick);

        /// <inheritdoc />
        public void SyncConnectingClient(INetChannel client)
        {
            DebugTools.Assert(NetManager.IsConnected);
            DebugTools.Assert(NetManager.IsServer);

            Sawmill.Info($"{client}: Sending server info...");

            var msg = new MsgConVars();
            msg.Tick = Timing.CurTick;
            msg.NetworkedVars = GetReplicatedVars();
            NetManager.ServerSendMessage(msg, client);
        }

        public List<(string name, object value)> GetReplicatedVars(bool all = false)
        {
            using var _ = Lock.ReadGuard();

            var nwVars = new List<(string name, object value)>();

            foreach (var cVar in _configVars.Values)
            {
                if (!cVar.Registered)
                    continue;

                if ((cVar.Flags & CVar.REPLICATED) == 0)
                    continue;

                if (!all)
                {
                    if (NetManager.IsClient)
                    {
                        if ((cVar.Flags & CVar.SERVER) != 0)
                            continue;
                    }
                    else if ((cVar.Flags & CVar.CLIENT) != 0)
                        continue;
                }

                nwVars.Add((cVar.Name, GetConfigVarValue(cVar)));

                Sawmill.Debug($"name={cVar.Name}, val={(cVar.Value ?? cVar.DefaultValue)}");
            }

            return nwVars;
        }

        /// <inheritdoc />
        public abstract T GetClientCVar<T>(INetChannel channel, string name);
    }
}
