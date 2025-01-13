using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects;

// This partial class contains various public methods for setting sprite component data.
public sealed partial class SpriteSystem
{
    private bool ValidateScale(EntityUid uid, Vector2 scale)
    {
        if (!(MathF.Abs(scale.X) < 0.005f) && !(MathF.Abs(scale.Y) < 0.005f))
            return true;

        // Scales of ~0.0025 or lower can lead to singular matrices due to rounding errors.
        Log.Error(
            $"Attempted to set layer sprite scale to very small values. Entity: {ToPrettyString(uid)}. Scale: {scale}");

        return false;
    }

    #region Transform
    public void SetScale(Entity<SpriteComponent?> sprite, Vector2 value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (!ValidateScale(sprite.Owner, value))
            return;

        sprite.Comp._bounds = sprite.Comp._bounds.Scale(value / sprite.Comp.scale);
        sprite.Comp.scale = value;
        sprite.Comp.LocalMatrix = Matrix3Helpers.CreateTransform(
            in sprite.Comp.offset,
            in sprite.Comp.rotation,
            in sprite.Comp.scale);
    }

    public void SetRotation(Entity<SpriteComponent?> sprite, Angle value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        sprite.Comp.rotation = value;
        sprite.Comp.LocalMatrix = Matrix3Helpers.CreateTransform(
            in sprite.Comp.offset,
            in sprite.Comp.rotation,
            in sprite.Comp.scale);
    }

    public void SetOffset(Entity<SpriteComponent?> sprite, Vector2 value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        sprite.Comp.offset = value;
        sprite.Comp.LocalMatrix = Matrix3Helpers.CreateTransform(
            in sprite.Comp.offset,
            in sprite.Comp.rotation,
            in sprite.Comp.scale);
    }
    #endregion

    public void SetVisible(Entity<SpriteComponent?> sprite, bool value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (sprite.Comp.Visible == value)
            return;

        sprite.Comp._visible = value;
        if (!sprite.Comp.TreeUpdateQueued)
            _tree.QueueTreeUpdate(sprite!);
    }

    public void SetDrawDepth(Entity<SpriteComponent?> sprite, int value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        sprite.Comp.drawDepth = value;
    }

    public void SetColor(Entity<SpriteComponent?> sprite, Color value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        sprite.Comp.color = value;
    }

    public void SetBaseRsi(Entity<SpriteComponent?> sprite, RSI? value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (value == sprite.Comp._baseRsi)
            return;

        sprite.Comp._baseRsi = value;
        if (value == null)
            return;

        for (var i = 0; i < sprite.Comp.Layers.Count; i++)
        {
            var layer = sprite.Comp.Layers[i];
            if (!layer.State.IsValid || layer.RSI != null)
                continue;

            layer.UpdateActualState();

            if (value.TryGetState(layer.State, out var state))
            {
                layer.AnimationTimeLeft = state.GetDelay(0);
            }
            else
            {
                Log.Error($"Layer {i} no longer has state '{layer.State}' due to base RSI change. Trace:\n{Environment.StackTrace}");
                layer.Texture = null;
            }
        }
    }

    public void SetContainerOccluded(Entity<SpriteComponent?> sprite, bool value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        sprite.Comp._containerOccluded = value;
        _tree.QueueTreeUpdate(sprite!);
    }

    public void SetShader(Entity<SpriteComponent?> sprite, ShaderInstance? shader)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        sprite.Comp.PostShader = shader;
    }

    public void SetSnapCardinals(Entity<SpriteComponent?> sprite, bool value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (value == sprite.Comp._snapCardinals)
            return;

        sprite.Comp._snapCardinals = value;
        RebuildBounds(sprite!);
    }
}
