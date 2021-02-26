namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedValue<T> : DeserializationResult<T>
    {
        public DeserializedValue(T value)
        {
            Value = value;
        }

        public override T Value { get; }

        public override object? RawValue => Value;
    }
}
