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
            ReloadPrototypes();
        }

    }
}
