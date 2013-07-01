using System;
using System.Collections.Generic;
using SS13_Shared.Serialization;

namespace SS13_Shared.GO
{
    [Serializable]
    public class EntityState: INetSerializableType
    {
        public int Uid { get; private set; }
        public EntityStateData StateData;

        public List<ComponentState> ComponentStates { get; private set; }

        public EntityState(int uid, List<ComponentState> componentStates )
        {
            Uid = uid;
            ComponentStates = componentStates;
        }

        public void SetStateData(EntityStateData data)
        {
            StateData = data;
        }
    }

    [Serializable]
    public struct EntityStateData : INetSerializableType
    {
        public Vector2 Position;
        public int Uid;
        public EntityStateData(int uid, Vector2 position)
        {
            Position = position;
            Uid = uid;
        }
    }
}
