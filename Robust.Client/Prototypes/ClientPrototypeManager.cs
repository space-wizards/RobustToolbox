using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
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

        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly TimeSpan _reloadDelay = TimeSpan.FromMilliseconds(10);
        private CancellationTokenSource _reloadToken = new();
        private readonly HashSet<ResourcePath> _reloadQueue = new();

        public override void Initialize()
        {
            base.Initialize();

            _netManager.RegisterNetMessage<MsgReloadPrototypes>(accept: NetMessageAccept.Server);

            _clyde.OnWindowFocused += WindowFocusedChanged;

            WatchResources();
        }

        private void WindowFocusedChanged(WindowFocusedEventArgs args)
        {
#if !FULL_RELEASE
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
#if !FULL_RELEASE
            var sw = Stopwatch.StartNew();

            var msg = _netManager.CreateNetMessage<MsgReloadPrototypes>();
            msg.Paths = _reloadQueue.ToArray();
            _netManager.ClientSendMessage(msg);

            ReloadPrototypes(_reloadQueue);

            _reloadQueue.Clear();

            Logger.Info($"Reloaded prototypes in {sw.ElapsedMilliseconds} ms");
#endif
        }

        private void WatchResources()
        {
#if !FULL_RELEASE
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
                        var file = new ResourcePath(args.FullPath);

                        foreach (var root in IoCManager.Resolve<IResourceManager>().GetContentRoots())
                        {
                            if (!file.TryRelativeTo(root, out var relative))
                            {
                                continue;
                            }

                            _reloadQueue.Add(relative);
                        }
                    });
                };

                watcher.EnableRaisingEvents = true;
                _watchers.Add(watcher);
            }
#endif
        }
    }
}
