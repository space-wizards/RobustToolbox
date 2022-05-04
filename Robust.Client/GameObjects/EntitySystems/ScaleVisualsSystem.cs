using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects;

public sealed class ScaleVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AppearanceChangeEvent>(OnChangeData);
    }

    private void OnChangeData(ref AppearanceChangeEvent ev)
    {
        if (!ev.AppearanceData.TryGetValue(ScaleVisuals.Scale, out var scale) ||
            !TryComp<SpriteComponent>(ev.Component.Owner, out var spriteComponent)) return;

        var floatScale = (float) scale;

        // Set it directly because prediction may call this multiple times.
        spriteComponent.Scale = new Vector2(floatScale, floatScale);
    }
}
