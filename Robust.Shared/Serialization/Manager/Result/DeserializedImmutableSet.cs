using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedImmutableSet<T> : DeserializationResult<ImmutableHashSet<T>>
    {
        public DeserializedImmutableSet(ImmutableHashSet<T>? value, IEnumerable<DeserializationResult> mappings)
        {
            Value = value;
            Mappings = mappings;
        }

        public override ImmutableHashSet<T>? Value { get; }

        public IEnumerable<DeserializationResult> Mappings { get; }

        public override object? RawValue => Value;

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            var sourceCollection = source.Cast<DeserializedImmutableSet<T>>();
            var valueSet = ImmutableHashSet.CreateBuilder<T>();
            var resList = new List<DeserializationResult>();

            if (sourceCollection.Value != null)
            {
                foreach (var oldRes in sourceCollection.Mappings)
                {
                    var newRes = oldRes.Copy().Cast<DeserializationResult<T>>();
                    valueSet.Add(newRes.Value!);
                    resList.Add(newRes);
                }
            }

            if (Value != null)
            {
                foreach (var oldRes in Mappings)
                {
                    var newRes = oldRes.Copy().Cast<DeserializationResult<T>>();
                    valueSet.Add(newRes.Value!);
                    resList.Add(newRes);
                }
            }

            return new DeserializedImmutableSet<T>(valueSet.ToImmutable(), resList);
        }

        public override DeserializationResult Copy()
        {
            var valueSet = ImmutableHashSet.CreateBuilder<T>();
            var resList = new List<DeserializationResult>();

            foreach (var oldRes in Mappings)
            {
                var newRes = oldRes.Copy().Cast<DeserializationResult<T>>();
                valueSet.Add(newRes.Value!);
                resList.Add(newRes);
            }

            return new DeserializedImmutableSet<T>(Value == null ? null : valueSet.ToImmutable(), resList);
        }

        public override void CallAfterDeserializationHook()
        {
            foreach (var val in Mappings)
            {
                val.CallAfterDeserializationHook();
            }
        }
    }
}
