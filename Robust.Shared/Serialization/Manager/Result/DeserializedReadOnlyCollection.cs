using System.Collections.Generic;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedReadOnlyCollection<T, E> : DeserializationResult<T> where T : IReadOnlyCollection<E>
    {
        public delegate T Create(List<E> elements);

        public DeserializedReadOnlyCollection(
            T? value,
            IEnumerable<DeserializationResult> mappings,
            Create createDelegate)
        {
            Value = value;
            Mappings = mappings;
            CreateDelegate = createDelegate;
        }

        public override T? Value { get; }

        public IEnumerable<DeserializationResult> Mappings { get; }

        public override object? RawValue => Value;

        private Create CreateDelegate { get; }

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            var sourceCollection = source.Cast<DeserializedReadOnlyCollection<T, E>>();
            var valueList = new List<E>();
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

            return new DeserializedReadOnlyCollection<T, E>(CreateDelegate(valueList), resList, CreateDelegate);
        }

        public override DeserializationResult Copy()
        {
            var valueList = new List<E>();
            var resList = new List<DeserializationResult>();

            foreach (var oldRes in Mappings)
            {
                var newRes = oldRes.Copy().Cast<DeserializationResult<E>>();
                valueList.Add(newRes.Value);
                resList.Add(newRes);
            }

            return new DeserializedReadOnlyCollection<T, E>(Value == null ? default : CreateDelegate(valueList), resList, CreateDelegate);
        }
    }
}
