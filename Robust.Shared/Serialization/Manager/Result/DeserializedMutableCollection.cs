using System;
using System.Collections;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedMutableCollection<T, E> : DeserializationResult<T> where T : class, ICollection<E>, new()
    {
        public DeserializedMutableCollection(T? value, IEnumerable<DeserializationResult> mappings)
        {
            Value = value;
            Mappings = mappings;
        }

        public override T? Value { get; }

        public IEnumerable<DeserializationResult> Mappings { get; }

        public override object? RawValue => Value;

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            var sourceCollection = source.Cast<DeserializedMutableCollection<T, E>>();
            var valueList = new T();
            var resList = new List<DeserializationResult>();

            if (sourceCollection.Value != null)
            {
                foreach (var oldRes in sourceCollection.Mappings)
                {
                    var newRes = oldRes.Copy().Cast<DeserializationResult<E>>();
                    valueList.Add(newRes.Value);
                    resList.Add(newRes);
                }
            }

            if (Value != null)
            {
                foreach (var oldRes in Mappings)
                {
                    var newRes = oldRes.Copy().Cast<DeserializationResult<E>>();
                    valueList.Add(newRes.Value);
                    resList.Add(newRes);
                }
            }

            return new DeserializedMutableCollection<T, E>(valueList, resList);
        }

        public override DeserializationResult Copy()
        {
            var valueList = new T();
            var resList = new List<DeserializationResult>();
            foreach (var oldRes in Mappings)
            {
                var newRes = oldRes.Copy().Cast<DeserializationResult<E>>();
                valueList.Add(newRes.Value);
                resList.Add(newRes);
            }

            return new DeserializedMutableCollection<T, E>(Value == null ? null : valueList, resList);
        }

        public static object Create(IEnumerable enumerable, IEnumerable<DeserializationResult> mappings)
        {
            var type = enumerable.GetType().GetGenericArguments()[0];
            var resultType = typeof(DeserializedMutableCollection<,>).MakeGenericType(enumerable.GetType(), type);

            return Activator.CreateInstance(resultType, enumerable, mappings)!;
        }
    }
}
