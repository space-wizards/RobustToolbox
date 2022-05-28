using System.Collections.Generic;
using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Shared.Console;

/// <summary>
/// Helpers for creating various completion results.
/// </summary>
public static class CompletionHelper
{
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
}
