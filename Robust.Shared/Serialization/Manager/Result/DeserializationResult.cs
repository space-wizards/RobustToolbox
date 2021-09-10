namespace Robust.Shared.Serialization.Manager.Result
{
    public abstract class DeserializationResult
    {
        public abstract object? RawValue { get; }

        public abstract DeserializationResult PushInheritanceFrom(DeserializationResult source);

        public abstract DeserializationResult Copy();

        public abstract void CallAfterDeserializationHook();

        public T Cast<T>() where T : DeserializationResult
        {
            if (this is T value) return value;
            throw new InvalidDeserializedResultTypeException<T>(GetType());
        }
    }

    public abstract class DeserializationResult<T> : DeserializationResult
    {
        public abstract T? Value { get; }
    }
}
