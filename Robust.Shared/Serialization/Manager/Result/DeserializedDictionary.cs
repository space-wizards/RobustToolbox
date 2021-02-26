using System;
using System.Collections;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedDictionary<TKey, TValue> : DeserializationResult<IReadOnlyDictionary<TKey, TValue>> where TKey : notnull
    {
        public DeserializedDictionary(IReadOnlyDictionary<TKey, TValue> value, IReadOnlyDictionary<DeserializationResult, DeserializationResult> mappings)
        {
            Value = value;
            Mappings = mappings;
        }

        public override IReadOnlyDictionary<TKey, TValue> Value { get; }

        public IReadOnlyDictionary<DeserializationResult, DeserializationResult> Mappings { get; }

        public override object? RawValue => Value;

        public static object Create(
            IDictionary dict,
            IDictionary<DeserializationResult, DeserializationResult> mappings)
        {
            var keyType = dict.Keys.GetType().GetGenericArguments()[0];
            var valType = dict.Values.GetType().GetGenericArguments()[0];
            var resultType = typeof(DeserializedDictionary<,>).MakeGenericType(keyType, valType);

            return Activator.CreateInstance(resultType, dict, mappings)!;
        }
    }
}
