using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared;

internal static class ProgramShared
{
    public static string PathOffset = "";

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
        return PathOffset + (contentStart ? "../../" : "../../../");
    }

    private static string FindEngineRootDir(bool contentStart)
    {
        return PathOffset + (contentStart ? "../../RobustToolbox/" : "../../");
    }
#endif

    internal static void PrintRuntimeInfo(ISawmill sawmill)
    {
        foreach (var line in RuntimeInformationPrinter.GetInformationDump())
        {
            sawmill.Debug(line);
        }
    }

    internal static void DoMounts(IResourceManagerInternal res, MountOptions? options, string contentBuildDir, ResPath assembliesPath, bool loadContentResources = true,
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

    internal static Task CheckBadFileExtensions(IResourceManager res, IConfigurationManager cfg, ISawmill sawmill)
    {
#if !DEBUG
        return Task.CompletedTask;
#else
        if (!cfg.GetCVar(CVars.ResCheckBadFileExtensions))
            return Task.CompletedTask;

        // Run on thread pool to avoid slowing down init.
        return Task.Run(() =>
        {
            foreach (var file in res.ContentFindFiles("/"))
            {
                if (file.Extension == "yaml")
                    sawmill.Warning($"{file} has extension \".yaml\". Robust only loads .yml files by convention, file will be ignored for prototypes and similar.");
            }
        });
#endif
    }

    internal static void FinishCheckBadFileExtensions(Task task)
    {
        task.Wait();
    }
}
