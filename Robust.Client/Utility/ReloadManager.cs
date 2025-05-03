using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Robust.Client.Utility;

internal sealed class ReloadManager : IReloadManager
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logMan = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly ITaskManager _tasks = default!;

    private readonly TimeSpan _reloadDelay = TimeSpan.FromMilliseconds(10);
    private CancellationTokenSource _reloadToken = new();
    private readonly HashSet<ResPath> _reloadQueue = new();

    public event Action<ResPath>? OnChanged;

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = _logMan.GetSawmill("reload");
        _clyde.OnWindowFocused += WindowFocusedChanged;
    }

    private void WindowFocusedChanged(WindowFocusedEventArgs args)
    {
#if TOOLS
        if (args.Focused && _reloadQueue.Count > 0)
        {
            Timer.Spawn(_reloadDelay, ReloadFiles, _reloadToken.Token);
        }
        else
        {
            _reloadToken.Cancel();
            _reloadToken = new CancellationTokenSource();
        }
#endif
    }

    private void ReloadFiles()
    {
        foreach (var file in _reloadQueue)
        {
            var rootedFile = file.ToRootedPath();

            if (!_res.ContentFileExists(rootedFile))
                continue;

            _sawmill.Info($"Reloading {rootedFile}");
            OnChanged?.Invoke(rootedFile);
        }

        _reloadQueue.Clear();
    }

    public void Register(ResPath directory, string filter)
    {
        Register(directory.ToString(), filter);
    }

    public void Register(string directory, string filter)
    {
        if (!_cfg.GetCVar(CVars.ResPrototypeReloadWatch))
            return;

#if TOOLS
        foreach (var root in _res.GetContentRoots())
        {
            var path = root + directory;

            if (!Directory.Exists(path))
            {
                continue;
            }

            var watcher = new FileSystemWatcher(path, filter)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite
            };


            watcher.Changed += OnWatch;

            try
            {
                watcher.EnableRaisingEvents = true;
            }
            catch (IOException ex)
            {
                _sawmill.Error($"Watching resources in path {path} threw an exception:\n{ex}");
            }
        }

        void OnWatch(object sender, FileSystemEventArgs args)
        {
            switch (args.ChangeType)
            {
                case WatcherChangeTypes.Renamed:
                case WatcherChangeTypes.Deleted:
                    return;
                case WatcherChangeTypes.Created:
                // case WatcherChangeTypes.Deleted:
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _tasks.RunOnMainThread(() =>
            {
                var fullPath = args.FullPath.Replace(Path.DirectorySeparatorChar, '/');
                var file = new ResPath(fullPath);

                foreach (var rootIter in _res.GetContentRoots())
                {
                    if (!file.TryRelativeTo(rootIter, out var relative))
                    {
                        continue;
                    }

                    _reloadQueue.Add(relative.Value);
                }
            });
        }
        #endif
    }
}
