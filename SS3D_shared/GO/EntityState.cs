using System;
using System.Collections.Generic;
using SS13_Shared.Serialization;

namespace SS13_Shared.GO
{
    [Serializable]
    public class EntityState : INetSerializableType
    {
        public EntityStateData StateData;

        public EntityState(int uid, List<ComponentState> componentStates, string templateName, string name)
        {
            SetStateData(new EntityStateData(uid, templateName, name));
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

        public EntityStateData(int uid, string templateName, string name)
        {
            Uid = uid;
            TemplateName = templateName;
            Name = name;
        }
    }
}