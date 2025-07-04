using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Audio.Components;
using Robust.Shared.IoC;

namespace Robust.Client.ComponentTrees;

/// <summary>
/// ComponentTreeSystem tracking the audible ranges of captioned audio entities
/// </summary>
public sealed class CaptionTreeSystem : ComponentTreeSystem<CaptionTreeComponent, CaptionComponent>
{
    #region Component Tree Overrides
    protected override bool DoFrameUpdate => false;
    protected override bool DoTickUpdate => true;
    protected override bool Recursive => true;
    protected override int InitialCapacity => 128;

    protected override Box2 ExtractAabb(in ComponentTreeEntry<CaptionComponent> entry, Vector2 pos, Angle rot)
    {
        if (TryComp<AudioComponent>(entry.Uid, out var audio))
        {
            var radius = audio.MaxDistance;
            var radiusVec = new Vector2(radius, radius);
            return new Box2(pos - radiusVec, pos + radiusVec);
        }
        return default;
    }

    protected override Box2 ExtractAabb(in ComponentTreeEntry<CaptionComponent> entry)
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
