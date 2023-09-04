using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects;

/// <summary>
///     An abstract entity system inheritor for systems that deal with appearance data.
/// </summary>
[InjectDependencies]
public abstract partial class VisualizerSystem<T> : EntitySystem
    where T: Component
{
    [Dependency] protected AppearanceSystem AppearanceSystem = default!;
    [Dependency] protected AnimationPlayerSystem AnimationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, AppearanceChangeEvent>(OnAppearanceChange);
    }

    protected virtual void OnAppearanceChange(EntityUid uid, T component, ref AppearanceChangeEvent args) {}
}
