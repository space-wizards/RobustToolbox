using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Client.GameObjects;

public sealed class EyeSystem : SharedEyeSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<EyeComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnRemove(EntityUid uid, EyeComponent component, ComponentRemove args)
    {
        component.Current = false;
    }

    private void OnHandleState(EntityUid uid, EyeComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not EyeComponentState state)
        {
            return;
        }

        component.DrawFov = state.DrawFov;
        // TODO: Should be a way for content to override lerping and lerp the zoom
        component.Zoom = state.Zoom;
        component.Offset = state.Offset;
        component.VisibilityMask = state.VisibilityMask;
    }
}
