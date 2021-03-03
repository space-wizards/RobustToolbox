using System.Collections.Generic;
using System.Collections.Immutable;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedImmutableList<T> : DeserializationResult<ImmutableList<T>>
    {
        public DeserializedImmutableList(ImmutableList<T>? value, IEnumerable<DeserializationResult> mappings)
        {
            Value = value;
            Mappings = mappings;
        }

        public override ImmutableList<T>? Value { get; }

        public IEnumerable<DeserializationResult> Mappings { get; }

        public override object? RawValue => Value;

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            var sourceCollection = source.Cast<DeserializedImmutableSet<T>>();
            var valueList = ImmutableList.CreateBuilder<T>();
            var resList = new List<DeserializationResult>();

            if (sourceCollection.Value != null)
            {
                foreach (var oldRes in sourceCollection.Mappings)
                {
                    var newRes = oldRes.Copy().Cast<DeserializationResult<T>>();
                    valueList.Add(newRes.Value!);
                    resList.Add(newRes);
                }
            }

            if (Value != null)
            {
                foreach (var oldRes in Mappings)
                {
                    var newRes = oldRes.Copy().Cast<DeserializationResult<T>>();
                    valueList.Add(newRes.Value);
                    resList.Add(newRes);
                }
            }

            return new DeserializedImmutableList<T>(valueList.ToImmutable(), resList);
        }

        public override DeserializationResult Copy()
        {
            var valueList = ImmutableList.CreateBuilder<T>();
            var resList = new List<DeserializationResult>();

            foreach (var oldRes in Mappings)
            {
                var newRes = oldRes.Copy().Cast<DeserializationResult<T>>();
                valueList.Add(newRes.Value!);
                resList.Add(newRes);
            }

            return new DeserializedImmutableList<T>(Value == null ? null : valueList.ToImmutable(), resList);
        }

        public override void CallAfterDeserializationHook()
        {
            foreach (var elem in Mappings)
            {
                elem.CallAfterDeserializationHook();
            }
        }
    }
}
