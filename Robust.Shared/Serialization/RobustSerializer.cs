using NetSerializer;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization
{
    internal abstract partial class RobustSerializer : IRobustSerializerInternal
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] protected readonly IRobustMappedStringSerializer MappedStringSerializer = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private readonly Dictionary<Type, Dictionary<string, Type?>> _cachedSerialized = new();

        private ISawmill LogSzr = default!;


        private Serializer _serializer = default!;

        private HashSet<Type> _serializableTypes = default!;

        private static Type[] AlwaysNetSerializable => new[]
        {
            typeof(Vector2i)
        };

        #region Statistics

        private readonly object _statsLock = new();

        // These stats aren't tracked correctly because the tracking code isn't thread safe. Oops!
        public long LargestObjectSerializedBytes { get; private set; }

        public Type? LargestObjectSerializedType { get; private set; }

        public long BytesSerialized { get; private set; }

        public long ObjectsSerialized { get; private set; }

        public long LargestObjectDeserializedBytes { get; private set; }

        public Type? LargestObjectDeserializedType { get; private set; }

        public long BytesDeserialized { get; private set; }

        public long ObjectsDeserialized { get; private set; }

        #endregion

        public void Initialize()
        {
            var types = _reflectionManager.FindTypesWithAttribute<NetSerializableAttribute>()
                .OrderBy(x => x.FullName, StringComparer.InvariantCulture)
                .ToList();

#if DEBUG
            // confirm only shared types are marked for serialization, no client & server only types
            foreach (var type in types)
            {
                if (type.Assembly.FullName!.Contains("Server"))
                {
                    throw new InvalidOperationException($"Type {type} is server specific but has a NetSerializableAttribute!");
                }

                if (type.Assembly.FullName.Contains("Client"))
                {
                    throw new InvalidOperationException($"Type {type} is client specific but has a NetSerializableAttribute!");
                }
            }
#endif

            LogSzr = _logManager.GetSawmill("szr");
            types.AddRange(AlwaysNetSerializable);
            types.Add(typeof(Vector2));

            MappedStringSerializer.Initialize();

            var settings = new Settings
            {
                CustomTypeSerializers = new[]
                {
                    MappedStringSerializer.TypeSerializer,
                    new Vector2Serializer(),
                    new Matrix3x2Serializer(),
                }
            };
            _serializer = new Serializer(types, settings);
            _serializableTypes = new HashSet<Type>(_serializer.GetTypeMap().Keys);
            LogSzr.Info($"Serializer Types Hash: {_serializer.GetSHA256()}");
        }

        public byte[] GetSerializableTypesHash() => Convert.FromHexString(_serializer.GetSHA256());
        public string GetSerializableTypesHashString() => _serializer.GetSHA256();

        internal void GetHashManifest(Stream stream, bool writeNewline=false)
        {
            _serializer.GetHashManifest(stream, writeNewline);
        }

        public (byte[] Hash, byte[] Package) GetStringSerializerPackage() => MappedStringSerializer.GeneratePackage();

        public Dictionary<Type, uint> GetTypeMap() => _serializer.GetTypeMap();

        public void Serialize(Stream stream, object toSerialize)
        {
            var start = StartMeasureStats(stream);
            _serializer.Serialize(stream, toSerialize);
            EndMeasureSerialize(stream, start, toSerialize.GetType());
        }

        public void SerializeDirect<T>(Stream stream, T toSerialize)
        {
            DebugTools.Assert(toSerialize == null || typeof(T) == toSerialize.GetType(),
                "Object must be of exact type specified in the generic parameter.");

            var start = StartMeasureStats(stream);
            _serializer.SerializeDirect(stream, toSerialize);
            EndMeasureSerialize(stream, start, typeof(T));
        }

        public T Deserialize<T>(Stream stream)
            => (T) Deserialize(stream);

        public void DeserializeDirect<T>(Stream stream, out T value)
        {
            var start = StartMeasureStats(stream);
            _serializer.DeserializeDirect(stream, out value);
            EndMeasureDeserialize(stream, start, typeof(T));
        }

        public object Deserialize(Stream stream)
        {
            var start = StartMeasureStats(stream);
            var result = _serializer.Deserialize(stream);
            EndMeasureDeserialize(stream, start, result.GetType());

            return result;
        }

        public bool CanSerialize(Type type)
            => _serializableTypes.Contains(type);

        /// <inheritdoc />
        public Type? FindSerializedType(Type assignableType, string serializedTypeName)
        {
            if (!_cachedSerialized.TryGetValue(assignableType, out var assigned))
            {
                assigned = new Dictionary<string, Type?>();
                _cachedSerialized[assignableType] = assigned;
            }

            if (assigned.TryGetValue(serializedTypeName, out var resolved))
                return resolved;

            var types = _reflectionManager.GetAllChildren(assignableType);
            foreach (var type in types)
            {
                var serializedAttribute = type.GetCustomAttribute<SerializedTypeAttribute>();

                if(serializedAttribute is null)
                    continue;

                if (serializedAttribute.SerializeName == serializedTypeName)
                {
                    assigned[serializedTypeName] = type;
                    return type;
                }
            }

            assigned[serializedTypeName] = null;
            return null;
        }

        private static long StartMeasureStats(Stream stream)
        {
            return stream.CanSeek ? stream.Position : 0;
        }

        private void EndMeasureDeserialize(Stream stream, long start, Type type)
        {
            lock (_statsLock)
            {
                ObjectsDeserialized += 1;

                if (stream.CanSeek)
                {
                    var end = stream.Position;
                    var byteCount = end - start;
                    BytesDeserialized += byteCount;

                    if (byteCount > LargestObjectDeserializedBytes)
                    {
                        LargestObjectDeserializedBytes = byteCount;
                        LargestObjectDeserializedType = type;
                    }
                }
            }
        }

        private void EndMeasureSerialize(Stream stream, long start, Type type)
        {
            lock (_statsLock)
            {
                ObjectsSerialized += 1;

                if (stream.CanSeek)
                {
                    var end = stream.Position;
                    var byteCount = end - start;
                    BytesSerialized += byteCount;

                    if (byteCount > LargestObjectSerializedBytes)
                    {
                        LargestObjectSerializedBytes = byteCount;
                        LargestObjectSerializedType = type;
                    }
                }
            }
        }
    }
}
