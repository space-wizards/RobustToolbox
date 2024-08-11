#if TOOLS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Shared.Asynchronous;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.Client.UserInterface.XAML.Proxy
{
    internal sealed class XamlHotReloadManager : IXamlHotReloadManager
    {
        [Dependency] ILogManager _logManager = null!;
        [Dependency] private readonly IResourceManager _resources = null!;
        [Dependency] private readonly ITaskManager _taskManager = null!;
        [Dependency] private readonly IXamlProxyManager _xamlProxyManager = null!;

        private ISawmill _sawmill = null!;
        private readonly List<FileSystemWatcher> _watchers = new();

        public void Initialize()
        {
            _sawmill = _logManager.GetSawmill("xamlhotreload");
            var codeLocation = InferCodeLocation();
            _sawmill.Info($"code location: {codeLocation}");

            if (codeLocation == null)
            {
                return;
            }

            _watchers.Add(CreateWatcher(codeLocation));
        }

        private FileSystemWatcher CreateWatcher(string location)
        {
            // TODO: Case sensitivity?
            var watcher = new FileSystemWatcher(location)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite,
            };

            watcher.Changed += (_, args) =>
            {
                switch (args.ChangeType)
                {
                    case WatcherChangeTypes.Renamed:
                    case WatcherChangeTypes.Deleted:
                        return;
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
            };
            watcher.EnableRaisingEvents = true;
            return watcher;
        }

        // PYREX NOTE: This is a hack, but this isn't a concept that actually
        // exists anywhere else in the project. I do not really want to
        // expose an API to do this operation that is typically nonsense
        // (But I would if asked!)
        private string? InferCodeLocation()
        {
            foreach (var contentRoot in _resources.GetContentRoots())
            {
                var systemPath = contentRoot.ToRelativeSystemPath();
                while (true)
                {
                    var files = Directory.GetFiles(systemPath);
                    if (files.Any(f => Path.GetFileName(f) == "SpaceStation14.sln"))
                    {
                        return systemPath;
                    }

                    var newPath = Directory.GetParent(systemPath);
                    if (newPath == null)
                    {
                        break;
                    }

                    systemPath = newPath.FullName;
                }
            }

            return null;
        }

        private string? ResourceFileName(string codeLocation, string path, Predicate<string> isDesired)
        {
            var resourceFileName = Path.GetFileName(path);
            var super = Directory.GetParent(path);

            var canonicalCodeLocation = Path.GetFullPath(codeLocation);

            while (true)
            {
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
}
#endif
