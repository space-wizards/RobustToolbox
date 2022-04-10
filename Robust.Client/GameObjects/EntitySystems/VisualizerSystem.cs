using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects;

/// <summary>
///     An abstract entity system inheritor for systems that deal with
///     appearance data, replacing <see cref="AppearanceVisualizer"/>.
/// </summary>
public abstract class VisualizerSystem<T> : EntitySystem
    where T: Component
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, AppearanceChangeEvent>(OnAppearanceChange);
    }

    protected virtual void OnAppearanceChange(EntityUid uid, T component, ref AppearanceChangeEvent args) {}
}
