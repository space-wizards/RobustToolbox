using System;
using System.Collections.Concurrent;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private readonly ConcurrentDictionary<Type, object> _customTypeSerializers = new();

        internal object GetOrCreateCustomTypeSerializer(Type type)
        {
            return _customTypeSerializers.GetOrAdd(type, CreateSerializer);
        }

        internal T GetOrCreateCustomTypeSerializer<T>()
        {
            return (T)GetOrCreateCustomTypeSerializer(typeof(T));
        }

        public T EnsureCustomReader<T, TType, TNode>()
            where T : ITypeReader<TType, TNode>
            where TNode : DataNode
        {
            return GetOrCreateCustomTypeSerializer<T>();
        }

        public T EnsureCustomCopier<T, TType>() where T : ITypeCopier<TType>
        {
            return GetOrCreateCustomTypeSerializer<T>();
        }

        public T EnsureCustomCopyCreator<T, TType>() where T : ITypeCopyCreator<TType>
        {
            return GetOrCreateCustomTypeSerializer<T>();
        }
    }
}
