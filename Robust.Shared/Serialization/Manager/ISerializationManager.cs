using System;
using Robust.Shared.Prototypes;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Manager
{
    public interface ISerializationManager
    {
        void Initialize();

        object Populate(Type type, YamlObjectSerializer serializer);

        YamlMappingNode? Serialize(Type type, object obj, YamlObjectSerializer.Context? context = null);

        public static string GetAutoDataClassMetadataName(Type type)
        {
            return $"{type.Namespace}.{type.Name}_AUTODATA";
        }
    }
}
