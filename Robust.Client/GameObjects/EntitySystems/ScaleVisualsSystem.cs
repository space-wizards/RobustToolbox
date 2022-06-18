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
            ev.Sprite == null) return;

        var vecScale = (Vector2)scale;

        // Set it directly because prediction may call this multiple times.
        ev.Sprite.Scale = vecScale;
    }
}
