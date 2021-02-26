using System;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedDefinition<T> : DeserializationResult
    {
        public DeserializedDefinition(T value, DeserializedFieldEntry[] mapping)
        {
            Value = value;
            Mapping = mapping;
        }

        public T Value { get; }

        public DeserializedFieldEntry[] Mapping { get; }

        public override object? RawValue => Value;

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            var dataDef = source.As<DeserializedDefinition<T>>();
            if (dataDef.Mapping.Length != Mapping.Length)
                throw new ArgumentException($"Mappinglength mismatch in PushInheritanceFrom ({typeof(T)})");

            //todo paul split populate into serializing into deserializeddatadef & populating obj
            var

            for (int i = 0; i < dataDef.Mapping.Length; i++)
            {
                if(Mapping[i].Mapped) continue;

            }
        }
    }
}
