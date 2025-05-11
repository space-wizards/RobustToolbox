using System;
using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Robust.Client.GameObjects;

// This partial class contains code related to updating a sprites bounding boxes and its position in the sprite tree.
public sealed partial class SpriteSystem
{
    /// <summary>
    /// Get a sprite's local bounding box. The returned bounds do factor in the sprite's scale but not the rotation or
    /// offset.
    /// </summary>
    public Box2 GetLocalBounds(Entity<SpriteComponent> sprite)
    {
        if (!sprite.Comp.BoundsDirty)
        {
            DebugTools.Assert(sprite.Comp.Layers.All(x => !x.BoundsDirty || !x.Drawn));
            return sprite.Comp._bounds;
        }

        var bounds = new Box2();
        foreach (var layer in sprite.Comp.Layers)
        {
            if (layer.Drawn)
                bounds = bounds.Union(GetLocalBounds(layer));
        }

        sprite.Comp._bounds = bounds;
        sprite.Comp.BoundsDirty = false;
        return sprite.Comp._bounds;
    }

    /// <summary>
    /// Get a layer's local bounding box relative to its owning sprite. Unlike the sprite variant of this method, this
    /// does account for the layer's rotation and offset.
    /// </summary>
    public Box2 GetLocalBounds(Layer layer)
    {
        if (!layer.BoundsDirty)
        {
            DebugTools.Assert(layer.Bounds.EqualsApprox(CalculateLocalBounds(layer)));
            return layer.Bounds;
        }

        layer.Bounds = CalculateLocalBounds(layer);
        layer.BoundsDirty = false;
        return layer.Bounds;
    }

    internal Box2 CalculateLocalBounds(Layer layer)
    {
        var textureSize = (Vector2) layer.PixelSize / EyeManager.PixelsPerMeter;
        var longestSide = MathF.Max(textureSize.X, textureSize.Y);
        var longestRotatedSide = Math.Max(longestSide, (textureSize.X + textureSize.Y) / MathF.Sqrt(2));

        Vector2 size;
        var sprite = layer.Owner.Comp;

        // If this layer has any form of arbitrary rotation, return a bounding box big enough to cover
        // any possible rotation.
        if (layer._rotation != 0)
        {
            size = new Vector2(longestRotatedSide, longestRotatedSide);
            return Box2.CenteredAround(layer.Offset, size * layer._scale);
        }

        var snapToCardinals = sprite.SnapCardinals;
        if (sprite.GranularLayersRendering && layer.RenderingStrategy != LayerRenderingStrategy.UseSpriteStrategy)
        {
            snapToCardinals = layer.RenderingStrategy == LayerRenderingStrategy.SnapToCardinals;
        }

        if (snapToCardinals)
        {
            // Snapping to cardinals only makes sense for 1-directional layers/sprites
            DebugTools.Assert(layer._actualState == null || layer._actualState.RsiDirections == RsiDirectionType.Dir1);

            // We won't know the actual direction it snaps to, so we ahve to assume the box is given by the longest side.
            size = new Vector2(longestSide, longestSide);
            return Box2.CenteredAround(layer.Offset, size * layer._scale);
        }

        // Build the bounding box based on how many directions the sprite has
        size = (layer._actualState?.RsiDirections) switch
        {
            RsiDirectionType.Dir4 => new Vector2(longestSide, longestSide),
            RsiDirectionType.Dir8 => new Vector2(longestRotatedSide, longestRotatedSide),
            _ => textureSize
        };

        return Box2.CenteredAround(layer.Offset, size * layer._scale);
    }

    /// <summary>
    /// Gets a sprite's bounding box in world coordinates.
    /// </summary>
    public Box2Rotated CalculateBounds(Entity<SpriteComponent> sprite, Vector2 worldPos, Angle worldRot, Angle eyeRot)
    {
        // fast check for invisible sprites
        if (!sprite.Comp.Visible || sprite.Comp.Layers.Count == 0)
            return new Box2Rotated(new Box2(worldPos, worldPos), Angle.Zero, worldPos);

        // We need to modify world rotation so that it lies between 0 and 2pi.
        // This matters for 4 or 8 directional sprites deciding which quadrant (octant?) they lie in.
        // the 0->2pi convention is set by the sprite-rendering code that selects the layers.
        // See RenderInternal().

        worldRot = worldRot.Reduced();
        if (worldRot.Theta < 0)
            worldRot = new Angle(worldRot.Theta + Math.Tau);

        // Next, what we do is take the box2 and apply the sprite's transform, and then the entity's transform. We
        // could do this via Matrix3.TransformBox, but that only yields bounding boxes. So instead we manually
        // transform our box by the combination of these matrices:

        var finalRotation = sprite.Comp.NoRotation
            ? sprite.Comp.Rotation - eyeRot
            : sprite.Comp.Rotation + worldRot;

        var bounds = GetLocalBounds(sprite);

        // slightly faster path if offset == 0 (true for 99.9% of sprites)
        if (sprite.Comp.Offset == Vector2.Zero)
            return new Box2Rotated(bounds.Translated(worldPos), finalRotation, worldPos);

        var adjustedOffset = sprite.Comp.NoRotation
            ? (-eyeRot).RotateVec(sprite.Comp.Offset)
            : worldRot.RotateVec(sprite.Comp.Offset);

        var position = adjustedOffset + worldPos;
        return new Box2Rotated(bounds.Translated(position), finalRotation, position);
    }

    private void DirtyBounds(Entity<SpriteComponent> sprite)
    {
        sprite.Comp.BoundsDirty = true;
        foreach (var layer in sprite.Comp.Layers)
        {
            layer.BoundsDirty = true;
        }
    }
}
