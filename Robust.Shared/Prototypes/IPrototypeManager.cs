using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes;

/// <summary>
/// Handle storage and loading of YAML prototypes.
/// </summary>
public interface IPrototypeManager
{
    void Initialize();

    /// <summary>
    /// Returns an IEnumerable to iterate all registered prototype kind by their ID.
    /// </summary>
    IEnumerable<string> GetPrototypeKinds();

    /// <summary>
    /// Return an IEnumerable to iterate all prototypes of a certain type.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the type of prototype is not registered.
    /// </exception>
    IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype;

    /// <summary>
    /// Return an IEnumerable to iterate all prototypes of a certain type.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the type of prototype is not registered.
    /// </exception>
    IEnumerable<IPrototype> EnumeratePrototypes(Type type);

    /// <summary>
    /// Return an IEnumerable to iterate all prototypes of a certain variant.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the variant of prototype is not registered.
    /// </exception>
    IEnumerable<IPrototype> EnumeratePrototypes(string variant);

    /// <summary>
    /// Returns an IEnumerable to iterate all parents of a prototype of a certain type.
    /// </summary>
    IEnumerable<T> EnumerateParents<T>(string id, bool includeSelf = false) where T : class, IPrototype, IInheritingPrototype;

    /// <summary>
    /// Returns an IEnumerable to iterate all parents of a prototype of a certain type.
    /// </summary>
    IEnumerable<IPrototype> EnumerateParents(Type type, string id, bool includeSelf = false);

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
    /// Thrown if the ID does not exist or the type of prototype is not registered.
    /// </exception>
    IPrototype Index(Type type, string id);

    /// <summary>
    ///     Returns whether a prototype of type <typeparamref name="T"/> with the specified <param name="id"/> exists.
    /// </summary>
    bool HasIndex<T>(string id) where T : class, IPrototype;
    bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : class, IPrototype;
    bool TryIndex(Type type, string id, [NotNullWhen(true)] out IPrototype? prototype);

    bool HasMapping<T>(string id);
    bool TryGetMapping(Type type, string id, [NotNullWhen(true)] out MappingDataNode? mappings);

    /// <summary>
    ///     Returns whether a prototype variant <param name="variant"/> exists.
    /// </summary>
    /// <param name="variant">Identifier for the prototype variant.</param>
    /// <returns>Whether the prototype variant exists.</returns>
    bool HasVariant(string variant);

    /// <summary>
    ///     Returns the Type for a prototype variant.
    /// </summary>
    /// <param name="variant">Identifier for the prototype variant.</param>
    /// <returns>The specified prototype Type.</returns>
    /// <exception cref="KeyNotFoundException">
    ///     Thrown when the specified prototype variant isn't registered or doesn't exist.
    /// </exception>
    Type GetVariantType(string variant);

    /// <summary>
    ///     Attempts to get the Type for a prototype variant.
    /// </summary>
    /// <param name="variant">Identifier for the prototype variant.</param>
    /// <param name="prototype">The specified prototype Type, or null.</param>
    /// <returns>Whether the prototype type was found and <see cref="prototype"/> isn't null.</returns>
    bool TryGetVariantType(string variant, [NotNullWhen(true)] out Type? prototype);

    /// <summary>
    ///     Attempts to get a prototype's variant.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="variant"></param>
    /// <returns></returns>
    bool TryGetVariantFrom(Type type, [NotNullWhen(true)] out string? variant);

    /// <summary>
    ///     Attempts to get a prototype's variant.
    /// </summary>
    /// <param name="prototype">The prototype in question.</param>
    /// <param name="variant">Identifier for the prototype variant, or null.</param>
    /// <returns>Whether the prototype variant was successfully retrieved.</returns>
    bool TryGetVariantFrom(IPrototype prototype, [NotNullWhen(true)] out string? variant);

    /// <summary>
    ///     Attempts to get a prototype's variant.
    /// </summary>
    /// <param name="variant">Identifier for the prototype variant, or null.</param>
    /// <typeparam name="T">The prototype in question.</typeparam>
    /// <returns>Whether the prototype variant was successfully retrieved.</returns>
    bool TryGetVariantFrom<T>([NotNullWhen(true)] out string? variant) where T : class, IPrototype;

    /// <summary>
    /// Load prototypes from files in a directory, recursively.
    /// </summary>
    void LoadDirectory(ResourcePath path, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null);

    Dictionary<string, HashSet<ErrorNode>> ValidateDirectory(ResourcePath path);

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
    void RegisterType(Type protoClass);

    event Action<YamlStream, string>? LoadedData;

    /// <summary>
    ///     Fired when prototype are reloaded. The event args contain the modified prototypes.
    /// </summary>
    /// <remarks>
    ///     This does NOT fire on initial prototype load.
    /// </remarks>
    event Action<PrototypesReloadedEventArgs> PrototypesReloaded;
}

public sealed record PrototypesReloadedEventArgs(IReadOnlyDictionary<Type, PrototypesReloadedEventArgs.PrototypeChangeSet> ByType)
{
    public sealed record PrototypeChangeSet(IReadOnlyDictionary<string, IPrototype> Modified);
}
