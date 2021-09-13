namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedValue : DeserializationResult
    {
        public DeserializedValue(object? value)
        {
            RawValue = value;
        }

        public override object? RawValue { get; }

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            return source.Copy().Cast<DeserializedValue>();
        }

        public override DeserializationResult Copy()
        {
            return new DeserializedValue(RawValue);
        }

        public override void CallAfterDeserializationHook()
        {
            if (RawValue is ISerializationHooks hooks)
                hooks.AfterDeserialization();
        }
    }

    public class DeserializedValue<T> : DeserializationResult<T>
    {
        public DeserializedValue(T value)
        {
            Value = value;
        }

        public override T Value { get; }

        public override object? RawValue => Value;

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            return source.Copy().Cast<DeserializedValue<T>>();
        }

        public override DeserializationResult Copy()
        {
            return new DeserializedValue<T>(Value);
        }

        public override void CallAfterDeserializationHook()
        {
            if (Value is ISerializationHooks hooks)
            {
                hooks.AfterDeserialization();
            }
        }
    }
}
