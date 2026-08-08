namespace Robust.Shared.Serialization;

/// <summary>
/// Implementers of this interface will have their <see cref="Clone"/> method
/// called when generating component states. This can be useful for reference types
/// used as datafields to make copies of values instead of references.
/// </summary>
/// <typeparam name="T">
/// Type returned by the <see cref="Clone"/> method.
/// This should probably be the same Type as the implementer.
/// </typeparam>
public interface IRobustCloneable<T>
{
    /// <summary>
    /// Returns a new instance of <typeparamref name="T"/> with the same values as this instance.
    /// </summary>
    /// <returns>A new instance of <typeparamref name="T"/> with the same values as this instance.</returns>
    T Clone();
}
