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

#region Statistics
        public static long LargestObjectSerializedBytes { get; private set; }
        public static Type LargestObjectSerializedType { get; private set; }

        public static long BytesSerialized { get; private set; }

        public static long ObjectsSerialized { get; private set; }

        public static long LargestObjectDeserializedBytes { get; private set; }
        public static Type LargestObjectDeserializedType { get; private set; }

        public static long BytesDeserialized { get; private set; }

        public static long ObjectsDeserialized { get; private set; }
#endregion

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
            var start = stream.Position;
            Serializer.Serialize(stream, toSerialize);
            var end = stream.Position;
            var byteCount = end - start;
            BytesSerialized += byteCount;
            ++ObjectsSerialized;

            if (byteCount > LargestObjectSerializedBytes)
            {
                LargestObjectSerializedBytes = byteCount;
                LargestObjectSerializedType = toSerialize.GetType();
            }
        }

        public T Deserialize<T>(Stream stream)
        {
            return (T) Deserialize(stream);
        }

        public object Deserialize(Stream stream)
        {
            var start = stream.Position;
            var result = Serializer.Deserialize(stream);
            var end = stream.Position;
            var byteCount = end - start;
            BytesDeserialized += byteCount;
            ++ObjectsDeserialized;

            if (byteCount > LargestObjectDeserializedBytes)
            {
                LargestObjectDeserializedBytes = byteCount;
                LargestObjectDeserializedType = result.GetType();
            }

            return result;
        }

        public bool CanSerialize(Type type)
        {
            return SerializableTypes.Contains(type);
        }

    }

}
