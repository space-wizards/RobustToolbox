using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using TerraFX.Interop.Windows;
using Timer = Robust.Shared.Timing.Timer;

namespace Robust.Client.Utility;

internal sealed class ReloadManager : IReloadManager
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logMan = default!;
    [Dependency] private readonly IResourceManagerInternal _res = default!;
#pragma warning disable CS0414
    [Dependency] private readonly ITaskManager _tasks = default!;
#pragma warning restore CS0414

    private readonly TimeSpan _reloadDelay = TimeSpan.FromMilliseconds(10);
    private CancellationTokenSource _reloadToken = new();
    private readonly HashSet<ResPath> _reloadQueue = new();
    private List<FileSystemWatcher> _watchers = new(); // this list is never used but needed to prevent them from being garbage collected
    private ResourceManifestData _manifest = ResourceManifestData.Default; // cache it

    public event Action<ResPath>? OnChanged;

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = _logMan.GetSawmill("reload");
        _clyde.OnWindowFocused += WindowFocusedChanged;
        _manifest = ResourceManifestData.LoadResourceManifest(_res);
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
            if (!_res.ContentFileExists(file))
                continue;

            _sawmill.Info($"Reloading {file}");
            OnChanged?.Invoke(file);
        }

        _reloadQueue.Clear();
    }

    public void Register(ResPath directory, string filter)
    {
        Register(directory.ToRelativeSystemPath(), filter);
    }

    private void ResolveDirectory()
    {

    }

    public void Register(string directory, string filter)
    {
        if (!_cfg.GetCVar(CVars.ResPrototypeReloadWatch))
            return;

#if TOOLS
        foreach (var root in _res.GetContentRoots())
        {
            var path = Path.Join(root, directory);

            if (!Directory.Exists(path))
            {
                continue;
            }

            var watcher = new FileSystemWatcher(path, filter)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite
            };

            _watchers.Add(watcher); // prevent garbage collection

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
                foreach (var rootIter in _res.GetContentRoots())
                {
                    var relPath = Path.GetRelativePath(rootIter, args.FullPath);
                    if (relPath == args.FullPath)
                    {
                        // Different root (i.e., "C:/" and "D:/")
                        continue;
                    }
                    if (relPath.Contains(".."))
                        continue;
                    var file = ResPath.FromRelativeSystemPath(relPath).ToRootedPath();
                    if (!file.CanonPath.Contains("/../"))
                        _reloadQueue.Add(file);
                    var path = ResolveModularPath(file, rootIter);
                    _reloadQueue.Add(path);
                }
            });
        }
#endif
    }

    private ResPath ResolveModularPath(ResPath relative, string rootIter)
    {
        var finalPath = relative;
        var rootName = new DirectoryInfo(rootIter).Name;

        if (_manifest.ModularResources == null)
            return finalPath;

        foreach (var (vfsPath, diskPath) in _manifest.ModularResources)
        {
            if (diskPath != rootName) continue;
            finalPath = new ResPath(vfsPath) / relative;
            break;
        }
        return finalPath;
    }
}
