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
        private float _receivedTime;
        public List<ComponentState> ComponentStates { get; }

        public float ReceivedTime
        {
            get => _receivedTime;
            set => _receivedTime = value;
        }

        public EntityState(EntityUid uid, List<ComponentState> componentStates, string templateName, string name, List<Tuple<uint, string>> synchedComponentTypes)
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
        public EntityUid Uid { get; set; }
        public List<Tuple<uint, string>> SynchedComponentTypes { get; set; }

        public EntityStateData(EntityUid uid, string templateName, string name, List<Tuple<uint, string>> synchedComponentTypes) : this()
        {
            Uid = uid;
            TemplateName = templateName;
            Name = name;
            SynchedComponentTypes = synchedComponentTypes;
        }
    }
}
