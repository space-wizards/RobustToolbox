using System;
using System.Collections;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedCollection<T> : DeserializationResult<T> where T : IEnumerable
    {
        public DeserializedCollection(T? value, IEnumerable<DeserializationResult> mappings)
        {
            Value = value;
            Mappings = mappings;
        }

        public override T? Value { get; }

        public IEnumerable<DeserializationResult> Mappings { get; }

        public override object? RawValue => Value;

        public static object Create(IEnumerable enumerable, IEnumerable<DeserializationResult> mappings)
        {
            var type = enumerable.GetType().GetGenericArguments()[0];
            var resultType = typeof(DeserializedCollection<>).MakeGenericType(type);

            return Activator.CreateInstance(resultType, enumerable, mappings)!;
        }
    }
}
