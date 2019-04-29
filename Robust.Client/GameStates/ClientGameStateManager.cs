using Robust.Client.Interfaces;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameStates;
using Robust.Shared.GameStates;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Timing;

namespace Robust.Client.GameStates
{
    /// <inheritdoc />
    public class ClientGameStateManager : IClientGameStateManager
    {
        private GameStateProcessor _processor;
        
        [Dependency] private readonly IClientEntityManager _entities;
        [Dependency] private readonly IPlayerManager _players;
        [Dependency] private readonly IClientNetManager _network;
        [Dependency] private readonly IBaseClient _client;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IGameTiming _timing;
        [Dependency] private readonly IConfigurationManager _config;

        /// <inheritdoc />
        public int MinBufferSize => _processor.MinBufferSize;

        /// <inheritdoc />
        public int TargetBufferSize => _processor.TargetBufferSize;

        /// <inheritdoc />
        public void Initialize()
        {
            _processor = new GameStateProcessor(_timing);

            _network.RegisterNetMessage<MsgState>(MsgState.NAME, HandleStateMessage);
            _network.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME);
            _client.RunLevelChanged += RunLevelChanged;

            if(!_config.IsCVarRegistered("net.interp"))
                _config.RegisterCVar("net.interp", false, CVar.ARCHIVE, b => _processor.Interpolation = b);

            if (!_config.IsCVarRegistered("net.interp_ratio"))
                _config.RegisterCVar("net.interp_ratio", 0, CVar.ARCHIVE, i => _processor.InterpRatio = i);

            if (!_config.IsCVarRegistered("net.logging"))
                _config.RegisterCVar("net.logging", false, CVar.ARCHIVE, b => _processor.Logging = b);

            _processor.Interpolation = _config.GetCVar<bool>("net.interp");
            _processor.InterpRatio = _config.GetCVar<int>("net.interp_ratio");
            _processor.Logging = _config.GetCVar<bool>("net.logging");
        }

        /// <inheritdoc />
        public void Reset()
        {
            _processor.Reset();
        }

        private void RunLevelChanged(object sender, RunLevelChangedEventArgs args)
        {
            if (args.NewLevel == ClientRunLevel.Initialize)
            {
                // We JUST left a server or the client started up, Reset everything.
                Reset();
            }
        }

        private void HandleStateMessage(MsgState message)
        {
            var state = message.State;

            _processor.AddNewState(state, message.MsgSize);

            // we always ack everything we receive, even if it is late
            AckGameState(state.ToSequence);
        }
        
        /// <inheritdoc />
        public void ApplyGameState()
        {
            if (!_processor.TryCalculateStates(_timing.CurTick, out var curState, out var nextState))
                return;

            ApplyGameState(curState, nextState);
        }

        private void AckGameState(GameTick sequence)
        {
            var msg = _network.CreateNetMessage<MsgStateAck>();
            msg.Sequence = sequence;
            _network.ClientSendMessage(msg);
        }

        private void ApplyGameState(GameState curState, GameState nextState)
        {
            _mapManager.ApplyGameStatePre(curState.MapData);
            _entities.ApplyEntityStates(curState.EntityStates, curState.EntityDeletions, nextState?.EntityStates);
            _players.ApplyPlayerStates(curState.PlayerStates);
            _mapManager.ApplyGameStatePost(curState.MapData);
        }
    }
}
