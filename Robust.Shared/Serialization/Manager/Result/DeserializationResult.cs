using System;

namespace Robust.Shared.Serialization.Manager.Result
{
    public abstract class DeserializationResult
    {
        public abstract object? RawValue { get; }

        public abstract DeserializationResult PushInheritanceFrom(DeserializationResult source);

        public abstract DeserializationResult Copy();

        public abstract void CallAfterDeserializationHook();

        public static DeserializationResult Value<T>(T value)
        {
            return Value(typeof(T), value);
        }

        public static DeserializationResult Value(Type type, object? value)
        {
            var genericType = typeof(DeserializedValue<>).MakeGenericType(type);
            return (DeserializationResult) Activator.CreateInstance(genericType, value)!;

        }

        public T Cast<T>() where T : DeserializationResult
        {
            if (this is T value) return value;
            throw new InvalidDeserializedResultTypeException<T>(GetType());
        }
    }

    public abstract class DeserializationResult<T> : DeserializationResult
    {
        public abstract T Value { get; }
    }
}
