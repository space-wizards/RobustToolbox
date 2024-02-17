using System.Collections.Generic;
using System.Threading.Tasks;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Robust.Shared.ViewVariables;

public interface IViewVariablesManager
{
    /// <summary>
    ///     Allows you to register the handlers for a domain.
    ///     Domains are the top-level segments of a VV path.
    ///     They provide ViewVariables with access to any number of objects.
    ///     A proper domain should only handle the next segment of the path.
    ///
    ///     <code>/entity/12345</code>
    ///
    ///     In the example above, "entity" would be a registered domain
    ///     and "12345" would be an object (entity UID) that is resolved by it.
    ///
    /// </summary>
    /// <param name="domain">The name of the domain to register.</param>
    /// <param name="resolveObject">The handler for resolving paths.</param>
    /// <param name="list">The handler for listing objects under the domain.</param>
    /// <seealso cref="UnregisterDomain"/>
    void RegisterDomain(string domain, DomainResolveObject resolveObject, DomainListPaths list);

    /// <summary>
    ///     Unregisters the handlers for a given domain.
    /// </summary>
    /// <param name="domain">The name of the domain to unregister.</param>
    /// <returns>Whether the domain existed and was able to be unregistered or not.</returns>
    /// <seealso cref="RegisterDomain"/>
    bool UnregisterDomain(string domain);

    /// <summary>
    ///     Retrieves the type handler for a given type.
    ///     Creates it if it didn't exist already. Allows you to register custom handlers for a type.
    ///     Type handlers expand the paths available under a certain type on VV.
    ///
    ///     <code>/entity/12345/Custom</code>
    ///
    ///     In the example above, "Custom" could be a path that the type handler for <see cref="EntityUid"/> provided.
    ///     It does not exist on the <see cref="EntityUid"/> declaration, but that does not matter:
    ///     VV treats it the same as a "real" member under that type.
    ///
    /// </summary>
    /// <returns>The type handler object, which allows you register handlers and paths for the type.</returns>
    /// <seealso cref="RegisterDomain"/>
    ViewVariablesTypeHandler<T> GetTypeHandler<T>();

    /// <param name="path">The path to be resolved.</param>
    /// <returns>An object representing the path, or null if the path couldn't be resolved.</returns>
    ViewVariablesPath? ResolvePath(string path);
    object? ReadPath(string path);
    string? ReadPathSerialized(string path);
    void WritePath(string path, string value);
    object? InvokePath(string path, string arguments);
    IEnumerable<string> ListPath(string path, VVListPathOptions options);

    Task<string?> ReadRemotePath(string path, ICommonSession? session = null);
    Task WriteRemotePath(string path, string value, ICommonSession? session = null);
    Task<string?> InvokeRemotePath(string path, string arguments, ICommonSession? session = null);
    Task<IEnumerable<string>> ListRemotePath(string path, VVListPathOptions options, ICommonSession? session = null);
}
