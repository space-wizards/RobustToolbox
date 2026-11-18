namespace Robust.Shared.Serialization;

/// <summary>
/// Implementers of this interface will have their <see cref="Clone"/> method
/// called when generating component states for fields marked with
/// <c>AutoNetworkedFieldAttribute</c>. This can be useful for reference types
/// used on auto-networked component fields to make copies of values instead of references.
///
/// This is separate from datafield serialization and
/// <c>ISerializationManager.CreateCopy</c>. Serialization copy behavior should use
/// generated data-definition copying or an <c>ITypeCopyCreator</c>.
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
