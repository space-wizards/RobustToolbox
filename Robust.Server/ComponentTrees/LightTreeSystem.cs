using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.IoC;

namespace Robust.Server.ComponentTrees;

public sealed class LightTreeSystem : ComponentTreeSystem<LightTreeComponent, PointLightComponent>
{
    [Dependency] private readonly PointLightSystem _pointLightSystem = default!;
    
    #region Component Tree Overrides
    protected override bool DoFrameUpdate => false;
    protected override bool DoTickUpdate => true;
    protected override int InitialCapacity => 128;
    protected override bool Recursive => true;

    protected override void OnCompStartup(EntityUid uid, PointLightComponent component, ComponentStartup args)
    {
        base.OnCompStartup(uid, component, args);
        /// This feels like a poor workaround to the fact that Entites can't have duplicate subscriptions to the same event, but it works. 
        _pointLightSystem.OnLightStartup(uid, component, args);
    }

    protected override Box2 ExtractAabb(in ComponentTreeEntry<PointLightComponent> entry, Vector2 pos, Angle rot)
    {
        // Really we should be rotating the light offset by the relative rotation. But I assume the light offset will
        // always be relatively small, so fuck it, this is probably faster than having to compute the angle every time.
        var radius = entry.Component.Radius + entry.Component.Offset.Length();
        var radiusVec = new Vector2(radius, radius);
        return new Box2(pos - radiusVec, pos + radiusVec);
    }

    protected override Box2 ExtractAabb(in ComponentTreeEntry<PointLightComponent> entry)
    {
        if (entry.Component.TreeUid == null)
            return default;

        var pos = XformSystem.GetRelativePosition(
            entry.Transform,
            entry.Component.TreeUid.Value,
            GetEntityQuery<TransformComponent>());

        return ExtractAabb(in entry, pos, default);
    }
    #endregion
}
