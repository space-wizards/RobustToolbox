using Robust.Shared.IoC;

namespace Robust.Shared.Serialization;

/// <summary>
///     Provides a method that gets executed after deserialization is complete.
/// </summary>
[RequiresExplicitImplementation]
public interface ISerializationHooks
{
    /// <summary>
    ///     Gets executed after deserialization is complete.
    /// </summary>
    /// <remarks>
    ///     This may run on any thread at any time unless otherwise specified. Invoking IoCManager within this method is not supported, use the
    ///     other method.
    /// </remarks>
    void AfterDeserialization()
    {
    }

    /// <summary>
    ///     Gets executed after deserialization is complete.
    /// </summary>
    /// <param name="collection">The main thread dependency collection.</param>
    /// <remarks>
    ///     This may run on any thread at any time unless otherwise specified. While the main thread dependency collection
    ///     is provided, care must be taken to not induce race conditions.
    /// </remarks>
    void AfterDeserialization(IDependencyCollection collection)
    {
    }
}
