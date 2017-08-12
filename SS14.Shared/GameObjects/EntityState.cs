using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace SS14.Shared.GameObjects
{
    [Serializable]
    public class EntityState : INetSerializableType
    {
        public EntityStateData StateData;
        [NonSerialized]
        public float ReceivedTime;

        public EntityState(int uid, List<ComponentState> componentStates, string templateName, string name, List<Tuple<uint, string>> synchedComponentTypes )
        {
            SetStateData(new EntityStateData(uid, templateName, name, synchedComponentTypes));
            ComponentStates = componentStates;
        }

        public List<ComponentState> ComponentStates { get; private set; }

        public void SetStateData(EntityStateData data)
        {
            StateData = data;
        }
    }

    [Serializable]
    public struct EntityStateData : INetSerializableType
    {
        public string Name;
        public string TemplateName;
        public int Uid;
        public List<Tuple<uint, string>> SynchedComponentTypes;

        public EntityStateData(int uid, string templateName, string name, List<Tuple<uint, string>> synchedComponentTypes)
        {
            Uid = uid;
            TemplateName = templateName;
            Name = name;
            SynchedComponentTypes = synchedComponentTypes;
        }
    }
}
