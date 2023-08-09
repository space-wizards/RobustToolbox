using System;
using System.Collections.Concurrent;

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
    }
}
