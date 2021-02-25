using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Robust.Shared.IoC;

namespace Robust.Shared.Serialization.Manager
{
    public abstract class DeserializationResult
    {
        public abstract object? GetValue();
    }

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

        public override object? GetValue() => Value;
    }

    public class DeserializedDefinition<T> : DeserializationResult
    {
        public readonly T Value;
        public readonly DeserializedFieldEntry[] Mapping;
        public override object? GetValue() => Value;

        public DeserializedDefinition(T value, DeserializedFieldEntry[] mapping)
        {
            if (!IoCManager.Resolve<ISerializationManager>().HasDataDefinition(typeof(T)))
                throw new ArgumentException("Provided value was not a datadefinition", nameof(value));
            //todo validate mappings array count
            Value = value;
            Mapping = mapping;
        }

        public static object Create(object value, DeserializedFieldEntry[] mappings)
        {
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
        public readonly IList<T> Value;
        public override object? GetValue() => Value;
        public IReadOnlyList<DeserializationResult> Mappings => _mappings;
        private List<DeserializationResult> _mappings;

        public DeserializedList<T> WithList(IList<T> value)
        {
            return new DeserializedList<T>(value, _mappings);
        }

        public static object Create(IList enumerable, IList<DeserializationResult> mappings)
        {
            var type = enumerable.GetType().GetGenericArguments()[0];

        var resultType = typeof(DeserializedList<>).MakeGenericType(type);
            return Activator.CreateInstance(resultType, enumerable, mappings)!;
        }

        public DeserializedList(IList<T> value, List<DeserializationResult> mappings)
        {
            Value = value;
            _mappings = mappings;
        }
    }

    public class DeserializedDictionary<TKey, TValue> : DeserializationResult where TKey : notnull
    {
        public readonly IDictionary<TKey, TValue> Value;
        public override object? GetValue() => Value;
        public IReadOnlyDictionary<DeserializationResult, DeserializationResult> Mappings => _mappings;
        private Dictionary<DeserializationResult, DeserializationResult> _mappings;

        public DeserializedDictionary(IDictionary<TKey, TValue> value, Dictionary<DeserializationResult, DeserializationResult> mappings)
        {
            Value = value;
            _mappings = mappings;
        }

        public DeserializedDictionary<TKey, TValue> WithDict(IDictionary<TKey, TValue> value)
        {
            return new DeserializedDictionary<TKey, TValue>(value, _mappings);
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

    public class DeserializedSet<T> : DeserializationResult
    {
        public readonly ISet<T> Value;
        public override object? GetValue() => Value;
        public IReadOnlyList<DeserializationResult> Mappings => _mappings;
        private List<DeserializationResult> _mappings;

        public DeserializedSet<T> WithSet(ISet<T> value)
        {
            return new DeserializedSet<T>(value, _mappings);
        }

        public DeserializedSet(ISet<T> value, List<DeserializationResult> mappings)
        {
            Value = value;
            _mappings = mappings;
        }
    }
}
