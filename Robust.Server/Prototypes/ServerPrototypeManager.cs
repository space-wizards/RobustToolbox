using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Server.Prototypes
{
    public sealed class ServerPrototypeManager : PrototypeManager
    {
        private readonly List<FileSystemWatcher> _watchers = new();

        public ServerPrototypeManager() : base()
        {
            RegisterIgnore("shader");
        }

        public override void Initialize()
        {
            base.Initialize();

            WatchResources();
            NetManager.RegisterNetMessage<MsgReloadPrototypes>(MsgReloadPrototypes.NAME,
                accept: NetMessageAccept.Client);
        }

        public override void ReloadPrototypes(ResourcePath file)
        {
#if !FULL_RELEASE
            base.ReloadPrototypes(file);

            var msg = NetManager.CreateNetMessage<MsgReloadPrototypes>();
            msg.Path = file;
            NetManager.ServerSendToAll(msg);
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
                        var then = DateTime.Now;
                        var file = new ResourcePath(args.FullPath);

                        foreach (var root in IoCManager.Resolve<IResourceManager>().GetContentRoots())
                        {
                            if (!file.TryRelativeTo(root, out var relative))
                            {
                                continue;
                            }

                            ReloadPrototypes(relative);
                        }

                        Logger.Info($"Reloaded prototypes in {(int) (DateTime.Now - then).TotalMilliseconds} ms");
                    });
                };

                watcher.EnableRaisingEvents = true;
                _watchers.Add(watcher);
            }
#endif
        }
    }
}
