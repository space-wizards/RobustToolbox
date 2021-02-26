using System;
using System.Collections;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedDictionary<TDict, TKey, TValue> :
        DeserializationResult<TDict>
        where TKey : notnull
        where TDict : IReadOnlyDictionary<TKey, TValue>
    {
        public DeserializedDictionary(
            TDict value,
            IReadOnlyDictionary<DeserializationResult, DeserializationResult> mappings)
        {
            Value = value;
            Mappings = mappings;
        }

        public override TDict Value { get; }

        public IReadOnlyDictionary<DeserializationResult, DeserializationResult> Mappings { get; }

        public override object? RawValue => Value;

        public static object Create(
            IDictionary dict,
            IDictionary<DeserializationResult, DeserializationResult> mappings)
        {
            var dictType = dict.GetType();
            var keyType = dict.Keys.GetType().GetGenericArguments()[0];
            var valType = dict.Values.GetType().GetGenericArguments()[0];

            var resultType = typeof(DeserializedDictionary<,,>).MakeGenericType(dictType, keyType, valType);

            return Activator.CreateInstance(resultType, dict, mappings)!;
        }
    }
}
