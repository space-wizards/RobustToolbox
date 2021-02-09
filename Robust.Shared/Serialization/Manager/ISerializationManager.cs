using System;
using Robust.Shared.Prototypes;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Manager
{
    public interface ISerializationManager
    {
        void Initialize();

        object Populate(Type type, YamlObjectSerializer serializer);

        void PushInheritance(object source, object target);

        void Serialize(Type type, object obj, YamlObjectSerializer serializer, bool alwaysWrite = false);

        public static string GetAutoDataClassMetadataName(Type type)
        {
            return $"{type.Namespace}.{type.Name}_AUTODATA";
        }
    }
}
