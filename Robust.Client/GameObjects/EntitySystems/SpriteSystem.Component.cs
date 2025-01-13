using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

public sealed partial class SpriteSystem
{
    /// <summary>
    /// Resets the sprite's animated layers to align with a given time (in seconds).
    /// </summary>
    public void SetAutoAnimateSync(SpriteComponent sprite, double time)
    {
        foreach (var layer in sprite.AllLayers)
        {
            if (layer is not SpriteComponent.Layer spriteLayer)
                continue;

            SetAutoAnimateSync(sprite, spriteLayer, time);
        }
    }

    /// <summary>
    /// Resets the layer's animation to align with a given time (in seconds).
    /// </summary>
    public void SetAutoAnimateSync(SpriteComponent sprite, SpriteComponent.Layer layer, double time)
    {
        if (!layer.AutoAnimated)
            return;

        var rsi = layer.RSI ?? sprite.BaseRSI;

        if (rsi == null || !rsi.TryGetState(layer.State, out var state))
        {
            state = GetFallbackState();
        }

        if (!state.IsAnimated)
        {
            return;
        }

        layer.AnimationTimeLeft = (float) -(time % state.TotalDelay);
        layer.AnimationFrame = 0;
    }

    public void CopySprite(Entity<SpriteComponent?> source, Entity<SpriteComponent?> target)
    {
        if (!Resolve(source.Owner, ref source.Comp))
            return;

        if (!Resolve(target.Owner, ref target.Comp))
            return;

        CopySprite(source.Comp, target.Comp);
    }

    public void CopySprite(SpriteComponent source, SpriteComponent target)
    {
        target._baseRsi = source._baseRsi;
        target._bounds = source._bounds;
        target._visible = source._visible;
        target.color = source.color;
        target.offset = source.offset;
        target.rotation = source.rotation;
        target.scale = source.scale;
        target.LocalMatrix = Matrix3Helpers.CreateTransform(in target.offset, in target.rotation, in target.scale);
        target.drawDepth = source.drawDepth;
        target._screenLock = source._screenLock;
        target.DirectionOverride = source.DirectionOverride;
        target.EnableDirectionOverride = source.EnableDirectionOverride;
        target.Layers = new List<SpriteComponent.Layer>(source.Layers.Count);
        foreach (var otherLayer in source.Layers)
        {
            target.Layers.Add(new SpriteComponent.Layer(otherLayer, target));
        }

        target.IsInert = source.IsInert;
        target.LayerMap = source.LayerMap.ShallowClone();
        target.PostShader = source.PostShader is {Mutable: true} ? source.PostShader.Duplicate() : source.PostShader;
        target.RenderOrder = source.RenderOrder;
        target.GranularLayersRendering = source.GranularLayersRendering;
    }

    public void RebuildBounds(Entity<SpriteComponent> sprite)
    {
        // Maybe the bounds calculation should be deferred?
        // The tree update is already deferred anyways.

        var bounds = new Box2();
        foreach (var layer in sprite.Comp.Layers)
        {
            if (layer is {Visible: true, Blank: false})
                bounds = bounds.Union(layer.CalculateBoundingBox());
        }

        sprite.Comp._bounds = bounds.Scale(sprite.Comp.Scale);
        _tree.QueueTreeUpdate(sprite);
    }

    /// <summary>
    /// Adds a sprite to a queue that will update <see cref="SpriteComponent.IsInert"/> next frame.
    /// </summary>
    public void QueueUpdateIsInert(Entity<SpriteComponent> sprite)
    {
        if (sprite.Comp._inertUpdateQueued)
            return;

        sprite.Comp._inertUpdateQueued = true;
        _inertUpdateQueue.Enqueue(sprite);
    }

    [Obsolete("Use QueueUpdateIsInert")]
    public void QueueUpdateInert(EntityUid uid, SpriteComponent sprite) => QueueUpdateIsInert(new (uid, sprite));
}
