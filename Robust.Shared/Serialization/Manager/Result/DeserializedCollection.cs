using System;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedCollection<TCollection, TElement> : DeserializationResult<TCollection> where TCollection : IReadOnlyCollection<TElement>
    {
        public delegate TCollection Create(List<TElement> elements);

        public DeserializedCollection(
            TCollection? value,
            IEnumerable<DeserializationResult> mappings,
            Create createDelegate)
        {
            Value = value;
            Mappings = mappings;
            CreateDelegate = createDelegate;
        }

        public override TCollection? Value { get; }

        public IEnumerable<DeserializationResult> Mappings { get; }

        public override object? RawValue => Value;

        private Create CreateDelegate { get; }

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            var sourceCollection = source.Cast<DeserializedCollection<TCollection, TElement>>();
            var valueList = new List<TElement>();
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

            return new DeserializedCollection<TCollection, TElement>(CreateDelegate(valueList), resList, CreateDelegate);
        }

        public override DeserializationResult Copy()
        {
            var valueList = new List<TElement>();
            var resList = new List<DeserializationResult>();

            foreach (var oldRes in Mappings)
            {
                var newRes = oldRes.Copy().Cast<DeserializationResult<TElement>>();
                valueList.Add(newRes.Value!);
                resList.Add(newRes);
            }

            return new DeserializedCollection<TCollection, TElement>(Value == null ? default : CreateDelegate(valueList), resList, CreateDelegate);
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
