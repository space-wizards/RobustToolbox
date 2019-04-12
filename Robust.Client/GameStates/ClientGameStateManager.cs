using Robust.Shared;
using Robust.Shared.GameStates;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network.Messages;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameStates;
using Robust.Client.Player;
using Robust.Shared.Interfaces.Map;

namespace Robust.Client.GameStates
{
    public class ClientGameStateManager : IClientGameStateManager
    {
        private uint GameSequence;

        [Dependency]
        private readonly IClientNetManager networkManager;
        [Dependency]
        private readonly IClientEntityManager entityManager;
        [Dependency]
        private readonly IPlayerManager playerManager;
        [Dependency]
        private readonly IClientNetManager netManager;
        [Dependency]
        private readonly IBaseClient baseClient;
        [Dependency]
        private readonly IMapManager _mapManager;

        public void Initialize()
        {
            netManager.RegisterNetMessage<MsgState>(MsgState.NAME, HandleStateMessage);
            netManager.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME);
            baseClient.RunLevelChanged += RunLevelChanged;
        }

        private void RunLevelChanged(object sender, RunLevelChangedEventArgs args)
        {
            if (args.NewLevel == ClientRunLevel.Initialize)
            {
                GameSequence = 0;
            }
        }

        public void HandleStateMessage(MsgState message)
        {
            var state = message.State;
            if (GameSequence < state.FromSequence)
            {
                Logger.ErrorS("net.state", "Got a game state that's too new to handle!");
            }
            if (GameSequence > state.ToSequence)
            {
                Logger.WarningS("net.state", "Got a game state that's too old to handle!");
                return;
            }
            AckGameState(state.ToSequence);

            state.GameTime = 0;//(float)timing.CurTime.TotalSeconds;
            ApplyGameState(state);
        }

        private void AckGameState(uint sequence)
        {
            var msg = networkManager.CreateNetMessage<MsgStateAck>();
            msg.Sequence = sequence;
            networkManager.ClientSendMessage(msg);
            GameSequence = sequence;
        }

        private void ApplyGameState(GameState gameState)
        {
            _mapManager.ApplyGameStatePre(gameState.MapData);
            entityManager.ApplyEntityStates(gameState.EntityStates, gameState.EntityDeletions, gameState.GameTime);
            playerManager.ApplyPlayerStates(gameState.PlayerStates);
            _mapManager.ApplyGameStatePost(gameState.MapData);
        }
    }
}
