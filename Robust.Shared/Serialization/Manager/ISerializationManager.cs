using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Manager
{
    public interface ISerializationManager
    {
        void Initialize();

        SerializationDataDefinition? GetDataDefinition(Type type);
        bool TryGetDataDefinition(Type type, [NotNullWhen(true)] out SerializationDataDefinition? dataDefinition);

        object Populate(Type type, ObjectSerializer serializer);

        object PushInheritance(object source, object target);

        object Copy(object source, object target);

        void Serialize(Type type, object obj, YamlObjectSerializer serializer, bool alwaysWrite = false);

        public static string GetAutoDataClassMetadataName(Type type)
        {
            return $"{type.Namespace}.{type.Name}_AUTODATA";
        }
    }
}
