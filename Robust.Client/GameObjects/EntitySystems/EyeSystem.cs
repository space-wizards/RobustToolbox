using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects;

public sealed class EyeSystem : SharedEyeSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EyeComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<EyeComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnInit(EntityUid uid, EyeComponent component, ComponentInit args)
    {
        component._eye = new Eye
        {
            Position = Transform(uid).MapPosition,
            Zoom = component._setZoomOnInitialize,
            DrawFov = component._setDrawFovOnInitialize
        };

        if ((_eyeManager.CurrentEye == component._eye) != component._setCurrentOnInitialize)
        {
            if (component._setCurrentOnInitialize)
            {
                _eyeManager.ClearCurrentEye();
            }
            else
            {
                _eyeManager.CurrentEye = component._eye;
            }
        }
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
