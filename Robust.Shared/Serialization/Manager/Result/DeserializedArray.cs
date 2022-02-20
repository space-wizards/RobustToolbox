using System;

namespace Robust.Shared.Serialization.Manager.Result
{
    public sealed class DeserializedArray : DeserializationResult
    {
        public DeserializedArray(Array array, DeserializationResult[] mappings)
        {
            Value = array;
            Mappings = mappings;
        }

        public Array Value { get; }

        public DeserializationResult[] Mappings { get; }

        public override object RawValue => Value;

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            var sourceCollection = source.Cast<DeserializedArray>();
            var values = (Array) Activator.CreateInstance(Value.GetType(), Value.Length)!;
            var results = new DeserializationResult[sourceCollection.Mappings.Length];

            for (var i = 0; i < sourceCollection.Mappings.Length; i++)
            {
                var oldRes = sourceCollection.Mappings[i];
                var newRes = oldRes.Copy();

                values.SetValue(newRes.RawValue, i);
                results[i] = newRes;
            }

            for (var i = 0; i < Mappings.Length; i++)
            {
                var oldRes = Mappings[i];
                var newRes = oldRes.Copy();

                values.SetValue(newRes.RawValue, i);
                results[i] = newRes;
            }

            return new DeserializedArray(values, results);
        }

        public override DeserializationResult Copy()
        {
            var values = (Array) Activator.CreateInstance(Value.GetType(), Value.Length)!;
            var results = new DeserializationResult[Mappings.Length];

            for (var i = 0; i < Mappings.Length; i++)
            {
                var oldRes = Mappings[i];
                var newRes = oldRes.Copy();

                values.SetValue(newRes.RawValue, i);
                results[i] = newRes;
            }

            return new DeserializedArray(values, results);
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
