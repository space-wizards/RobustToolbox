using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace SS14.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class EntityState
    {
        private EntityStateData stateData;
        [NonSerialized]
        private float receivedTime;

        public EntityState(int uid, List<ComponentState> componentStates, string templateName, string name, List<Tuple<uint, string>> synchedComponentTypes )
        {
            SetStateData(new EntityStateData(uid, templateName, name, synchedComponentTypes));
            ComponentStates = componentStates;
        }

        public List<ComponentState> ComponentStates { get; private set; }
        public EntityStateData StateData { get => stateData; set => stateData = value; }
        public float ReceivedTime { get => receivedTime; set => receivedTime = value; }

        public void SetStateData(EntityStateData data)
        {
            StateData = data;
        }
    }

    [Serializable, NetSerializable]
    public struct EntityStateData
    {
        private string name;
        private string templateName;
        private int uid;
        private List<Tuple<uint, string>> synchedComponentTypes;

        public string Name { get => name; set => name = value; }
        public string TemplateName { get => templateName; set => templateName = value; }
        public int Uid { get => uid; set => uid = value; }
        public List<Tuple<uint, string>> SynchedComponentTypes { get => synchedComponentTypes; set => synchedComponentTypes = value; }

        public EntityStateData(int uid, string templateName, string name, List<Tuple<uint, string>> synchedComponentTypes) : this()
        {
            Uid = uid;
            TemplateName = templateName;
            Name = name;
            SynchedComponentTypes = synchedComponentTypes;
        }
    }
}
