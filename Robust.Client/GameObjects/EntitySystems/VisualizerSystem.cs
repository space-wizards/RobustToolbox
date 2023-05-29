using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects;

/// <summary>
///     An abstract entity system inheritor for systems that deal with appearance data.
/// </summary>
public abstract class VisualizerSystem<T> : EntitySystem
    where T: Component
{
    [Dependency] protected readonly AppearanceSystem AppearanceSystem = default!;
    [Dependency] protected readonly AnimationPlayerSystem AnimationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, AppearanceChangeEvent>(OnAppearanceChange);
    }

    protected virtual void OnAppearanceChange(EntityUid uid, T component, ref AppearanceChangeEvent args) {}
}
