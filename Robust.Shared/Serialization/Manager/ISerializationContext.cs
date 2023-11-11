namespace Robust.Shared.Serialization.Manager;

// TODO Serialization: make this actually not kanser to use holy moly (& allow generics)
public interface ISerializationContext
{
    SerializationManager.SerializerProvider SerializerProvider { get; }

    // This is just here for content tests that may want their own test diffs.
    /// <summary>
    /// Are we currently iterating prototypes or entities for writing.
    /// </summary>
    bool WritingReadingPrototypes { get; }
}

public sealed class EntityDiffContext : ISerializationContext
{
    public SerializationManager.SerializerProvider SerializerProvider { get; }
    public bool WritingReadingPrototypes { get; set; } = true;

    public EntityDiffContext()
    {
        SerializerProvider = new();
        SerializerProvider.RegisterSerializer(this);
    }
}
