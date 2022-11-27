namespace Robust.Shared.Serialization.Manager
{
    // TODO Serialization: make this actually not kanser to use holy moly (& allow generics)
    public interface ISerializationContext
    {
        SerializationManager.SerializerProvider SerializerProvider { get; }
    }
}
