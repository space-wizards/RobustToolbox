using Lidgren.Network;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameStates;
using SS14.Client.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Network.Messages;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameStates
{
    public class GameStateManager : IGameStateManager
    {
        public Dictionary<uint, GameState> GameStates { get; set; }

        //[Dependency]
        //private readonly IGameTiming timing;
        [Dependency]
        private readonly IClientNetManager networkManager;
        [Dependency]
        private readonly IClientEntityManager entityManager;
        [Dependency]
        private readonly IPlayerManager playerManager;

        public GameState CurrentState { get; private set; }

        public GameStateManager()
        {
            GameStates = new Dictionary<uint, GameState>();
        }

        #region Network

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

        #endregion Network

        private void CullOldStates(uint sequence)
        {
            foreach (uint v in GameStates.Keys.Where(v => v <= sequence).ToList())
                GameStates.Remove(v);
        }

        private void AckGameState(uint sequence)
        {
            NetOutgoingMessage ack = networkManager.CreateMessage();
            ack.Write((byte)NetMessages.StateAck);
            ack.Write(sequence);
            networkManager.ClientSendMessage(ack, NetDeliveryMethod.ReliableUnordered);
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
