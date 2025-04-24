#if TOOLS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Shared.Asynchronous;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.Client.UserInterface.XAML.Proxy;

/// <summary>
/// The real implementation of <see cref="IXamlHotReloadManager" />.
/// </summary>
/// <remarks>
/// Its behavior is described there.
/// </remarks>
internal sealed class XamlHotReloadManager : IXamlHotReloadManager
{
    private const string MarkerFileName = "SpaceStation14.sln";

    [Dependency] ILogManager _logManager = null!;
    [Dependency] private readonly IResourceManager _resources = null!;
    [Dependency] private readonly ITaskManager _taskManager = null!;
    [Dependency] private readonly IXamlProxyManager _xamlProxyManager = null!;

    private ISawmill _sawmill = null!;
    private FileSystemWatcher? _watcher;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("xamlhotreload");
        var codeLocation = InferCodeLocation();

        if (codeLocation == null)
        {
            _sawmill.Warning($"could not find code -- where is {MarkerFileName}?");
            return;
        }

        _sawmill.Info($"code location: {codeLocation}");

        // must not be gc'ed or else it will stop reporting
        // therefore: keep a reference
        _watcher = CreateWatcher(codeLocation);
    }

    /// <summary>
    /// Create a file system watcher that identifies XAML changes in a given
    /// location.
    /// </summary>
    /// <param name="location">the location (a real path on the OS file system)</param>
    /// <returns>the new watcher</returns>
    /// <exception cref="ArgumentOutOfRangeException">if <see cref="FileSystemWatcher"/> violates its type-related postconditions</exception>
    private FileSystemWatcher CreateWatcher(string location)
    {
        var watcher = new FileSystemWatcher(location)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
        };

        void OnWatcherEvent(object sender, FileSystemEventArgs args)
        {
            switch (args.ChangeType)
            {
                case WatcherChangeTypes.Deleted:
                    return;
                case WatcherChangeTypes.Renamed:
                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(args));
            }

            _taskManager.RunOnMainThread(() =>
            {
                var resourceFileName =
                    ResourceFileName(location, args.FullPath, _xamlProxyManager.CanSetImplementation);
                if (resourceFileName == null)
                {
                    return;
                }

                string newText;
                try
                {
                    newText = File.ReadAllText(args.FullPath);
                }
                catch (IOException ie)
                {
                    _sawmill.Warning($"error attempting a hot reload -- skipped: {ie}");
                    return;
                }

                _xamlProxyManager.SetImplementation(resourceFileName, newText);
            });
        }

        watcher.Changed += OnWatcherEvent;
        watcher.Renamed += OnWatcherEvent;
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    /// <summary>
    /// Using the content roots of the project, infer the location of its code.
    /// </summary>
    /// <remarks>
    /// This kind of introspection is almost universally a bad idea, but we don't
    /// feasibly have other options, so I've buried it in a private method.
    /// </remarks>
    /// <returns>the inferred code location or null</returns>
    private string? InferCodeLocation()
    {
        // ascend upwards from each content root until the solution file is found
        foreach (var contentRoot in _resources.GetContentRoots())
        {
            var systemPath = contentRoot.ToRelativeSystemPath();
            while (true)
            {
                var files = Array.Empty<string>();
                try
                {
                    files = Directory.GetFiles(systemPath);
                }
                catch (IOException) { }  // this is allowed to fail, and if so we just keep going up

                if (files.Any(f => Path.GetFileName(f).Equals(MarkerFileName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return systemPath;
                }

                DirectoryInfo? newPath = null;
                try
                {
                    newPath = Directory.GetParent(systemPath);
                }
                catch (IOException) { } // ditto here. if we don't find it, we're in the wrong place

                if (newPath == null)
                {
                    break;
                }

                systemPath = newPath.FullName;
            }
        }

        return null;
    }

    /// <summary>
    /// Infer the name of the resource file associated with the XAML item at the given path.
    /// </summary>
    /// <param name="codeLocation">the code location</param>
    /// <param name="realPath">the real path of the file</param>
    /// <param name="isDesired">a function returning true if something expects this file</param>
    /// <returns>the name of a desired resource that matches this file, or null</returns>
    private string? ResourceFileName(string codeLocation, string realPath, Predicate<string> isDesired)
    {
        // start with the name of the file and systematically add each super-directory until we reach
        // the inferred code location.
        //
        // for /home/pyrex/ss14/Content.Client/Instruments/UI/InstrumentMenu.xaml, the following names
        // will be tried:
        //
        // - InstrumentMenu.xaml
        // - UI.InstrumentMenu.xaml
        // - Instruments.UI.InstrumentMenu.xaml
        // - Content.Client.Instruments.UI.InstrumentMenu.xaml
        var resourceFileName = Path.GetFileName(realPath);
        var super = Directory.GetParent(realPath);

        var canonicalCodeLocation = Path.GetFullPath(codeLocation);

        while (true)
        {
            // did someone want it: OK, jump out
            if (isDesired(resourceFileName))
            {
                return resourceFileName;
            }

            if (super == null || Path.GetFullPath(super.FullName) == canonicalCodeLocation)
            {
                return null;
            }

            resourceFileName = super.Name + "." + resourceFileName;
            super = super.Parent;
        }
    }
}
#endif
