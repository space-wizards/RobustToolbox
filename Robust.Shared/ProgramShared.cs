using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    private static string FindContentRootDir(StartType startType)
    {
        var relative = startType switch
        {
            StartType.Engine => "../../../",
            StartType.Content => "../../",
            StartType.Loader => throw new InvalidOperationException(),
            StartType.ContentAppBundle => "../../../../../",
            _ => throw new ArgumentOutOfRangeException(nameof(startType), startType, null)
        };
        return PathOffset + relative;
    }

    private static string FindEngineRootDir(StartType startType)
    {
        var relative = startType switch
        {
            StartType.Engine => "../../",
            StartType.Content => "../../RobustToolbox/",
            StartType.Loader => throw new InvalidOperationException(),
            StartType.ContentAppBundle => "../../../../../RobustToolbox/",
            _ => throw new ArgumentOutOfRangeException(nameof(startType), startType, null)
        };
        return PathOffset + relative;
    }
#endif

    internal static void PrintRuntimeInfo(ISawmill sawmill)
    {
        foreach (var line in RuntimeInformationPrinter.GetInformationDump())
        {
            sawmill.Debug(line);
        }
    }

    internal static void DoMounts(
        IResourceManagerInternal res,
        MountOptions? options,
        string contentBuildDir,
        ResPath assembliesPath,
        bool loadContentResources = true,
        StartType startType = StartType.Engine)
    {
#if FULL_RELEASE
        if (startType != StartType.Loader)
            res.MountContentDirectory(@"Resources/");
#else
        var engineRoot = FindEngineRootDir(startType);
        // System.Console.WriteLine($"ENGINE DIR IS {engineRoot}");
        res.MountContentDirectory($@"{engineRoot}Resources/");

        if (loadContentResources)
        {
            var contentRootDir = FindContentRootDir(startType);
            // System.Console.WriteLine($"CONTENT DIR IS {Path.GetFullPath(contentRootDir)}");
            if (startType == StartType.ContentAppBundle)
            {
                res.MountContentDirectory("./", assembliesPath);
            }
            else
            {
                res.MountContentDirectory($@"{contentRootDir}bin/{contentBuildDir}/", assembliesPath);
            }

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

internal enum StartType
{
    /// <summary>
    /// We've been started from <c>RobustToolbox/bin/Client/Robust.Client</c>
    /// </summary>
    Engine,

    /// <summary>
    /// We've been started from e.g. <c>bin/Content.Client/Content.Client</c>
    /// </summary>
    Content,

    /// <summary>
    /// We've been started from the launcher loader.
    /// </summary>
    Loader,

    /// <summary>
    /// (macOS only)
    /// We've been started from e.g. <c>bin/Content.Client/Space Station 14.app/Contents/MacOS/Content.Client</c>
    /// </summary>
    ContentAppBundle
}
