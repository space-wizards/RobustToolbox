using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Server.GameObjects;

public sealed class AppearanceSystem : SharedAppearanceSystem
{
    protected override void OnAppearanceGetState(EntityUid uid, AppearanceComponent component, ref ComponentGetState args)
    {
        args.State = new AppearanceComponentState(component.AppearanceData);
    }
}
