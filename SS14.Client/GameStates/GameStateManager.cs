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
        public Dictionary<uint, GameState> GameStates;

        private List<uint> AckedGameStates;

        public GameState CurrentState
        {
            get
            {
                return GameStates.Values.Last();
            }
        }

        public GameStateManager()
        {
            GameStates = new Dictionary<uint, GameState>();
            AckedGameStates = new List<uint>();
        }

        public void HandleFullStateMessage(MsgFullState message)
        {
            if (!GameStates.Keys.Contains(message.State.Sequence))
            {
                message.State.GameTime = (float)IoCManager.Resolve<IGameTiming>().CurTime.TotalSeconds;
                ApplyFullGameState(message.State);
            }
        }

        public void HandleStateUpdateMessage(MsgStateUpdate message)
        {
            if (GameStates.ContainsKey(message.StateDelta.FromSequence))
            {
                ApplyDeltaGameState(message.StateDelta);
            }
        }

        private void CullOldStates(uint sequence)
        {
            foreach (uint v in GameStates.Keys.Where(v => v < sequence).ToList())
                GameStates.Remove(v);
        }

        private void AckGameState(uint sequence)
        {
            IClientNetManager networkManager = IoCManager.Resolve<IClientNetManager>();
            NetOutgoingMessage ack = networkManager.CreateMessage();
            ack.Write((byte)NetMessages.StateAck);
            ack.Write(sequence);
            networkManager.ClientSendMessage(ack, NetDeliveryMethod.ReliableUnordered);

            AckedGameStates.Add(sequence);
        }

        private void ApplyFullGameState(GameState gameState)
        {
            GameStates[gameState.Sequence] = gameState;
            AckGameState(gameState.Sequence);

            IoCManager.Resolve<IClientEntityManager>().ApplyEntityStates(CurrentState.EntityStates, CurrentState.GameTime);
            IoCManager.Resolve<IPlayerManager>().ApplyPlayerStates(CurrentState.PlayerStates);
        }

        private void ApplyDeltaGameState(GameStateDelta delta)
        {
            AckGameState(delta.Sequence);

            GameState fromState = GameStates[delta.FromSequence];
            GameState newState = fromState + delta;
            newState.GameTime = (float)IoCManager.Resolve<IGameTiming>().CurTime.TotalSeconds;

            GameStates[delta.Sequence] = newState;

            IoCManager.Resolve<IClientEntityManager>().ApplyEntityStates(CurrentState.EntityStates, CurrentState.GameTime);
            IoCManager.Resolve<IPlayerManager>().ApplyPlayerStates(CurrentState.PlayerStates);

            CullOldStates(delta.FromSequence);
        }
    }
}