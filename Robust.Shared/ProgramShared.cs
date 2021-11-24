using System.Collections.Generic;
using System.Runtime.InteropServices;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared;

internal static class ProgramShared
{
    public static void RunExecCommands(IConsoleHost consoleHost, IReadOnlyList<string>? commands)
    {
        if (commands == null)
            return;

        foreach (var cmd in commands)
        {
            consoleHost.ExecuteCommand(cmd);
        }
    }

#if !FULL_RELEASE
    private static string FindContentRootDir(bool contentStart)
    {
        return contentStart ? "../../" : "../../../";
    }

    private static string FindEngineRootDir(bool contentStart)
    {
        return contentStart ? "../../RobustToolbox/" : "../../";
    }
#endif

    internal static void PrintRuntimeInfo(ISawmill sawmill)
    {
        sawmill.Debug($"Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.RuntimeIdentifier}");
        sawmill.Debug($"OS: {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}");
    }

    internal static void DoMounts(IResourceManagerInternal res, MountOptions? options, string contentBuildDir, ResourcePath assembliesPath, bool loadContentResources = true,
        bool loader = false, bool contentStart = false)
    {
#if FULL_RELEASE
            if (!loader)
                res.MountContentDirectory(@"Resources/");
#else
        res.MountContentDirectory($@"{FindEngineRootDir(contentStart)}Resources/");

        if (loadContentResources)
        {
            var contentRootDir = FindContentRootDir(contentStart);
            res.MountContentDirectory($@"{contentRootDir}bin/{contentBuildDir}/", assembliesPath);
            res.MountContentDirectory($@"{contentRootDir}Resources/");
        }
#endif

        if (options == null)
            return;

        foreach (var diskPath in options.DirMounts)
        {
            res.MountContentDirectory(diskPath);
        }

        foreach (var diskPath in options.ZipMounts)
        {
            res.MountContentPack(diskPath);
        }
    }
}
