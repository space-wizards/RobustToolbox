using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Client.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Client.Prototypes
{
    public sealed class ClientPrototypeManager : PrototypeManager
    {
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly IClientGameTiming _timing = default!;
        [Dependency] private readonly IGameControllerInternal _controller = default!;
        [Dependency] private readonly IReloadManager _reload = default!;

        public override void Initialize()
        {
            base.Initialize();

            _netManager.RegisterNetMessage<MsgReloadPrototypes>(accept: NetMessageAccept.Server);

            _reload.Register("/Prototypes", "*.yml");
            _reload.OnChanged += ReloadPrototypeQueue;
        }

        public override void LoadDefaultPrototypes(Dictionary<Type, HashSet<string>>? changed = null)
        {
            LoadDirectory(new("/EnginePrototypes/"), changed: changed);
            LoadDirectory(_controller.Options.PrototypeDirectory, changed: changed);
            ResolveResults();
        }

        private void ReloadPrototypeQueue(ResPath file)
        {
            if (file.Extension != "yml")
                return;

#if TOOLS
            var sw = Stopwatch.StartNew();

            var msg = new MsgReloadPrototypes
            {
                Paths = [file]
            };
            _netManager.ClientSendMessage(msg);

            // Reloading prototypes modifies entities. This currently causes some state management debug asserts to
            // fail. To avoid this, we set `IGameTiming.ApplyingState` to true, even though this isn't really applying a
            // server state.
            using var _ = _timing.StartStateApplicationArea();
            ReloadPrototypes([file]);

            Sawmill.Info($"Reloaded prototypes in {sw.ElapsedMilliseconds} ms");
#endif
        }
    }
}
