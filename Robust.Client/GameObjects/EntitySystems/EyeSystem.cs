using Robust.Client.Graphics;
using Robust.Client.Physics;
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

        // Make sure this runs *after* entities have been moved by interpolation and movement.
        UpdatesAfter.Add(typeof(TransformSystem));
        UpdatesAfter.Add(typeof(PhysicsSystem));
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

    /// <inheritdoc />
    public override void FrameUpdate(float frameTime)
    {
        var query = AllEntityQuery<EyeComponent>();

        while (query.MoveNext(out var uid, out var eyeComponent))
        {
            if (eyeComponent._eye == null)
                continue;

            if (!TryComp<TransformComponent>(eyeComponent.Target, out var xform))
            {
                xform = Transform(uid);
                eyeComponent.Target = null;
            }

            eyeComponent._eye.Position = xform.MapPosition;
        }
    }
}
