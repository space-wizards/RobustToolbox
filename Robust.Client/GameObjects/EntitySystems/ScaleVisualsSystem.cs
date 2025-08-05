using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects;

public sealed class ScaleVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ScaleVisualsComponent, AppearanceChangeEvent>(OnChangeData);
    }

    private void OnChangeData(EntityUid uid, ScaleVisualsComponent component, ref AppearanceChangeEvent ev)
    {
        if (!ev.AppearanceData.TryGetValue(ScaleVisuals.Scale, out var scale) ||
            ev.Sprite == null) return;

        var vecScale = (Vector2)scale;

        // Set it directly because prediction may call this multiple times.
        _sprite.SetScale((uid, ev.Sprite), vecScale);
    }
}
