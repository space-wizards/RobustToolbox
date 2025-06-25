using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;
using static Robust.Client.Graphics.RSI;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Robust.Client.GameObjects;

// This partial class contains various public methods for modifying a layer's properties.
public sealed partial class SpriteSystem
{
    #region SetData

    public void LayerSetData(Entity<SpriteComponent?> sprite, int index, PrototypeLayerData data)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetData(layer, data);
    }

    public void LayerSetData(Entity<SpriteComponent?> sprite, string key, PrototypeLayerData data)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetData(layer, data);
    }

    public void LayerSetData(Entity<SpriteComponent?> sprite, Enum key, PrototypeLayerData data)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetData(layer, data);
    }

    public void LayerSetData(Layer layer, PrototypeLayerData data)
    {
        DebugTools.Assert(layer.Owner != default);
        DebugTools.AssertNotNull(layer.Owner.Comp);
        DebugTools.AssertEqual(layer.Owner.Comp.Layers[layer.Index], layer);
        // TODO SPRITE ECS
        layer._parent.LayerSetData(layer, data);
    }

    #endregion

    #region SpriteSpecifier

    public void LayerSetSprite(Entity<SpriteComponent?> sprite, int index, SpriteSpecifier specifier)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetSprite(layer, specifier);
    }

    public void LayerSetSprite(Entity<SpriteComponent?> sprite, string key, SpriteSpecifier specifier)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetSprite(layer, specifier);
    }

    public void LayerSetSprite(Entity<SpriteComponent?> sprite, Enum key, SpriteSpecifier specifier)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetSprite(layer, specifier);
    }

    public void LayerSetSprite(Layer layer, SpriteSpecifier specifier)
    {
        switch (specifier)
        {
            case SpriteSpecifier.Texture tex:
                LayerSetTexture(layer, tex.TexturePath);
                break;

            case SpriteSpecifier.Rsi rsi:
                LayerSetRsi(layer, rsi.RsiPath, rsi.RsiState);
                break;

            default:
                throw new NotImplementedException();
        }
    }

    #endregion

    #region Texture

    public void LayerSetTexture(Entity<SpriteComponent?> sprite, int index, Texture? texture)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetTexture(layer, texture);
    }

    public void LayerSetTexture(Entity<SpriteComponent?> sprite, string key, Texture? texture)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetTexture(layer, texture);
    }

    public void LayerSetTexture(Entity<SpriteComponent?> sprite, Enum key, Texture? texture)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetTexture(layer, texture);
    }

    public void LayerSetTexture(Layer layer, Texture? texture)
    {
        LayerSetRsiState(layer, StateId.Invalid, refresh: true);
        layer.Texture = texture;
    }

    public void LayerSetTexture(Entity<SpriteComponent?> sprite, int index, ResPath path)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetTexture(layer, path);
    }

    public void LayerSetTexture(Entity<SpriteComponent?> sprite, string key, ResPath path)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetTexture(layer, path);
    }

    public void LayerSetTexture(Entity<SpriteComponent?> sprite, Enum key, ResPath path)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetTexture(layer, path);
    }

    private void LayerSetTexture(Layer layer, ResPath path)
    {
        if (!_resourceCache.TryGetResource<TextureResource>(TextureRoot / path, out var texture))
        {
            if (path.Extension == "rsi")
                Log.Error($"Expected texture but got rsi '{path}', did you mean 'sprite:' instead of 'texture:'?");
            Log.Error($"Unable to load texture '{path}'. Trace:\n{Environment.StackTrace}");
        }

        LayerSetTexture(layer, texture?.Texture);
    }

    #endregion

    #region RsiState

    public void LayerSetRsiState(Entity<SpriteComponent?> sprite, int index, StateId state)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetRsiState(layer, state);
    }

    public void LayerSetRsiState(Entity<SpriteComponent?> sprite, string key, StateId state)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetRsiState(layer, state);
    }

    public void LayerSetRsiState(Entity<SpriteComponent?> sprite, Enum key, StateId state)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetRsiState(layer, state);
    }

    public void LayerSetRsiState(Layer layer, StateId state, bool refresh = false)
    {
        DebugTools.Assert(layer.Owner != default);
        DebugTools.AssertNotNull(layer.Owner.Comp);
        DebugTools.AssertEqual(layer.Owner.Comp.Layers[layer.Index], layer);

        if (layer.StateId == state && !refresh)
            return;

        layer.StateId = state;
        RefreshCachedState(layer, true, null);
        _tree.QueueTreeUpdate(layer.Owner);
        QueueUpdateIsInert(layer.Owner);
        layer.BoundsDirty = true;
        layer.Owner.Comp.BoundsDirty = true;
    }

    #endregion

    #region Rsi

    public void LayerSetRsi(Entity<SpriteComponent?> sprite, int index, RSI? rsi, StateId? state = null)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetRsi(layer, rsi, state);
    }

    public void LayerSetRsi(Entity<SpriteComponent?> sprite, string key, RSI? rsi, StateId? state = null)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetRsi(layer, rsi, state);
    }

    public void LayerSetRsi(Entity<SpriteComponent?> sprite, Enum key, RSI? rsi, StateId? state = null)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetRsi(layer, rsi, state);
    }

    public void LayerSetRsi(Layer layer, RSI? rsi, StateId? state = null)
    {
        layer._rsi = rsi;
        LayerSetRsiState(layer, state ?? layer.StateId, refresh: true);
    }

    public void LayerSetRsi(Entity<SpriteComponent?> sprite, int index, ResPath rsi, StateId? state = null)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetRsi(layer, rsi, state);
    }

    public void LayerSetRsi(Entity<SpriteComponent?> sprite, string key, ResPath rsi, StateId? state = null)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetRsi(layer, rsi, state);
    }

    public void LayerSetRsi(Entity<SpriteComponent?> sprite, Enum key, ResPath rsi, StateId? state = null)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetRsi(layer, rsi, state);
    }

    public void LayerSetRsi(Layer layer, ResPath rsi, StateId? state = null)
    {
        if (!_resourceCache.TryGetResource<RSIResource>(TextureRoot / rsi, out var res))
            Log.Error($"Unable to load RSI '{rsi}' for entity {ToPrettyString(layer.Owner)}. Trace:\n{Environment.StackTrace}");

        LayerSetRsi(layer, res?.RSI, state);
    }

    #endregion

    #region Scale

    public void LayerSetScale(Entity<SpriteComponent?> sprite, int index, Vector2 value)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetScale(layer, value);
    }

    public void LayerSetScale(Entity<SpriteComponent?> sprite, string key, Vector2 value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetScale(layer, value);
    }

    public void LayerSetScale(Entity<SpriteComponent?> sprite, Enum key, Vector2 value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetScale(layer, value);
    }

    public void LayerSetScale(Layer layer, Vector2 value)
    {
        DebugTools.Assert(layer.Owner != default);
        DebugTools.AssertNotNull(layer.Owner.Comp);
        DebugTools.AssertEqual(layer.Owner.Comp.Layers[layer.Index], layer);

        if (layer._scale.EqualsApprox(value))
            return;

        if (!ValidateScale(layer.Owner, value))
            return;

        layer._scale = value;
        layer.UpdateLocalMatrix();
        _tree.QueueTreeUpdate(layer.Owner);
        layer.BoundsDirty = true;
        layer.Owner.Comp.BoundsDirty = true;
    }

    #endregion

    #region Rotation

    public void LayerSetRotation(Entity<SpriteComponent?> sprite, int index, Angle value)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetRotation(layer, value);
    }

    public void LayerSetRotation(Entity<SpriteComponent?> sprite, string key, Angle value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetRotation(layer, value);
    }

    public void LayerSetRotation(Entity<SpriteComponent?> sprite, Enum key, Angle value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetRotation(layer, value);
    }

    public void LayerSetRotation(Layer layer, Angle value)
    {
        DebugTools.Assert(layer.Owner != default);
        DebugTools.AssertNotNull(layer.Owner.Comp);
        DebugTools.AssertEqual(layer.Owner.Comp.Layers[layer.Index], layer);

        if (layer._rotation.EqualsApprox(value))
            return;

        layer._rotation = value;
        layer.UpdateLocalMatrix();
        _tree.QueueTreeUpdate(layer.Owner);
        layer.BoundsDirty = true;
        layer.Owner.Comp.BoundsDirty = true;
    }

    #endregion

    #region Offset

    public void LayerSetOffset(Entity<SpriteComponent?> sprite, int index, Vector2 value)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetOffset(layer, value);
    }

    public void LayerSetOffset(Entity<SpriteComponent?> sprite, string key, Vector2 value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetOffset(layer, value);
    }

    public void LayerSetOffset(Entity<SpriteComponent?> sprite, Enum key, Vector2 value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetOffset(layer, value);
    }

    public void LayerSetOffset(Layer layer, Vector2 value)
    {
        DebugTools.Assert(layer.Owner != default);
        DebugTools.AssertNotNull(layer.Owner.Comp);
        DebugTools.AssertEqual(layer.Owner.Comp.Layers[layer.Index], layer);

        if (layer._offset.EqualsApprox(value))
            return;

        layer._offset = value;
        layer.UpdateLocalMatrix();
        _tree.QueueTreeUpdate(layer.Owner);
        layer.BoundsDirty = true;
        layer.Owner.Comp.BoundsDirty = true;
    }

    #endregion

    #region Visible

    public void LayerSetVisible(Entity<SpriteComponent?> sprite, int index, bool value)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetVisible(layer, value);
    }

    public void LayerSetVisible(Entity<SpriteComponent?> sprite, string key, bool value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetVisible(layer, value);
    }

    public void LayerSetVisible(Entity<SpriteComponent?> sprite, Enum key, bool value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetVisible(layer, value);
    }

    public void LayerSetVisible(Layer layer, bool value)
    {
        DebugTools.Assert(layer.Owner != default);
        DebugTools.AssertNotNull(layer.Owner.Comp);
        DebugTools.AssertEqual(layer.Owner.Comp.Layers[layer.Index], layer);

        if (layer._visible == value)
            return;

        layer._visible = value;
        QueueUpdateIsInert(layer.Owner);
        _tree.QueueTreeUpdate(layer.Owner);
        layer.Owner.Comp.BoundsDirty = true;
    }

    #endregion

    #region Color

    public void LayerSetColor(Entity<SpriteComponent?> sprite, int index, Color value)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetColor(layer, value);
    }

    public void LayerSetColor(Entity<SpriteComponent?> sprite, string key, Color value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetColor(layer, value);
    }

    public void LayerSetColor(Entity<SpriteComponent?> sprite, Enum key, Color value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetColor(layer, value);
    }

    public void LayerSetColor(Layer layer, Color value)
    {
        DebugTools.Assert(layer.Owner != default);
        DebugTools.AssertNotNull(layer.Owner.Comp);
        DebugTools.AssertEqual(layer.Owner.Comp.Layers[layer.Index], layer);

        layer.Color = value;
    }

    #endregion

    #region DirOffset

    public void LayerSetDirOffset(Entity<SpriteComponent?> sprite, int index, DirectionOffset value)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetDirOffset(layer, value);
    }

    public void LayerSetDirOffset(Entity<SpriteComponent?> sprite, string key, DirectionOffset value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetDirOffset(layer, value);
    }

    public void LayerSetDirOffset(Entity<SpriteComponent?> sprite, Enum key, DirectionOffset value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetDirOffset(layer, value);
    }

    public void LayerSetDirOffset(Layer layer, DirectionOffset value)
    {
        DebugTools.Assert(layer.Owner != default);
        DebugTools.AssertNotNull(layer.Owner.Comp);
        DebugTools.AssertEqual(layer.Owner.Comp.Layers[layer.Index], layer);

        layer.DirOffset = value;
    }

    #endregion

    #region AnimationTime

    public void LayerSetAnimationTime(Entity<SpriteComponent?> sprite, int index, float value)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetAnimationTime(layer, value);
    }

    public void LayerSetAnimationTime(Entity<SpriteComponent?> sprite, string key, float value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetAnimationTime(layer, value);
    }

    public void LayerSetAnimationTime(Entity<SpriteComponent?> sprite, Enum key, float value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetAnimationTime(layer, value);
    }

    public void LayerSetAnimationTime(Layer layer, float value)
    {
        DebugTools.Assert(layer.Owner != default);
        DebugTools.AssertNotNull(layer.Owner.Comp);
        DebugTools.AssertEqual(layer.Owner.Comp.Layers[layer.Index], layer);

        if (!layer.StateId.IsValid)
            return;

        if (layer.ActualRsi is not { } rsi)
            return;

        var state = rsi[layer.StateId];
        if (value > layer.AnimationTime)
        {
            // Handle advancing differently from going backwards.
            layer.AnimationTimeLeft -= (value - layer.AnimationTime);
        }
        else
        {
            // Going backwards we re-calculate from zero.
            // Definitely possible to optimize this for going backwards but I'm too lazy to figure that out.
            layer.AnimationTimeLeft = -value + state.GetDelay(0);
            layer.AnimationFrame = 0;
        }

        layer.AnimationTime = value;
        layer.AdvanceFrameAnimation(state);
        layer.SetAnimationTime(value);
    }

    #endregion

    #region AutoAnimated

    public void LayerSetAutoAnimated(Entity<SpriteComponent?> sprite, int index, bool value)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetAutoAnimated(layer, value);
    }

    public void LayerSetAutoAnimated(Entity<SpriteComponent?> sprite, string key, bool value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetAutoAnimated(layer, value);
    }

    public void LayerSetAutoAnimated(Entity<SpriteComponent?> sprite, Enum key, bool value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetAutoAnimated(layer, value);
    }

    public void LayerSetAutoAnimated(Layer layer, bool value)
    {
        DebugTools.Assert(layer.Owner != default);
        DebugTools.AssertNotNull(layer.Owner.Comp);
        DebugTools.AssertEqual(layer.Owner.Comp.Layers[layer.Index], layer);

        if (layer._autoAnimated == value)
            return;

        layer._autoAnimated = value;
        QueueUpdateIsInert(layer.Owner);
    }

    #endregion

    #region LayerSetRenderingStrategy

    public void LayerSetRenderingStrategy(Entity<SpriteComponent?> sprite, int index, LayerRenderingStrategy value)
    {
        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetRenderingStrategy(layer, value);
    }

    public void LayerSetRenderingStrategy(Entity<SpriteComponent?> sprite, string key, LayerRenderingStrategy value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetRenderingStrategy(layer, value);
    }

    public void LayerSetRenderingStrategy(Entity<SpriteComponent?> sprite, Enum key, LayerRenderingStrategy value)
    {
        if (TryGetLayer(sprite, key, out var layer, true))
            LayerSetRenderingStrategy(layer, value);
    }

    public void LayerSetRenderingStrategy(Layer layer, LayerRenderingStrategy value)
    {
        DebugTools.Assert(layer.Owner != default);
        DebugTools.AssertNotNull(layer.Owner.Comp);
        DebugTools.AssertEqual(layer.Owner.Comp.Layers[layer.Index], layer);

        layer.RenderingStrategy = value;
        layer.BoundsDirty = true;
        layer.Owner.Comp.BoundsDirty = true;
        _tree.QueueTreeUpdate(layer.Owner);
    }

    #endregion

    /// <summary>
    /// Refreshes an RSI layer's cached RSI state.
    /// </summary>
    private void RefreshCachedState(Layer layer, bool logErrors, RSI.State? fallback)
    {
        if (!layer.StateId.IsValid)
        {
            layer._actualState = null;
        }
        else if (layer.ActualRsi is not { } rsi)
        {
            layer._actualState = fallback ?? GetFallbackState();
            if (logErrors)
                Log.Error(
                    $"{ToPrettyString(layer.Owner)} has no RSI to pull new state from! Trace:\n{Environment.StackTrace}");
        }
        else if (!rsi.TryGetState(layer.StateId, out layer._actualState))
        {
            layer._actualState = fallback ?? GetFallbackState();
            if (logErrors)
                Log.Error(
                    $"{ToPrettyString(layer.Owner)}'s state '{layer.StateId}' does not exist in RSI {rsi.Path}. Trace:\n{Environment.StackTrace}");
        }

        layer.AnimationFrame = 0;
        layer.AnimationTime = 0;
        layer.AnimationTimeLeft = layer._actualState?.GetDelay(0) ?? 0f;
    }
}
