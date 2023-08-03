using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Server.GameObjects;

public sealed class EyeSystem : SharedEyeSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeComponent, ComponentGetState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, EyeComponent component, ref ComponentGetState args)
    {
        args.State = new EyeComponentState(component.DrawFov, component.Zoom, component.Offset, component.VisibilityMask);
    }
}
