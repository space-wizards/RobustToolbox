using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Prototypes;

namespace Robust.Server.Prototypes
{
    public sealed class ServerPrototypeManager : PrototypeManager
    {
        private DateTime _timeSinceLastReload = DateTime.Now;
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

        public override void ReloadPrototypes()
        {
#if !FULL_RELEASE
            base.ReloadPrototypes();

            var msg = NetManager.CreateNetMessage<MsgReloadPrototypes>();
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

                watcher.Changed += (_, _) =>
                {
                    TaskManager.RunOnMainThread(() =>
                    {
                        if ((DateTime.Now - _timeSinceLastReload).TotalSeconds < 0.25)
                        {
                            return;
                        }

                        _timeSinceLastReload = DateTime.Now;

                        ReloadPrototypes();
                    });
                };

                watcher.EnableRaisingEvents = true;
                _watchers.Add(watcher);
            }
#endif
        }
    }
}
