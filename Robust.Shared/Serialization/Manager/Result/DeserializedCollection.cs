using System;
using System.Collections;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedCollection<T, E> : DeserializationResult<T> where T : class, ICollection<E>, new()
    {
        public DeserializedCollection(T? value, IEnumerable<DeserializationResult> mappings)
        {
            Value = value;
            Mappings = mappings;
        }

        public override T? Value { get; }

        public IEnumerable<DeserializationResult> Mappings { get; }

        public override object? RawValue => Value;
        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            var sourceCollection = source.As<DeserializedCollection<T, E>>();

            var valueList = new T();
            var resList = new List<DeserializationResult>();
            if (sourceCollection.Value != null)
            {
                foreach (var oldRes in sourceCollection.Mappings)
                {
                    var newRes = oldRes.Copy().As<DeserializationResult<E>>();
                    valueList.Add(newRes.Value);
                    resList.Add(newRes);
                }
            }

            if (Value != null)
            {
                foreach (var oldRes in Mappings)
                {
                    var newRes = oldRes.Copy().As<DeserializationResult<E>>();
                    valueList.Add(newRes.Value);
                    resList.Add(newRes);
                }
            }

            return new DeserializedCollection<T, E>(valueList, resList);
        }

        public override DeserializationResult Copy()
        {
            var valueList = new T();
            var resList = new List<DeserializationResult>();
            foreach (var oldRes in Mappings)
            {
                var newRes = oldRes.Copy().As<DeserializationResult<E>>();
                valueList.Add(newRes.Value);
                resList.Add(newRes);
            }

            return new DeserializedCollection<T, E>(Value == null ? null : valueList, resList);
        }

        public static object Create(IEnumerable enumerable, IEnumerable<DeserializationResult> mappings)
        {
            var type = enumerable.GetType().GetGenericArguments()[0];
            var resultType = typeof(DeserializedCollection<,>).MakeGenericType(enumerable.GetType(),type);

            return Activator.CreateInstance(resultType, enumerable, mappings)!;
        }
    }
}
