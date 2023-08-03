using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Client.ComponentTrees;

public sealed class SpriteTreeSystem : ComponentTreeSystem<SpriteTreeComponent, SpriteComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpriteComponent, QueueSpriteTreeUpdateEvent>(OnQueueUpdate);
    }

    private void OnQueueUpdate(EntityUid uid, SpriteComponent component, ref QueueSpriteTreeUpdateEvent args)
        => QueueTreeUpdate(uid, component, args.Xform);

    // TODO remove this when finally ECSing sprite components
    [ByRefEvent]
    internal readonly struct QueueSpriteTreeUpdateEvent
    {
        public readonly TransformComponent Xform;
        public QueueSpriteTreeUpdateEvent(TransformComponent xform)
        {
            Xform = xform;
        }
    }

    #region Component Tree Overrides
    protected override bool DoFrameUpdate => true;
    protected override bool DoTickUpdate => false;
    protected override bool Recursive => true;
    protected override int InitialCapacity => 1024;

    protected override Box2 ExtractAabb(in ComponentTreeEntry<SpriteComponent> entry, Vector2 pos, Angle rot)
        => entry.Component.CalculateRotatedBoundingBox(pos, rot, default).CalcBoundingBox();
    #endregion
}
