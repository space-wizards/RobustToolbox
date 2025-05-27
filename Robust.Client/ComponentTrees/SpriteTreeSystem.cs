using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Client.ComponentTrees;

public sealed class SpriteTreeSystem : ComponentTreeSystem<SpriteTreeComponent, SpriteComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    #region Component Tree Overrides
    protected override bool DoFrameUpdate => true;
    protected override bool DoTickUpdate => false;
    protected override bool Recursive => true;
    protected override int InitialCapacity => 1024;

    protected override Box2 ExtractAabb(in ComponentTreeEntry<SpriteComponent> entry, Vector2 pos, Angle rot)
    {
        // TODO SPRITE optimize this
        // Because the just take the BB of the rotated BB, I'mt pretty sure we do a lot of unnecessary maths.
        return _sprite.CalculateBounds((entry.Uid, entry.Component), pos, rot, default).CalcBoundingBox();
    }

    #endregion
}
