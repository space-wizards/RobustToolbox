using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Utility;

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
            var mappingDict = Mappings.ToDictionary(p => p.Key.Copy(), p => p.Value.Copy());

            foreach (var (keyRes, valRes) in sourceRes.Mappings)
            {
                var newKeyRes = keyRes.Copy();
                var newValueRes = valRes.Copy();

                var oldEntry = mappingDict.FirstOrNull(p => Equals(p.Key.RawValue, newKeyRes.RawValue));
                if (oldEntry.HasValue)
                {
                    newKeyRes = oldEntry.Value.Key.PushInheritanceFrom(newKeyRes);
                    newValueRes = oldEntry.Value.Value.PushInheritanceFrom(newValueRes);
                    mappingDict.Remove(oldEntry.Value.Key);
                }
                mappingDict.Add(newKeyRes, newValueRes);
            }

            var valueDict = new TDict();
            foreach (var (key, val) in mappingDict)
            {
                valueDict.Add((TKey) key.RawValue!, (TValue) val.RawValue!);
            }

            return new DeserializedDictionary<TDict, TKey, TValue>(valueDict, mappingDict);
        }

        public override DeserializationResult Copy()
        {
            var valueDict = new TDict();
            var mappingDict = new Dictionary<DeserializationResult, DeserializationResult>();

            foreach (var (keyRes, valRes) in Mappings)
            {
                var newKeyRes = keyRes.Copy();
                var newValueRes = valRes.Copy();

                valueDict.Add((TKey) newKeyRes.RawValue!, (TValue)newValueRes.RawValue!);
                mappingDict.Add(newKeyRes, newValueRes);
            }

            return new DeserializedDictionary<TDict, TKey, TValue>(valueDict, mappingDict);
        }

        public override void CallAfterDeserializationHook()
        {
            foreach (var (key, val) in Mappings)
            {
                key.CallAfterDeserializationHook();
                val.CallAfterDeserializationHook();
            }
        }
    }
}
