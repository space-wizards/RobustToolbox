namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedValue<T> : DeserializationResult<T>
    {
        public DeserializedValue(T? value)
        {
            Value = value;
        }

        public override T? Value { get; }

        public override object? RawValue => Value;

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            return source.Copy().Cast<DeserializedValue<T>>();
        }

        public override DeserializationResult Copy()
        {
            return new DeserializedValue<T>(Value);
        }
    }
}
