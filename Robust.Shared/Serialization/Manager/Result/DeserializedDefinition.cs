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
    }
}
