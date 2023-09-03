using Robust.Shared.IoC;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedEyeSystem : EntitySystem
{
    [Dependency] protected SharedTransformSystem TransformSystem = default!;
}
