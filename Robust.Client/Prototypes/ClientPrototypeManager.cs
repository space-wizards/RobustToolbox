﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Robust.Client.Graphics;
using Robust.Client.Timing;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Robust.Client.Prototypes
{
    public sealed class ClientPrototypeManager : PrototypeManager
    {
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly IClientGameTiming _timing = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IGameControllerInternal _controller = default!;

        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly TimeSpan _reloadDelay = TimeSpan.FromMilliseconds(10);
        private CancellationTokenSource _reloadToken = new();
        private readonly HashSet<ResPath> _reloadQueue = new();

        public override void Initialize()
        {
            base.Initialize();

            _netManager.RegisterNetMessage<MsgReloadPrototypes>(accept: NetMessageAccept.Server);

            _clyde.OnWindowFocused += WindowFocusedChanged;

            WatchResources();
        }

        public override void LoadDefaultPrototypes(Dictionary<Type, HashSet<string>>? changed = null)
        {
            LoadDirectory(new("/EnginePrototypes/"), changed: changed);
            LoadDirectory(_controller.Options.PrototypeDirectory, changed: changed);
            ResolveResults();
        }

        private void WindowFocusedChanged(WindowFocusedEventArgs args)
        {
#if TOOLS
            if (args.Focused && _reloadQueue.Count > 0)
            {
                Timer.Spawn(_reloadDelay, ReloadPrototypeQueue, _reloadToken.Token);
            }
            else
            {
                _reloadToken.Cancel();
                _reloadToken = new CancellationTokenSource();
            }
#endif
        }

        private void ReloadPrototypeQueue()
        {
#if TOOLS
            var sw = Stopwatch.StartNew();

            var msg = new MsgReloadPrototypes();
            msg.Paths = _reloadQueue.ToArray();
            _netManager.ClientSendMessage(msg);

            // Reloading prototypes modifies entities. This currently causes some state management debug asserts to
            // fail. To avoid this, we set `IGameTiming.ApplyingState` to true, even though this isn't really applying a
            // server state.
            using var _ = _timing.StartStateApplicationArea();
            ReloadPrototypes(_reloadQueue);

            _reloadQueue.Clear();

            Logger.Info($"Reloaded prototypes in {sw.ElapsedMilliseconds} ms");
#endif
        }

        private void WatchResources()
        {
            if (!_cfg.GetCVar(CVars.ResPrototypeReloadWatch))
                return;

#if TOOLS
            foreach (var path in Resources.GetContentRoots().Select(r => r.ToString())
                .Where(r => Directory.Exists(r + "/Prototypes")).Select(p => p + "/Prototypes"))
            {
                var watcher = new FileSystemWatcher(path, "*.yml")
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
                        // case WatcherChangeTypes.Deleted:
                        case WatcherChangeTypes.Changed:
                        case WatcherChangeTypes.All:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    TaskManager.RunOnMainThread(() =>
                    {
                        var file = new ResPath(args.FullPath);

                        foreach (var root in Resources.GetContentRoots())
                        {
                            if (!file.TryRelativeTo(root, out var relative))
                            {
                                continue;
                            }

                            _reloadQueue.Add(relative.Value);
                        }
                    });
                };

                try
                {
                    watcher.EnableRaisingEvents = true;
                    _watchers.Add(watcher);
                }
                catch (IOException ex)
                {
                    Logger.Error($"Watching resources in path {path} threw an exception:\n{ex}");
                }
            }
#endif
        }
    }
}
