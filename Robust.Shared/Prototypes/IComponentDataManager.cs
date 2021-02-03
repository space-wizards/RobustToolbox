using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes
{
    public interface IComponentDataManager
    {
        DataClass ParseComponentData(string compName, YamlMappingNode mapping, YamlObjectSerializer.Context? context = null);

        YamlMappingNode? SerializeNonDefaultComponentData(IComponent comp, YamlObjectSerializer.Context? context = null);

        IYamlFieldDefinition[] GetComponentDataDefinition(string compName);

        DataClass GetEmptyComponentData(string compName);

        void PopulateComponent(IComponent comp, DataClass values);

        void PushInheritance(string compName, DataClass source, DataClass target);

        void RegisterCustomDataClasses();
    }
}
