using Robust.Shared.IoC;

namespace Robust.Shared.GameObjects;

[InjectDependencies]
public abstract partial class SharedEyeSystem : EntitySystem
{
    [Dependency] protected SharedTransformSystem TransformSystem = default!;
}
