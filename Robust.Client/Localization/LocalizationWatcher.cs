using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Robust.Client.Console;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Robust.Client.Localization;

internal sealed class LocalizationWatcher : IPostInjectInit
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IClientConsoleHost _console = default!;
    [Dependency] private readonly IResourceManagerInternal _resources = default!;

    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly TimeSpan _reloadDelay = TimeSpan.FromMilliseconds(10);
    private CancellationTokenSource _reloadToken = new();
    private bool _shouldReload;

    public void PostInject()
    {
        _clyde.OnWindowFocused += WindowFocused;
        _resources.RootAdded += RootAdded;
    }

    private void WindowFocused(WindowFocusedEventArgs args)
    {
#if !FULL_RELEASE
        if (args.Focused && _shouldReload)
        {
            Timer.Spawn(_reloadDelay, ReloadLocalization, _reloadToken.Token);
        }
        else
        {
            _reloadToken.Cancel();
            _reloadToken = new CancellationTokenSource();
        }
#endif
    }

    private void RootAdded(IContentRoot root)
    {
#if !FULL_RELEASE
        if (root is not IDirRoot dir)
        {
            return;
        }

        Watch(dir.FullPath);
#endif
    }

    private void ReloadLocalization()
    {
#if !FULL_RELEASE
        _shouldReload = false;

        var sw = new Stopwatch();
        sw.Start();

        _console.ExecuteCommand("rldloc");

        Logger.Info($"Reloaded localization in {sw.Elapsed.TotalMilliseconds} ms");
#endif
    }

    private void Watch(string path)
    {
#if !FULL_RELEASE
        var watcher = new FileSystemWatcher(path, "*.ftl")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite
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
                    throw new ArgumentOutOfRangeException();
            }

            _shouldReload = true;
        };

        watcher.EnableRaisingEvents = true;
        _watchers.Add(watcher);
#endif
    }
}
