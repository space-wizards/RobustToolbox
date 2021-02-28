using System.Collections.Generic;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedDictionary<TDict, TKey, TValue> :
        DeserializationResult<TDict>
        where TKey : notnull
        where TDict : IDictionary<TKey, TValue>, new()
    {
        public DeserializedDictionary(
            TDict? value,
            IReadOnlyDictionary<DeserializationResult, DeserializationResult> mappings)
        {
            Value = value;
            Mappings = mappings;
        }

        public override TDict? Value { get; }

        public IReadOnlyDictionary<DeserializationResult, DeserializationResult> Mappings { get; }

        public override object? RawValue => Value;

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            var sourceRes = source.Cast<DeserializedDictionary<TDict, TKey, TValue>>();
            var valueDict = new TDict();
            var mappingDict = new Dictionary<DeserializationResult, DeserializationResult>();

            foreach (var (keyRes, valRes) in sourceRes.Mappings)
            {
                var newKeyRes = keyRes.Copy().Cast<DeserializationResult<TKey>>();
                var newValueRes = valRes.Copy().Cast<DeserializationResult<TValue>>();

                valueDict.Add(newKeyRes.Value, newValueRes.Value);
                mappingDict.Add(newKeyRes, newValueRes);
            }

            foreach (var (keyRes, valRes) in Mappings)
            {
                var newKeyRes = keyRes.Copy().Cast<DeserializationResult<TKey>>();
                var newValueRes = valRes.Copy().Cast<DeserializationResult<TValue>>();

                valueDict.Add(newKeyRes.Value, newValueRes.Value);
                mappingDict.Add(newKeyRes, newValueRes);
            }

            return new DeserializedDictionary<TDict, TKey, TValue>(valueDict, mappingDict);
        }

        public override DeserializationResult Copy()
        {
            var valueDict = new TDict();
            var mappingDict = new Dictionary<DeserializationResult, DeserializationResult>();

            foreach (var (keyRes, valRes) in Mappings)
            {
                var newKeyRes = keyRes.Copy().Cast<DeserializationResult<TKey>>();
                var newValueRes = valRes.Copy().Cast<DeserializationResult<TValue>>();

                valueDict.Add(newKeyRes.Value, newValueRes.Value);
                mappingDict.Add(newKeyRes, newValueRes);
            }

            return new DeserializedDictionary<TDict, TKey, TValue>(valueDict, mappingDict);
        }
    }
}
