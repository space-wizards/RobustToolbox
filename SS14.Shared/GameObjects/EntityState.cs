using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace SS14.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class EntityState
    {
        public EntityStateData StateData { get; set; }
        [NonSerialized]
        public float ReceivedTime;
        public List<ComponentState> ComponentStates { get; private set; }

        public EntityState(int uid, List<ComponentState> componentStates, string templateName, string name, List<Tuple<uint, string>> synchedComponentTypes )
        {
            SetStateData(new EntityStateData(uid, templateName, name, synchedComponentTypes));
            ComponentStates = componentStates;
        }
        
        public void SetStateData(EntityStateData data)
        {
            StateData = data;
        }
    }

    [Serializable, NetSerializable]
    public struct EntityStateData
    {
        public string Name { get; set; }
        public string TemplateName { get; set; }
        public int Uid { get; set; }
        public List<Tuple<uint, string>> SynchedComponentTypes { get; set; }

        public EntityStateData(int uid, string templateName, string name, List<Tuple<uint, string>> synchedComponentTypes) : this()
        {
            Uid = uid;
            TemplateName = templateName;
            Name = name;
            SynchedComponentTypes = synchedComponentTypes;
        }
    }
}
