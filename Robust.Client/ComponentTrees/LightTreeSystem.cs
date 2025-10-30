using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Client.ComponentTrees;

public sealed class LightTreeSystem : ComponentTreeSystem<LightTreeComponent, PointLightComponent>
{
    [Dependency] private readonly PointLightSystem _light = default!;

    #region Component Tree Overrides
    protected override bool DoFrameUpdate => true;
    protected override bool DoTickUpdate => false;
    protected override bool Recursive => true;
    protected override int InitialCapacity => 128;

    protected override void OnCompInit(Entity<PointLightComponent> ent, ref ComponentInit args)
    {
        base.OnCompInit(ent, ref args);
        _light.SetMask(ent.Comp.MaskPath, ent.Comp);
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
            entry.Component.TreeUid.Value);

        return ExtractAabb(in entry, pos, default);
    }
    #endregion
}
