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

        SubscribeLocalEvent<T, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<T, AppearanceChangeEvent>(OnAppearanceChange);
    }

    protected abstract void OnComponentInit(EntityUid uid, T component, ComponentInit args);
    protected abstract void OnAppearanceChange(EntityUid uid, T component, ref AppearanceChangeEvent args);
}
