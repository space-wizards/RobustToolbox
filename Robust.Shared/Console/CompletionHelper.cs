using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Shared.Console;

/// <summary>
/// Helpers for creating various completion results.
/// </summary>
[PublicAPI]
public static class CompletionHelper
{
    /// <summary>
    ///     Returns the booleans False and True as completion options.
    /// </summary>
    public static IEnumerable<CompletionOption> Booleans => new[]
        { new CompletionOption(bool.FalseString), new CompletionOption(bool.TrueString) };

    public static IEnumerable<CompletionOption> ContentFilePath(string arg, IResourceManager res)
    {
        var curPath = arg;
        if (!curPath.StartsWith("/"))
            curPath = "/";

        var resPath = new ResourcePath(curPath);

        if (!curPath.EndsWith("/"))
            resPath = (resPath / "..").Clean();

        var options = res.ContentGetDirectoryEntries(resPath)
            .OrderBy(c => c)
            .Select(c =>
            {
                var opt = (resPath / c).ToString();

                if (c.EndsWith("/"))
                    return new CompletionOption(opt + "/", Flags: CompletionOptionFlags.PartialCompletion);

                return new CompletionOption(opt);
            });

        return options;
    }

    public static IEnumerable<CompletionOption> UserFilePath(string arg, IWritableDirProvider provider)
    {
        var curPath = arg;
        if (curPath == "")
            curPath = "/";

        var resPath = new ResourcePath(curPath);

        if (!resPath.IsRooted)
            return Enumerable.Empty<CompletionOption>();

        if (!curPath.EndsWith("/"))
            resPath = (resPath / "..").Clean();

        var entries = provider.DirectoryEntries(resPath);

        return entries
            .Select(c =>
            {
                var full = resPath / c;
                if (provider.IsDir(full))
                    return new CompletionOption($"{full}/", Flags: CompletionOptionFlags.PartialCompletion);

                return new CompletionOption(full.ToString());
            })
            .OrderBy(c => c.Value);
    }

    /// <summary>
    ///     Returns a completion list for all prototype IDs of the given type.
    /// </summary>
    /// <remarks>
    ///     Don't use this for prototypes types that likely have a large number of entries, like <see cref="EntityPrototype"/>.
    /// </remarks>
    public static IEnumerable<CompletionOption> PrototypeIDs<T>(bool sorted = true, IPrototypeManager? proto = null)
        where T: class, IPrototype
    {
        IoCManager.Resolve(ref proto);

        var protoOptions = proto.EnumeratePrototypes<T>().Select(p => new CompletionOption(p.ID));
        return sorted ? protoOptions.OrderBy(o => o.Value) : protoOptions;
    }

    /// <summary>
    ///     Returns a list of connected session names.
    /// </summary>
    public static IEnumerable<CompletionOption> SessionNames(bool sorted = true, ISharedPlayerManager? players = null)
    {
        IoCManager.Resolve(ref players);

        var playerOptions = players.Sessions.Select(p => new CompletionOption(p.Name));
        return sorted ? playerOptions.OrderBy(o => o.Value) : playerOptions;
    }
}
