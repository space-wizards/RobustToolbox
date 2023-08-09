using Robust.Shared.IoC;

namespace Robust.Shared.GameObjects;

public abstract class SharedEyeSystem : EntitySystem
{
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
}
