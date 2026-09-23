using Robust.Server.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;

namespace Robust.Server.ComponentTrees;

public sealed class LightTreeSystem : SharedLightTreeSystem
{
    public override void Initialize()
    {
        if (!Enabled)
            return;

        base.Initialize();

        // TODO LIGHT move PointLightComponent to shared
        Query = EntityManager.GetEntityQuery<SharedPointLightComponent, PointLightComponent>();
    }
}
