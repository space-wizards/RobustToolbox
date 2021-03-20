using System.Diagnostics;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Prototypes;

namespace Robust.Server.Prototypes
{
    public sealed class ServerPrototypeManager : PrototypeManager
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IConGroupController _conGroups = default!;

        public ServerPrototypeManager()
        {
            RegisterIgnore("shader");
        }

        public override void Initialize()
        {
            base.Initialize();

            NetManager.RegisterNetMessage<MsgReloadPrototypes>(MsgReloadPrototypes.NAME, HandleReloadPrototypes, NetMessageAccept.Server);
        }

        private void HandleReloadPrototypes(MsgReloadPrototypes msg)
        {
#if !FULL_RELEASE
            if (!_playerManager.TryGetSessionByChannel(msg.MsgChannel, out var player) ||
                !_conGroups.CanAdminReloadPrototypes(player))
            {
                return;
            }

            var sw = Stopwatch.StartNew();

            ReloadPrototypes(msg.Paths);

            Logger.Info($"Reloaded prototypes in {sw.ElapsedMilliseconds} ms");
#endif
        }

    }
}
