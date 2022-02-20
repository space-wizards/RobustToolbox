using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Server.GameObjects;

public sealed class AppearanceSystem : SharedAppearanceSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ServerAppearanceComponent, ComponentGetState>(OnAppearanceGetState);
    }

    private static void OnAppearanceGetState(EntityUid uid, ServerAppearanceComponent component, ref ComponentGetState args)
    {
        args.State = new AppearanceComponentState(component.AppearanceData);
    }
}
