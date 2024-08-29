namespace Robust.Shared.Serialization;

/// <summary>
/// Provides a method that gets executed after deserialization is complete and a method that gets executed before serialization
/// </summary>
[RequiresExplicitImplementation]
public interface ISerializationHooks
{
    /// <summary>
    /// Gets executed after deserialization is complete
    /// </summary>
    void AfterDeserialization() {}
}
