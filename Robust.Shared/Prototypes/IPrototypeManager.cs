using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes;

/// <summary>
/// Handle storage and loading of YAML prototypes.
/// </summary>
/// <remarks>
/// Terminology:
/// "Kinds" are the types of prototypes there are, like <see cref="EntityPrototype"/>.
/// "Prototypes" are simply filled-in prototypes from YAML.
/// </remarks>
public interface IPrototypeManager
{
    void Initialize();

    /// <summary>
    /// Returns an <see cref="IEnumerable{T}"/> of all registered prototype kinds by their ID.
    /// </summary>
    IEnumerable<string> GetPrototypeKinds();

    /// <summary>
    /// Return an <see cref="IEnumerable{T}"/> of all prototypes of a certain kind.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the type of prototype is not registered.
    /// </exception>
    IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype;

    /// <summary>
    /// Return an <see cref="IEnumerable{T}"/> of all prototypes of a certain kind.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the kind of prototype is not registered.
    /// </exception>
    IEnumerable<IPrototype> EnumeratePrototypes(Type kind);

    /// <summary>
    /// Return an <see cref="IEnumerable{T}"/> of all prototypes of a certain kind.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the kind of prototype is not registered.
    /// </exception>
    IEnumerable<IPrototype> EnumeratePrototypes(string variant);

    /// <summary>
    /// Returns an <see cref="IEnumerable{T}"/> of all parents of a prototype of a certain kind.
    /// </summary>
    IEnumerable<T> EnumerateParents<T>(string kind, bool includeSelf = false)
        where T : class, IPrototype, IInheritingPrototype;

    /// <summary>
    /// Returns an <see cref="IEnumerable{T}"/> of parents of a prototype of a certain kind.
    /// </summary>
    IEnumerable<IPrototype> EnumerateParents(Type kind, string id, bool includeSelf = false);

    /// <summary>
    /// Index for a <see cref="IPrototype"/> by ID.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the type of prototype is not registered.
    /// </exception>
    T Index<T>(string id) where T : class, IPrototype;

    /// <summary>
    /// Index for a <see cref="IPrototype"/> by ID.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the ID does not exist or the kind of prototype is not registered.
    /// </exception>
    IPrototype Index(Type kind, string id);

    /// <summary>
    ///     Returns whether a prototype of type <typeparamref name="T"/> with the specified <param name="id"/> exists.
    /// </summary>
    bool HasIndex<T>(string id) where T : class, IPrototype;

    bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : class, IPrototype;
    bool TryIndex(Type kind, string id, [NotNullWhen(true)] out IPrototype? prototype);

    bool HasMapping<T>(string id);
    bool TryGetMapping(Type kind, string id, [NotNullWhen(true)] out MappingDataNode? mappings);

    /// <summary>
    ///     Returns whether a prototype variant <param name="variant"/> exists.
    /// </summary>
    /// <param name="variant">Identifier for the prototype variant.</param>
    /// <returns>Whether the prototype variant exists.</returns>
    [Obsolete("Variant is outdated naming, use *kind* functions instead")]
    bool HasVariant(string variant);

    /// <summary>
    ///     Returns the Type for a prototype variant.
    /// </summary>
    /// <param name="variant">Identifier for the prototype variant.</param>
    /// <returns>The specified prototype Type.</returns>
    /// <exception cref="KeyNotFoundException">
    ///     Thrown when the specified prototype variant isn't registered or doesn't exist.
    /// </exception>
    [Obsolete("Variant is outdated naming, use *kind* functions instead")]
    Type GetVariantType(string variant);

    /// <summary>
    ///     Attempts to get the Type for a prototype variant.
    /// </summary>
    /// <param name="variant">Identifier for the prototype variant.</param>
    /// <param name="prototype">The specified prototype Type, or null.</param>
    /// <returns>Whether the prototype type was found and <see cref="prototype"/> isn't null.</returns>
    [Obsolete("Variant is outdated naming, use *kind* functions instead")]
    bool TryGetVariantType(string variant, [NotNullWhen(true)] out Type? prototype);

    /// <summary>
    ///     Attempts to get a prototype's variant.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="variant"></param>
    /// <returns></returns>
    [Obsolete("Variant is outdated naming, use *kind* functions instead")]
    bool TryGetVariantFrom(Type type, [NotNullWhen(true)] out string? variant);

    /// <summary>
    ///     Attempts to get a prototype's variant.
    /// </summary>
    /// <param name="prototype">The prototype in question.</param>
    /// <param name="variant">Identifier for the prototype variant, or null.</param>
    /// <returns>Whether the prototype variant was successfully retrieved.</returns>
    [Obsolete("Variant is outdated naming, use *kind* functions instead")]
    bool TryGetVariantFrom(IPrototype prototype, [NotNullWhen(true)] out string? variant);

    /// <summary>
    ///     Attempts to get a prototype's variant.
    /// </summary>
    /// <param name="variant">Identifier for the prototype variant, or null.</param>
    /// <typeparam name="T">The prototype in question.</typeparam>
    /// <returns>Whether the prototype variant was successfully retrieved.</returns>
    [Obsolete("Variant is outdated naming, use *kind* functions instead")]
    bool TryGetVariantFrom<T>([NotNullWhen(true)] out string? variant) where T : class, IPrototype;

    /// <summary>
    ///     Returns whether a prototype kind <param name="kind"/> exists.
    /// </summary>
    /// <param name="kind">Identifier for the prototype kind.</param>
    /// <returns>Whether the prototype kind exists.</returns>
    bool HasKind(string kind);

    /// <summary>
    ///     Returns the Type for a prototype kind.
    /// </summary>
    /// <param name="kind">Identifier for the prototype kind.</param>
    /// <returns>The specified prototype Type.</returns>
    /// <exception cref="KeyNotFoundException">
    ///     Thrown when the specified prototype kind isn't registered or doesn't exist.
    /// </exception>
    Type GetKindType(string kind);

    /// <summary>
    ///     Attempts to get the Type for a prototype kind.
    /// </summary>
    /// <param name="kind">Identifier for the prototype kind.</param>
    /// <param name="prototype">The specified prototype Type, or null.</param>
    /// <returns>Whether the prototype type was found and <see cref="prototype"/> isn't null.</returns>
    bool TryGetKindType(string kind, [NotNullWhen(true)] out Type? prototype);

    /// <summary>
    ///     Attempts to get a prototype's kind.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="kind"></param>
    /// <returns></returns>
    bool TryGetKindFrom(Type type, [NotNullWhen(true)] out string? kind);

    /// <summary>
    ///     Attempts to get a prototype's kind.
    /// </summary>
    /// <param name="prototype">The prototype in question.</param>
    /// <param name="kind">Identifier for the prototype kind, or null.</param>
    /// <returns>Whether the prototype kind was successfully retrieved.</returns>
    bool TryGetKindFrom(IPrototype prototype, [NotNullWhen(true)] out string? kind);

    /// <summary>
    ///     Attempts to get a prototype's kind.
    /// </summary>
    /// <param name="kind">Identifier for the prototype kind, or null.</param>
    /// <typeparam name="T">The prototype in question.</typeparam>
    /// <returns>Whether the prototype kind was successfully retrieved.</returns>
    bool TryGetKindFrom<T>([NotNullWhen(true)] out string? kind) where T : class, IPrototype;

    /// <summary>
    /// Load prototypes from files in a directory, recursively.
    /// </summary>
    void LoadDirectory(ResPath path, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null);

    Dictionary<string, HashSet<ErrorNode>> ValidateDirectory(ResPath path);

    void LoadFromStream(TextReader stream, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null);

    void LoadString(string str, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null);

    void RemoveString(string prototypes);

    /// <summary>
    /// Clear out all prototypes and reset to a blank slate.
    /// </summary>
    void Clear();

    /// <summary>
    /// Syncs all inter-prototype data. Call this when operations adding new prototypes are done.
    /// </summary>
    void ResolveResults();

    /// <summary>
    /// Reload the changes from LoadString
    /// </summary>
    /// <param name="prototypes">Changes from load string</param>
    void ReloadPrototypes(Dictionary<Type, HashSet<string>> prototypes);

    /// <summary>
    ///     Registers a specific prototype name to be ignored.
    /// </summary>
    void RegisterIgnore(string name);

    /// <summary>
    /// Loads a single prototype class type into the manager.
    /// </summary>
    /// <param name="protoClass">A prototype class type that implements IPrototype. This type also
    /// requires a <see cref="PrototypeAttribute"/> with a non-empty class string.</param>
    [Obsolete("Prototype type is outdated naming, use *king* functions instead")]
    void RegisterType(Type protoClass);

    /// <summary>
    /// Loads a single prototype kind into the manager.
    /// </summary>
    /// <param name="kind">
    /// The type of the prototype kind that implements <see cref="IPrototype"/>. This type also
    /// requires a <see cref="PrototypeAttribute"/> with a non-empty class string.
    /// </param>
    void RegisterKind(Type kind);

    /// <summary>
    ///     Fired when prototype are reloaded. The event args contain the modified prototypes.
    /// </summary>
    /// <remarks>
    ///     This does NOT fire on initial prototype load.
    /// </remarks>
    event Action<PrototypesReloadedEventArgs> PrototypesReloaded;
}

internal interface IPrototypeManagerInternal : IPrototypeManager
{
    event Action<DataNodeDocument>? LoadedData;
}

public sealed record PrototypesReloadedEventArgs(
    IReadOnlyDictionary<Type, PrototypesReloadedEventArgs.PrototypeChangeSet> ByType)
{
    public sealed record PrototypeChangeSet(IReadOnlyDictionary<string, IPrototype> Modified);
}
