using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameStates;
using SS14.Client.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Network.Messages;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameStates
{
    public class ClientGameStateManager : IClientGameStateManager
    {
        private Dictionary<uint, GameState> GameStates = new Dictionary<uint, GameState>();

        [Dependency]
        private readonly IClientNetManager networkManager;
        [Dependency]
        private readonly IClientEntityManager entityManager;
        [Dependency]
        private readonly IPlayerManager playerManager;
        [Dependency]
        private readonly ISS14Serializer serializer;
        [Dependency]
        private readonly IClientNetManager netManager;

        public void Initialize()
        {
            netManager.RegisterNetMessage<MsgFullState>(MsgFullState.NAME, HandleFullStateMessage);
            netManager.RegisterNetMessage<MsgStateUpdate>(MsgStateUpdate.NAME, HandleStateUpdateMessage);
        }

        public void HandleFullStateMessage(MsgFullState message)
        {
            if (!GameStates.ContainsKey(message.State.Sequence))
            {
                AckGameState(message.State.Sequence);

                message.State.GameTime = 0;//(float)timing.CurTime.TotalSeconds;
                ApplyGameState(message.State);
            }
        }

        public void HandleStateUpdateMessage(MsgStateUpdate message)
        {
            GameStateDelta delta = message.StateDelta;

            if (GameStates.ContainsKey(delta.FromSequence))
            {
                AckGameState(delta.Sequence);

                GameState fromState = GameStates[delta.FromSequence];
                GameState newState = fromState + delta;
                newState.GameTime = 0;//(float)timing.CurTime.TotalSeconds;
                ApplyGameState(newState);

                CullOldStates(delta.FromSequence);
            }
        }

        private void AckGameState(uint sequence)
        {
            var msg = networkManager.CreateNetMessage<MsgStateAck>();
            msg.Sequence = sequence;
            networkManager.ClientSendMessage(msg);
        }

        private void ApplyGameState(GameState gameState)
        {
            GameStates[gameState.Sequence] = gameState;
            CurrentState = gameState;

            entityManager.ApplyEntityStates(CurrentState.EntityStates, CurrentState.GameTime);
            playerManager.ApplyPlayerStates(CurrentState.PlayerStates);
        }
    }
}
