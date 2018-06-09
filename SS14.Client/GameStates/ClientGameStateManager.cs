using SS14.Client.Interfaces;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameStates;
using SS14.Client.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network.Messages;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameStates
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
            entityManager.ApplyEntityStates(gameState.EntityStates, gameState.EntityDeletions, gameState.GameTime);
            playerManager.ApplyPlayerStates(gameState.PlayerStates);
        }
    }
}
