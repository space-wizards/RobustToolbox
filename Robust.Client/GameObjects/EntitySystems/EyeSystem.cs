using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects;

public sealed class EyeSystem : SharedEyeSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, EyeComponent component, ComponentInit args)
    {
        component._eye = new Eye
        {
            Position = Transform(uid).MapPosition,
            Zoom = component.Zoom,
            DrawFov = component.DrawFov
        };

        // Who even knows if this is needed anymore.
        _eyeManager.ClearCurrentEye();
        _eyeManager.CurrentEye = component._eye;
    }
}
