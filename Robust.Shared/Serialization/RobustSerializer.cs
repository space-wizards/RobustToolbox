using NetSerializer;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Lidgren.Network;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Log;

namespace Robust.Shared.Serialization
{

    public partial class RobustSerializer : IRobustSerializer
    {

        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly INetManager _netManager = default!;

        private readonly Lazy<ISawmill> _lazyLogSzr = new Lazy<ISawmill>(() => Logger.GetSawmill("szr"));

        private ISawmill LogSzr => _lazyLogSzr.Value;


        private Serializer _serializer = default!;

        private HashSet<Type> _serializableTypes = default!;

        private readonly RobustMappedStringSerializer _mappedStringSerializer = new RobustMappedStringSerializer();

        #region Statistics

        public static long LargestObjectSerializedBytes { get; private set; }

        public static Type? LargestObjectSerializedType { get; private set; }

        public static long BytesSerialized { get; private set; }

        public static long ObjectsSerialized { get; private set; }

        public static long LargestObjectDeserializedBytes { get; private set; }

        public static Type? LargestObjectDeserializedType { get; private set; }

        public static long BytesDeserialized { get; private set; }

        public static long ObjectsDeserialized { get; private set; }

        #endregion

        public void Initialize()
        {
            IoCManager.RegisterInstance<IRobustMappedStringSerializer>(_mappedStringSerializer);

            var types = _reflectionManager.FindTypesWithAttribute<NetSerializableAttribute>().ToList();
#if !FULL_RELEASE
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

            var settings = new Settings
            {
                CustomTypeSerializers = new ITypeSerializer[] {_mappedStringSerializer}
            };
            _serializer = new Serializer(types, settings);
            _serializableTypes = new HashSet<Type>(_serializer.GetTypeMap().Keys);
            LogSzr.Info($"Serializer Types Hash: {_serializer.GetSHA256()}");

            if (_netManager.IsClient)
            {
                _mappedStringSerializer.LockMappedStrings = true;
            }
            else
            {
                var defaultAssemblies = AssemblyLoadContext.Default.Assemblies;
                var gameAssemblies = _reflectionManager.Assemblies;
                var robustShared = defaultAssemblies
                    .First(a => a.GetName().Name == "Robust.Shared");
                _mappedStringSerializer.AddStrings(robustShared);

                // TODO: Need to add a GetSharedAssemblies method to the reflection manager

                var contentShared = gameAssemblies
                    .FirstOrDefault(a => a.GetName().Name == "Content.Shared");
                if (contentShared != null)
                {
                    _mappedStringSerializer.AddStrings(contentShared);
                }

                // TODO: Need to add a GetServerAssemblies method to the reflection manager

                var contentServer = gameAssemblies
                    .FirstOrDefault(a => a.GetName().Name == "Content.Server");
                if (contentServer != null)
                {
                    _mappedStringSerializer.AddStrings(contentServer);
                }
            }

            _mappedStringSerializer.NetworkInitialize(_netManager);
        }

        public void Serialize(Stream stream, object toSerialize)
        {
            var start = stream.Position;
            _serializer.Serialize(stream, toSerialize);
            var end = stream.Position;
            var byteCount = end - start;
            BytesSerialized += byteCount;
            ++ObjectsSerialized;

            if (byteCount <= LargestObjectSerializedBytes)
            {
                return;
            }

            LargestObjectSerializedBytes = byteCount;
            LargestObjectSerializedType = toSerialize.GetType();
        }

        public T Deserialize<T>(Stream stream)
            => (T) Deserialize(stream);

        public object Deserialize(Stream stream)
        {
            var start = stream.Position;
            var result = _serializer.Deserialize(stream);
            var end = stream.Position;
            var byteCount = end - start;
            BytesDeserialized += byteCount;
            ++ObjectsDeserialized;

            if (byteCount <= LargestObjectDeserializedBytes)
            {
                return result;
            }

            LargestObjectDeserializedBytes = byteCount;
            LargestObjectDeserializedType = result.GetType();

            return result;
        }

        public bool CanSerialize(Type type)
            => _serializableTypes.Contains(type);

    }

}
