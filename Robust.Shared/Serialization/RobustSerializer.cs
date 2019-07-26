using NetSerializer;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Robust.Shared.Serialization
{
    public class RobustSerializer : IRobustSerializer
    {
        [Dependency]
#pragma warning disable 649
        private readonly IReflectionManager reflectionManager;
#pragma warning restore 649
        private Serializer Serializer;
        private HashSet<Type> SerializableTypes;

        public void Initialize()
        {
            var types = reflectionManager.FindTypesWithAttribute<NetSerializableAttribute>().ToList();
            #if DEBUG
            foreach (var type in types)
            {
                if (type.Assembly.FullName.Contains("Server") || type.Assembly.FullName.Contains("Client"))
                {
                    throw new InvalidOperationException($"Type {type} is server/client specific but has a NetSerializableAttribute!");
                }
            }
            #endif

            var settings = new Settings();
            Serializer = new Serializer(types, settings);
            SerializableTypes = new HashSet<Type>(Serializer.GetTypeMap().Keys);
        }

        public void Serialize(Stream stream, object toSerialize)
        {
            Serializer.Serialize(stream, toSerialize);
        }

        public T Deserialize<T>(Stream stream)
        {
            return (T)Deserialize(stream);
        }

        public object Deserialize(Stream stream)
        {
            return Serializer.Deserialize(stream);
        }

        public bool CanSerialize(Type type)
        {
            return SerializableTypes.Contains(type);
        }
    }
}
