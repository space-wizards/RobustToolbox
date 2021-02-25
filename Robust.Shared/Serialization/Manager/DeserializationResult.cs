using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;

namespace Robust.Shared.Serialization.Manager
{
    public abstract class DeserializationResult{ }

    public class DeserializedValue<T> : DeserializationResult
    {
        public readonly T Value;

        public DeserializedValue(T value)
        {
            Value = value;
        }

        public static object Create(object value)
        {
            var type = typeof(DeserializedValue<>).MakeGenericType(value.GetType());
            return Activator.CreateInstance(type, value)!;
        }
    }

    public class DeserializedDefinition<T> : DeserializationResult
    {
        public readonly T Value;
        public readonly DeserializedFieldEntry[] Mapping;

        public DeserializedDefinition(T value, DeserializedFieldEntry[] mapping)
        {
            Value = value;
            Mapping = mapping;
        }

        public static object Create(object value, DeserializedFieldEntry[] mappings)
        {
            if (!IoCManager.Resolve<ISerializationManager>().HasDataDefinition(value.GetType()))
                throw new ArgumentException("Provided value was not a datadefinition", nameof(value));
            //todo validate mappings array count
            var type = typeof(DeserializedDefinition<>).MakeGenericType(value.GetType());
            return Activator.CreateInstance(type, value, mappings)!;
        }

        public class DeserializedFieldEntry
        {
            public readonly bool Mapped;
            public readonly DeserializationResult? Result;

            public DeserializedFieldEntry(bool mapped, DeserializationResult? result)
            {
                Mapped = mapped;
                Result = result;
            }
        }
    }

    public class DeserializedList<T> : DeserializationResult
    {
        public IReadOnlyList<T> Value => _value;
        private List<T> _value;
        public IReadOnlyList<DeserializationResult> Mappings => _mappings;
        private List<DeserializationResult> _mappings;

        public static object Create(IList enumerable, IList<DeserializationResult> mappings)
        {
            var type = enumerable.GetType().GetGenericArguments()[0];

        var resultType = typeof(DeserializedList<>).MakeGenericType(type);
            return Activator.CreateInstance(resultType, enumerable, mappings)!;
        }

        public DeserializedList(List<T> value, List<DeserializationResult> mappings)
        {
            _value = value;
            _mappings = mappings;
        }
    }

    public class DeserializedDictionary<TKey, TValue> : DeserializationResult where TKey : notnull
    {
        public IReadOnlyDictionary<TKey, TValue> Value => _value;
        private Dictionary<TKey, TValue> _value;
        public IReadOnlyDictionary<DeserializationResult, DeserializationResult> Mappings => _mappings;
        private Dictionary<DeserializationResult, DeserializationResult> _mappings;

        public DeserializedDictionary(Dictionary<TKey, TValue> value, Dictionary<DeserializationResult, DeserializationResult> mappings)
        {
            _value = value;
            _mappings = mappings;
        }

        public static object Create(IDictionary dict,
            IDictionary<DeserializationResult, DeserializationResult> mappings)
        {
            var keyType = dict.Keys.GetType().GetGenericArguments()[0];
            var valType = dict.Values.GetType().GetGenericArguments()[0];
            var resultType = typeof(DeserializedDictionary<,>).MakeGenericType(keyType, valType);
            return Activator.CreateInstance(resultType, dict, mappings)!;
        }
    }
}
