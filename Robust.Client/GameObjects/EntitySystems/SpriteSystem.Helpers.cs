using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.Client.GameObjects;

public sealed partial class SpriteSystem
{
    /// <summary>
    /// Gets an entity's sprite position in world terms.
    /// </summary>
    public Vector2 GetSpriteWorldPosition(Entity<SpriteComponent?, TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp2))
            return Vector2.Zero;

        var (worldPos, worldRot) = _xforms.GetWorldPositionRotation(entity.Owner);

        if (!Resolve(entity, ref entity.Comp1, false))
        {
            return worldPos;
        }

        if (entity.Comp1.NoRotation)
        {
            return worldPos + entity.Comp1.Offset;
        }

        return worldPos + worldRot.RotateVec(entity.Comp1.Rotation.RotateVec(entity.Comp1.Offset));
    }

    /// <summary>
    /// Gets an entity's sprite position in screen coordinates.
    /// </summary>
    public ScreenCoordinates GetSpriteScreenCoordinates(Entity<SpriteComponent?, TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp2))
            return ScreenCoordinates.Invalid;

        var spriteCoords = GetSpriteWorldPosition(entity);
        return _eye.MapToScreen(new MapCoordinates(spriteCoords, entity.Comp2.MapID));
    }
}
