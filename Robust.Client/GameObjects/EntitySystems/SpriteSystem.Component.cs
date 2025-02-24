using System;
using System.Collections.Generic;
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

        target.Comp._baseRsi = source.Comp._baseRsi;
        target.Comp._bounds = source.Comp._bounds;
        target.Comp._visible = source.Comp._visible;
        target.Comp.color = source.Comp.color;
        target.Comp.offset = source.Comp.offset;
        target.Comp.rotation = source.Comp.rotation;
        target.Comp.scale = source.Comp.scale;
        target.Comp.LocalMatrix = Matrix3Helpers.CreateTransform(
            in target.Comp.offset,
            in target.Comp.rotation,
            in target
            .Comp.scale);

        target.Comp.drawDepth = source.Comp.drawDepth;
        target.Comp.NoRotation = source.Comp.NoRotation;
        target.Comp.DirectionOverride = source.Comp.DirectionOverride;
        target.Comp.EnableDirectionOverride = source.Comp.EnableDirectionOverride;
        target.Comp.Layers = new List<SpriteComponent.Layer>(source.Comp.Layers.Count);
        foreach (var otherLayer in source.Comp.Layers)
        {
            var layer = new SpriteComponent.Layer(otherLayer, target.Comp);
            layer.Index = target.Comp.Layers.Count;
            layer.Owner = target!;
            target.Comp.Layers.Add(layer);
        }

        target.Comp.IsInert = source.Comp.IsInert;
        target.Comp.LayerMap = source.Comp.LayerMap.ShallowClone();
        target.Comp.PostShader = source.Comp.PostShader is {Mutable: true}
            ? source.Comp.PostShader.Duplicate()
            : source.Comp.PostShader;

        target.Comp.RenderOrder = source.Comp.RenderOrder;
        target.Comp.GranularLayersRendering = source.Comp.GranularLayersRendering;

        DirtyBounds(target!);
        _tree.QueueTreeUpdate(target!);
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
