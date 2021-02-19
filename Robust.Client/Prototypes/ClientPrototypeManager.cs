using System;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Prototypes;

namespace Robust.Client.Prototypes
{
    public sealed class ClientPrototypeManager : PrototypeManager
    {
        public override void Initialize()
        {
            base.Initialize();

            NetManager.RegisterNetMessage<MsgReloadPrototypes>(MsgReloadPrototypes.NAME, HandleReloadPrototypes, NetMessageAccept.Client);
        }

        private void HandleReloadPrototypes(MsgReloadPrototypes msg)
        {
            var then = DateTime.Now;

            ReloadPrototypes(msg.Path);

            Logger.Info($"Reloaded prototypes in {(int) (DateTime.Now - then).TotalMilliseconds} ms");
        }
    }
}
