using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Collections;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
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

    /// <summary>
    /// Special-cased file handler for audio that accounts for serverside completion.
    /// </summary>
    public static IEnumerable<CompletionOption> AudioFilePath(string arg, IPrototypeManager protoManager,
        IResourceManager res)
    {
        var resPath = GetUpdatedPath(arg);
        var paths = new HashSet<string>();

        foreach (var path in res.ContentGetDirectoryEntries(resPath))
        {
            paths.Add(path);
        }

        foreach (var audioProto in protoManager.EnumeratePrototypes<AudioMetadataPrototype>())
        {
            var hero = new ResPath(audioProto.ID);

            if (!hero.TryRelativeTo(resPath, out _))
                continue;

            paths.Add(hero.GetNextSegment(resPath).ToString());
        }

        return GetPaths(resPath, paths, res);
    }

    private static ResPath GetUpdatedPath(string arg)
    {
        var curPath = arg;
        if (!curPath.StartsWith("/"))
            curPath = "/";

        var resPath = new ResPath(curPath);

        if (!curPath.EndsWith("/"))
        {
            resPath /= "..";
            resPath = resPath.Clean();
        }

        return resPath;
    }

    private static IEnumerable<CompletionOption> GetPaths(ResPath resPath, IEnumerable<string> inputs, IResourceManager res)
    {
        var options = inputs
            .OrderBy(c => c)
            .Select(c =>
            {
                var opt = (resPath / c).ToString();

                if (c.EndsWith("/"))
                    return new CompletionOption(opt, Flags: CompletionOptionFlags.PartialCompletion);

                return new CompletionOption(opt);
            });

        return options;
    }

    public static IEnumerable<CompletionOption> ContentFilePath(string arg, IResourceManager res)
    {
        var resPath = GetUpdatedPath(arg);
        return GetPaths(resPath, res.ContentGetDirectoryEntries(resPath), res);
    }

    public static IEnumerable<CompletionOption> ContentDirPath(string arg, IResourceManager res)
    {
        var curPath = arg;
        if (!curPath.StartsWith("/"))
            return new[] { new CompletionOption("/") };

        var resPath = new ResPath(curPath);

        if (!curPath.EndsWith("/"))
        {
            resPath /= "..";
            resPath = resPath.Clean();
        }

        var options = res.ContentGetDirectoryEntries(resPath)
            .Where(c => c.EndsWith("/"))
            .OrderBy(c => c)
            .Select(c =>
            {
                var opt = (resPath / c).ToString();

                return new CompletionOption(opt, Flags: CompletionOptionFlags.PartialCompletion);
            });

        return options;
    }

    public static IEnumerable<CompletionOption> UserFilePath(string arg, IWritableDirProvider provider)
    {
        var curPath = arg;
        if (curPath == "")
            curPath = "/";

        var resPath = new ResPath(curPath);

        if (!resPath.IsRooted)
            return Enumerable.Empty<CompletionOption>();

        if (!curPath.EndsWith("/"))
        {
            resPath /= "..";
            resPath = resPath.Clean();
        }

        var entries = provider.DirectoryEntries(resPath);

        return entries
            .Select(c =>
            {
                var full = resPath / c;
                if (provider.IsDir(full))
                    return new CompletionOption($"{full}", Flags: CompletionOptionFlags.PartialCompletion);

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

    public static IEnumerable<CompletionOption> MapIds(IEntityManager? entManager = null)
    {
        IoCManager.Resolve(ref entManager);

        return entManager.EntityQuery<MapComponent>(true).Select(o => new CompletionOption(o.MapId.ToString()));
    }

    public static IEnumerable<CompletionOption> MapUids(IEntityManager? entManager = null)
    {
        IoCManager.Resolve(ref entManager);

        return Components<MapComponent>(string.Empty, entManager, limit: 128);
    }

    /// <summary>
    /// Return all existing entities as possible completions. You should generally avoid using this unless you need to.
    /// </summary>
    public static IEnumerable<CompletionOption> NetEntities(string text, IEntityManager? entManager = null, int limit = 20)
    {
        if (text != string.Empty && !NetEntity.TryParse(text, out _))
            yield break;

        IoCManager.Resolve(ref entManager);
        var query = entManager.AllEntityQueryEnumerator<MetaDataComponent>();

        var i = 0;
        while (i < limit && query.MoveNext(out var metadata))
        {
            var netString = metadata.NetEntity.ToString();
            if (!netString.StartsWith(text))
                continue;

            i++;
            yield return new CompletionOption(netString, metadata.EntityName);
        }
    }

    public static IEnumerable<CompletionOption> Components<T>(string text, IEntityManager? entManager = null, int limit = 20) where T : IComponent
    {
        if (text != string.Empty && !NetEntity.TryParse(text, out _))
            yield break;

        IoCManager.Resolve(ref entManager);
        var query = entManager.AllEntityQueryEnumerator<T, MetaDataComponent>();

        var i = 0;
        while (i < limit && query.MoveNext(out _, out var metadata))
        {
            var netString = metadata.NetEntity.ToString();
            if (!netString.StartsWith(text))
                continue;

            i++;
            yield return new CompletionOption(netString, metadata.EntityName);
        }
    }
}
