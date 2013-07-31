using System;
using System.Collections.Generic;
using SS13_Shared.Serialization;

namespace SS13_Shared.GO
{
    [Serializable]
    public class EntityState: INetSerializableType
    {
        public EntityStateData StateData;
        public List<ComponentState> ComponentStates { get; private set; }

        public EntityState(int uid, List<ComponentState> componentStates, string templateName, string name )
        {
            SetStateData(new EntityStateData(uid, templateName, name));
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
        public int Uid;
        public string TemplateName;
        public string Name;
        public EntityStateData(int uid, string templateName, string name)
        {
            Uid = uid;
            TemplateName = templateName;
            Name = name;
        }
    }
}
