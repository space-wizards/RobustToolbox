using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Robust.Client.GameObjects;

public sealed class UserInterfaceSystem : SharedUserInterfaceSystem
{
    public override void Initialize()
    {
        base.Initialize();
        ProtoManager.PrototypesReloaded += OnProtoReload;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ProtoManager.PrototypesReloaded -= OnProtoReload;
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        var player = Player.LocalEntity;

        if (!UserQuery.TryComp(player, out var userComp))
            return;

        foreach (var uid in userComp.OpenInterfaces.Keys)
        {
            if (!UIQuery.TryComp(uid, out var uiComp))
                continue;

            foreach (var bui in uiComp.ClientOpenInterfaces.Values)
            {
                bui.OnProtoReload(obj);
            }
        }
    }
}
