using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Robust.Shared.Random;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

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
    /// Returns the count of the specified prototype.
    /// </summary>
    int Count<T>() where T : class, IPrototype;

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
    /// <remarks>
    /// Note that this will skip abstract parents, even if the abstract parent may have concrete grand-parents.
    /// </remarks>
    IEnumerable<T> EnumerateParents<T>(T proto, bool includeSelf = false)
        where T : class, IPrototype, IInheritingPrototype;

    /// <inheritdoc cref="EnumerateParents{T}(T,bool)"/>
    IEnumerable<T> EnumerateParents<T>(string id, bool includeSelf = false)
        where T : class, IPrototype, IInheritingPrototype;

    /// <inheritdoc cref="EnumerateParents{T}(T,bool)"/>
    IEnumerable<IPrototype> EnumerateParents(Type kind, string id, bool includeSelf = false);

    /// <summary>
    /// Variant of <see cref="EnumerateParents{T}(T,bool)"/> that includes abstract parents.
    /// </summary>
    IEnumerable<(string id, T?)> EnumerateAllParents<T>(string id, bool includeSelf = false)
        where T : class, IPrototype, IInheritingPrototype;

    /// <summary>
    /// Returns all of the registered prototype kinds.
    /// </summary>
    IEnumerable<Type> EnumeratePrototypeKinds();

    /// <summary>
    /// Index for a <see cref="IPrototype"/> by ID.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the type of prototype is not registered.
    /// </exception>
    T Index<T>(string id) where T : class, IPrototype;

    /// <inheritdoc cref="Index{T}(string)"/>
    EntityPrototype Index(EntProtoId id);

    /// <inheritdoc cref="Index{T}(string)"/>
    T Index<T>(ProtoId<T> id) where T : class, IPrototype;

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

    /// <inheritdoc cref="HasIndex{T}(string)"/>
    bool HasIndex(EntProtoId id);

    /// <inheritdoc cref="HasIndex{T}(string)"/>
    bool HasIndex<T>(ProtoId<T> id) where T : class, IPrototype;

    /// <inheritdoc cref="HasIndex{T}(string)"/>
    bool HasIndex(EntProtoId? id);

    /// <inheritdoc cref="HasIndex{T}(string)"/>
    bool HasIndex<T>(ProtoId<T>? id) where T : class, IPrototype;

    bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : class, IPrototype;
    bool TryIndex(Type kind, string id, [NotNullWhen(true)] out IPrototype? prototype);

    /// <summary>
    /// Attempts to get a dictionary containing all current instances of a given prototype kind.
    /// The dictionary will be valid up until prototypes are next reloaded.
    /// </summary>
    bool TryGetInstances<T>([NotNullWhen(true)] out FrozenDictionary<string, T>? instances)
        where T : IPrototype;

    /// <summary>
    /// Gets a dictionary containing all current instances of a given prototype kind.
    /// The dictionary will be valid up until prototypes are next reloaded.
    /// </summary>
    FrozenDictionary<string, T> GetInstances<T>() where T : IPrototype;

    /// <inheritdoc cref="TryIndex{T}(ProtoId{T}, out T, bool)"/>
    bool TryIndex(EntProtoId id, [NotNullWhen(true)] out EntityPrototype? prototype,  bool logError = true);

    /// <summary>
    /// Attempt to retrieve the prototype corresponding to the given prototype id.
    /// Unless otherwise specified, this will log an error if the id does not match any known prototype.
    /// </summary>
    bool TryIndex<T>(ProtoId<T> id, [NotNullWhen(true)] out T? prototype,  bool logError = true) where T : class, IPrototype;

    /// <inheritdoc cref="TryIndex{T}(ProtoId{T}, out T, bool)"/>
    bool TryIndex(EntProtoId? id, [NotNullWhen(true)] out EntityPrototype? prototype,  bool logError = true);

    /// <inheritdoc cref="TryIndex{T}(ProtoId{T}, out T, bool)"/>
    bool TryIndex<T>(ProtoId<T>? id, [NotNullWhen(true)] out T? prototype, bool logError = true) where T : class, IPrototype;

    bool HasMapping<T>(string id);
    bool TryGetMapping(Type kind, string id, [NotNullWhen(true)] out MappingDataNode? mappings);

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

    /// <summary>
    /// Validate all prototypes defined in yaml files contained in the given directory.
    /// </summary>
    /// <param name="path">The directory containing the yaml files that need validating.</param>
    /// <returns>A dictionary containing sets of errors for each file that failed validation.</returns>
    Dictionary<string, HashSet<ErrorNode>> ValidateDirectory(ResPath path);

    /// <summary>
    /// Validate all prototypes defined in yaml files contained in the given directory.
    /// </summary>
    /// <param name="path">The directory containing the yaml files that need validating.</param>
    /// <param name="prototypes">The prototypes ids that were present in the directory.</param>
    /// <returns>A dictionary containing sets of errors for each file that failed validation.</returns>
    Dictionary<string, HashSet<ErrorNode>> ValidateDirectory(ResPath path,
        out Dictionary<Type, HashSet<string>> prototypes);

    /// <summary>
    /// This method uses reflection to validate that all static prototype id fields correspond to valid prototypes.
    /// This will validate all known to <see cref="IReflectionManager"/>
    /// </summary>
    /// <remarks>
    /// This will validate any field that has either a <see cref="ValidatePrototypeIdAttribute{T}"/> attribute, or a
    /// <see cref="DataFieldAttribute"/> with a <see cref="PrototypeIdSerializer{TPrototype}"/> serializer.
    /// </remarks>
    /// <param name="prototypes">A collection prototypes to use for validation. Any prototype not in this collection
    /// will be considered invalid.</param>
    List<string> ValidateStaticFields(Dictionary<Type, HashSet<string>> prototypes);

    /// <summary>
    /// This is a variant of <see cref="ValidateStaticFields(System.Collections.Generic.Dictionary{System.Type,System.Collections.Generic.HashSet{string}})"/> that only validates a single type.
    /// </summary>
    List<string> ValidateStaticFields(Type type, Dictionary<Type, HashSet<string>> prototypes);

    /// <summary>
    /// This method will serialize all loaded prototypes into yaml and then validate them. This can be used to ensure
    /// that hard coded default values for data-fields all pass the normal yaml validation steps.
    /// </summary>
    /// <returns>Returns a collection of yaml validation errors, sorted by prototype kind id. The outer dictionary is
    /// empty, everything was successfully validated.</returns>
    Dictionary<Type, Dictionary<string, HashSet<ErrorNode>>> ValidateAllPrototypesSerializable(ISerializationContext? ctx);

    void LoadFromStream(TextReader stream, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null);

    void LoadString(string str, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null);

    void RemoveString(string prototypes);

    /// <summary>
    /// Clear out all prototypes and reset to a blank slate.
    /// </summary>
    void Clear();

    /// <summary>
    /// Calls <see cref="Clear"/> and then rediscovers all prototype kinds.
    /// </summary>
    void ReloadPrototypeKinds();

    /// <summary>
    /// Calls <see cref="ReloadPrototypeKinds"/> and then loads prototypes from the default directories.
    /// </summary>
    void Reset();

    /// <summary>
    /// Loads prototypes from the default directories.
    /// </summary>
    /// <param name="loaded">Dictionary that will be filled with all the loaded prototypes.</param>
    void LoadDefaultPrototypes(Dictionary<Type, HashSet<string>>? loaded = null);

    /// <summary>
    /// Call this when operations adding new prototypes are done.
    /// This will handle prototype inheritance, instance creation, and update entity categories.
    /// When loading extra prototypes, or reloading a subset of existing prototypes, you should probably use
    /// <see cref="ReloadPrototypes"/> instead.
    /// </summary>
    void ResolveResults();

    /// <summary>
    /// This should be called after new or updated prototypes ahve been loaded.
    /// This will handle prototype inheritance, instance creation, and update entity categories.
    /// It will also invoke <see cref="PrototypesReloaded"/> and raise a <see cref="PrototypesReloadedEventArgs"/>
    /// event with information about the modified prototypes.
    /// </summary>
    void ReloadPrototypes(
        Dictionary<Type, HashSet<string>> modified,
        Dictionary<Type, HashSet<string>>? removed = null);

    /// <summary>
    ///     Registers a specific prototype name to be ignored.
    /// </summary>
    void RegisterIgnore(string name);

    /// <summary>
    /// Loads several prototype kinds into the manager. Note that this will re-build a frozen dictionary and should be avoided if possible.
    /// </summary>
    /// <param name="kind">
    /// The type of the prototype kind that implements <see cref="IPrototype"/>. This type also
    /// requires a <see cref="PrototypeAttribute"/> with a non-empty class string.
    /// </param>
    void RegisterKind(params Type[] kinds);

    /// <summary>
    ///     Fired when prototype are reloaded. The event args contain the modified and removed prototypes.
    /// </summary>
    /// <remarks>
    ///     This does NOT fire on initial prototype load.
    /// </remarks>
    event Action<PrototypesReloadedEventArgs> PrototypesReloaded;

    /// <summary>
    /// Get the yaml data for a given prototype.
    /// </summary>
    IReadOnlyDictionary<string, MappingDataNode> GetPrototypeData(EntityPrototype prototype);

    /// <summary>
    ///     Forces all prototypes in the given file to be abstract.
    ///     This makes them be read as abstract prototypes (mappings) instead of regular prototype instances.
    ///     Calling this method will not retroactively abstract prototypes that have already been read.
    /// </summary>
    /// <param name="path">
    ///     The file to force prototypes to be abstract in.
    ///     This must start from the Resources-level directory, but not include Resources itself.
    ///     For example: /Prototypes/Guidebook/antagonist.yml
    /// </param>
    void AbstractFile(ResPath path);

    /// <summary>
    ///     Forces all prototypes in files recursively within this directory to be abstract.
    ///     This makes them be read as abstract prototypes (mappings) instead of regular prototype instances.
    ///     Calling this method will not retroactively abstract prototypes that have already been read.
    /// </summary>
    /// <param name="path">
    ///     The directory to force prototypes to be abstract in.
    ///     This must start from the Resources-level directory, but not include Resources itself.
    ///     For example: /Prototypes/Guidebook
    /// </param>
    void AbstractDirectory(ResPath path);

    /// <summary>
    /// Tries to get a random prototype.
    /// </summary>
    bool TryGetRandom<T>(IRobustRandom random, [NotNullWhen(true)] out IPrototype? prototype) where T : class, IPrototype;

    /// <summary>
    /// Entity prototypes grouped by their categories.
    /// </summary>
    FrozenDictionary<ProtoId<EntityCategoryPrototype>, IReadOnlyList<EntityPrototype>> Categories { get; }
}

internal interface IPrototypeManagerInternal : IPrototypeManager
{
    event Action<DataNodeDocument>? LoadedData;
}

/// <summary>
/// This is event contains information about prototypes that have been modified. It is broadcast as a system event,
/// whenever <see cref="IPrototypeManager.PrototypesReloaded"/> gets invoked.
/// </summary>
public sealed record PrototypesReloadedEventArgs(HashSet<Type> Modified,
    IReadOnlyDictionary<Type, PrototypesReloadedEventArgs.PrototypeChangeSet> ByType,
    IReadOnlyDictionary<Type, HashSet<string>>? Removed = null)
{
    public sealed record PrototypeChangeSet(IReadOnlyDictionary<string, IPrototype> Modified);

    /// <summary>
    /// Checks whether a given prototype kind was modified at all. This includes both changes and removals.
    /// </summary>
    public bool WasModified<T>() where T : IPrototype
    {
        return Modified.Contains(typeof(T));
    }

    /// <summary>
    /// Returns a set of all modified prototype instances of a given kind. This includes both changes and removals.
    /// </summary>
    public bool TryGetModified<T>([NotNullWhen(true)] out HashSet<string>? modified) where T : IPrototype
    {
        modified = null;
        if (!WasModified<T>())
            return false;

        modified = new();
        if (ByType.TryGetValue(typeof(T), out var mod))
            modified.UnionWith(mod.Modified.Keys);

        if (Removed != null && Removed.TryGetValue(typeof(T), out var rem))
            modified.UnionWith(rem);

        return true;
    }
}
