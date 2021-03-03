using System;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedMutableCollection<TCollection, TElement> : DeserializationResult<TCollection>
        where TCollection : class, ICollection<TElement>, new()
    {
        public DeserializedMutableCollection(TCollection? value, IEnumerable<DeserializationResult> mappings)
        {
            Value = value;
            Mappings = mappings;
        }

        public override TCollection? Value { get; }

        public IEnumerable<DeserializationResult> Mappings { get; }

        public override object? RawValue => Value;

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            var sourceCollection = source.Cast<DeserializedMutableCollection<TCollection, TElement>>();
            var valueList = new TCollection();
            var resList = new List<DeserializationResult>();

            if (sourceCollection.Value != null)
            {
                foreach (var oldRes in sourceCollection.Mappings)
                {
                    var newRes = oldRes.Copy().Cast<DeserializationResult<TElement>>();
                    valueList.Add(newRes.Value!);
                    resList.Add(newRes);
                }
            }

            if (Value != null)
            {
                foreach (var oldRes in Mappings)
                {
                    var newRes = oldRes.Copy().Cast<DeserializationResult<TElement>>();
                    valueList.Add(newRes.Value!);
                    resList.Add(newRes);
                }
            }

            return new DeserializedMutableCollection<TCollection, TElement>(valueList, resList);
        }

        public override DeserializationResult Copy()
        {
            var valueList = new TCollection();
            var resList = new List<DeserializationResult>();
            foreach (var oldRes in Mappings)
            {
                var newRes = oldRes.Copy();
                valueList.Add((TElement) newRes.RawValue!);
                resList.Add(newRes);
            }

            return new DeserializedMutableCollection<TCollection, TElement>(Value == null ? null : valueList, resList);
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
