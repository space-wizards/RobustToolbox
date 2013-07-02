using System;
using System.Collections.Generic;
using Lidgren.Network;
using SS13_Shared.GO;
using SS13_Shared.Serialization;

namespace SS13_Shared
{
    [Serializable]
    public class GameState : INetSerializableType
    {
        public uint Sequence { get; private set;}
        public List<EntityState> EntityStates { get; set; }

        public GameState(uint sequence)
        {
            Sequence = sequence;
        }

        public NetOutgoingMessage GameStateUpdate(NetOutgoingMessage StateUpdateMessage)
        {
            StateUpdateMessage.Write(Sequence);
            return StateUpdateMessage;
        }

        public NetOutgoingMessage WriteDelta(NetOutgoingMessage StateDeltaMessage)
        {
            return StateDeltaMessage;
        }

        public static GameStateDelta operator -(GameState toState, GameState fromState)
        {
            return Delta(fromState, toState);
        }

        public static GameState operator + (GameState fromState, GameStateDelta delta)
        {
            return Patch(fromState, delta);
        }

        public static GameState operator + (GameStateDelta delta, GameState fromState)
        {
            return Patch(fromState, delta);
        }

        public static GameStateDelta Delta(GameState fromState, GameState toState)
        {
            var delta = new GameStateDelta();
            delta.Sequence = toState.Sequence;
            delta.Create(fromState ,toState);
            return delta;
        }

        public static GameState Patch(GameState fromState, GameStateDelta delta)
        {
            return delta.Apply(fromState);
        }

    }
}
