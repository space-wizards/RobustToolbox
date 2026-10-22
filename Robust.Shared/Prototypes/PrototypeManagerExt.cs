 using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Robust.Shared.Prototypes;

/// <summary>
/// Extension methods for working with <see cref="IPrototypeManager"/>.
/// </summary>
[PublicAPI]
public static class PrototypeManagerExt
{
    /// <summary>
    /// Index for a <see cref="IPrototype"/> by ID, returning null if the ID is null.
    /// </summary>
    /// <param name="prototypeManager"></param>
    /// <param name="protoId">The prototype ID to look up.</param>
    /// <typeparam name="T">The kind of prototype to look up.</typeparam>
    /// <returns>The prototype, or null if <paramref name="protoId"/> is <see langword="null"/>.</returns>
    /// <exception cref="UnknownPrototypeException">
    /// Thrown if the prototype ID given is invalid.
    /// </exception>
    [return: NotNullIfNotNull(nameof(protoId))]
    public static T? Index<T>(this IPrototypeManager prototypeManager, ProtoId<T>? protoId) where T : class, IPrototype
    {
        return protoId is null ? null : prototypeManager.Index(protoId.Value);
    }
}
