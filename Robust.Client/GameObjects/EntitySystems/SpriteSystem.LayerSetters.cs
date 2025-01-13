using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;
using static Robust.Client.Graphics.RSI;
using static Robust.Shared.Serialization.TypeSerializers.Implementations.SpriteSpecifierSerializer;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Robust.Client.GameObjects;

// This partial class contains various public methods for modifying a layer's properties.
public sealed partial class SpriteSystem
{
    #region SetData

    public void LayerSetData(Entity<SpriteComponent?> sprite, int index, PrototypeLayerData data)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetData(layer, data);
    }

    public void LayerSetData(Layer layer, PrototypeLayerData data)
    {
        // TODO SPRITE store layer index.
        var index = layer._parent.Layers.IndexOf(layer);

        // TODO SPRITE ECS
        layer._parent.LayerSetData(layer, index, data);
    }

    public void LayerSetData(Entity<SpriteComponent?> sprite, string key, PrototypeLayerData data)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetData(sprite, index, data);
    }

    public void LayerSetData(Entity<SpriteComponent?> sprite, Enum key, PrototypeLayerData data)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetData(sprite, index, data);
    }

    #endregion

    #region SpriteSpecifier

    public void LayerSetSprite(Entity<SpriteComponent?> sprite, int index, SpriteSpecifier specifier)
    {
        switch (specifier)
        {
            case SpriteSpecifier.Texture tex:
                LayerSetTexture(sprite, index, tex.TexturePath);
                break;

            case SpriteSpecifier.Rsi rsi:
                LayerSetRsi(sprite, index, rsi.RsiPath, rsi.RsiState);
                break;

            default:
                throw new NotImplementedException();
        }
    }

    public void LayerSetSprite(Entity<SpriteComponent?> sprite, string key, SpriteSpecifier specifier)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetSprite(sprite, index, specifier);
    }

    public void LayerSetSprite(Entity<SpriteComponent?> sprite, Enum key, SpriteSpecifier specifier)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetSprite(sprite, index, specifier);
    }

    #endregion

    #region Texture

    public void LayerSetTexture(Entity<SpriteComponent?> sprite, int index, Texture? texture)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetTexture(layer, texture);
    }

    public void LayerSetTexture(Layer layer, Texture? texture)
    {
        LayerSetRsiState(layer, StateId.Invalid, refresh: true);
        layer.Texture = texture;
    }

    public void LayerSetTexture(Entity<SpriteComponent?> sprite, string key, Texture? texture)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetTexture(sprite, index, texture);
    }

    public void LayerSetTexture(Entity<SpriteComponent?> sprite, Enum key, Texture? texture)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetTexture(sprite, index, texture);
    }

    public void LayerSetTexture(Entity<SpriteComponent?> sprite, int index, ResPath path)
    {
        if (!_resourceCache.TryGetResource<TextureResource>(TextureRoot / path, out var texture))
        {
            if (path.Extension == "rsi")
                Log.Error($"Expected texture but got rsi '{path}', did you mean 'sprite:' instead of 'texture:'?");
            Log.Error($"Unable to load texture '{path}'. Trace:\n{Environment.StackTrace}");
        }

        LayerSetTexture(sprite, index, texture?.Texture);
    }

    public void LayerSetTexture(Entity<SpriteComponent?> sprite, string key, ResPath texture)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetTexture(sprite, index, texture);
    }

    public void LayerSetTexture(Entity<SpriteComponent?> sprite, Enum key, ResPath texture)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetTexture(sprite, index, texture);
    }

    #endregion

    #region RsiState

    public void LayerSetRsiState(Entity<SpriteComponent?> sprite, int index, StateId state)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetRsiState(layer, state);
    }

    public void LayerSetRsiState(Layer layer, StateId state, bool refresh = false)
    {
        if (layer.StateId == state && !refresh)
            return;

        layer.StateId = state;

        if (!layer.StateId.IsValid)
        {
            layer._actualState = null;
        }
        else if (layer.ActualRsi is not {} rsi)
        {
            Log.Error($"{ToPrettyString(layer.Owner)} has no RSI to pull new state from! Trace:\n{Environment.StackTrace}");
            layer._actualState = GetFallbackState();
        }
        else if (!rsi.TryGetState(layer.StateId, out layer._actualState))
        {
            layer._actualState = GetFallbackState();
            Log.Error($"{ToPrettyString(layer.Owner)}'s state '{state}' does not exist in RSI {rsi.Path}. Trace:\n{Environment.StackTrace}");
        }

        layer.AnimationFrame = 0;
        layer.AnimationTime = 0;
        layer.AnimationTimeLeft = layer._actualState?.GetDelay(0) ?? 0f;

        RebuildBounds(layer.Owner);
        QueueUpdateIsInert(layer.Owner);
    }

    public void LayerSetRsiState(Entity<SpriteComponent?> sprite, string key, StateId state)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetRsiState(sprite, index, state);
    }

    public void LayerSetRsiState(Entity<SpriteComponent?> sprite, Enum key, StateId state)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetRsiState(sprite, index, state);
    }

    #endregion

    #region Rsi

    public void LayerSetRsi(Entity<SpriteComponent?> sprite, int index, RSI? rsi, StateId? state = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (!TryGetLayer(sprite, index, out var layer, true))
            return;

        layer._rsi = rsi;
        LayerSetRsiState(layer, state ?? layer.StateId, refresh: true);
    }

    public void LayerSetRsi(Entity<SpriteComponent> sprite, Layer layer, RSI? rsi, StateId? state = null)
    {
        layer._rsi = rsi;
        LayerSetRsiState(layer, state ?? layer.StateId, refresh: true);
    }

    public void LayerSetRsi(Entity<SpriteComponent?> sprite, string key, RSI? rsi, StateId? state = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetRsi(sprite, index, rsi, state);
    }

    public void LayerSetRsi(Entity<SpriteComponent?> sprite, Enum key, RSI? rsi, StateId? state = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetRsi(sprite, index, rsi, state);
    }

    public void LayerSetRsi(Entity<SpriteComponent?> sprite, int index, ResPath rsi, StateId? state = null)
    {
        if (!_resourceCache.TryGetResource<RSIResource>(TextureRoot / rsi, out var res))
            Log.Error($"Unable to load RSI '{rsi}' for entity {ToPrettyString(sprite)}. Trace:\n{Environment.StackTrace}");

        LayerSetRsi(sprite, index, res?.RSI, state);
    }

    public void LayerSetRsi(Entity<SpriteComponent?> sprite, string key, ResPath rsi, StateId? state = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetRsi(sprite, index, rsi, state);
    }

    public void LayerSetRsi(Entity<SpriteComponent?> sprite, Enum key, ResPath rsi, StateId? state = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetRsi(sprite, index, rsi, state);
    }

    #endregion

    #region Scale

    public void LayerSetScale(Entity<SpriteComponent?> sprite, int index, Vector2 value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetScale(layer, value);
    }

    public void LayerSetScale(Layer layer, Vector2 value)
    {
        if (layer._scale.EqualsApprox(value))
            return;

        if (!ValidateScale(layer.Owner, value))
            return;

        layer._scale = value;
        layer.UpdateLocalMatrix();
        RebuildBounds(layer.Owner);
    }

    public void LayerSetScale(Entity<SpriteComponent?> sprite, string key, Vector2 value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetScale(sprite, index, value);
    }

    public void LayerSetScale(Entity<SpriteComponent?> sprite, Enum key, Vector2 value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetScale(sprite, index, value);
    }

    #endregion

    #region Rotation

    public void LayerSetRotation(Entity<SpriteComponent?> sprite, int index, Angle value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetRotation(layer, value);
    }

    public void LayerSetRotation(Layer layer, Angle value)
    {
        if (layer._rotation.EqualsApprox(value))
            return;

        layer._rotation = value;
        layer.UpdateLocalMatrix();
        RebuildBounds(layer.Owner);
    }

    public void LayerSetRotation(Entity<SpriteComponent?> sprite, string key, Angle value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetRotation(sprite, index, value);
    }

    public void LayerSetRotation(Entity<SpriteComponent?> sprite, Enum key, Angle value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetRotation(sprite, index, value);
    }

    #endregion

    #region Offset

    public void LayerSetOffset(Entity<SpriteComponent?> sprite, int index, Vector2 value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetOffset(layer, value);
    }

    public void LayerSetOffset(Layer layer, Vector2 value)
    {
        if (layer._offset.EqualsApprox(value))
            return;

        layer._offset = value;
        layer.UpdateLocalMatrix();
        RebuildBounds(layer.Owner);
    }

    public void LayerSetOffset(Entity<SpriteComponent?> sprite, string key, Vector2 value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetOffset(sprite, index, value);
    }

    public void LayerSetOffset(Entity<SpriteComponent?> sprite, Enum key, Vector2 value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetOffset(sprite, index, value);
    }

    #endregion

    #region Visible

    public void LayerSetVisible(Entity<SpriteComponent?> sprite, int index, bool value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetVisible(layer, value);
    }

    public void LayerSetVisible(Layer layer, bool value)
    {
        if (layer._visible == value)
            return;

        layer._visible = value;
        QueueUpdateIsInert(layer.Owner);
        RebuildBounds(layer.Owner);
    }

    public void LayerSetVisible(Entity<SpriteComponent?> sprite, string key, bool value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetVisible(sprite, index, value);
    }

    public void LayerSetVisible(Entity<SpriteComponent?> sprite, Enum key, bool value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetVisible(sprite, index, value);
    }

    #endregion

    #region Color

    public void LayerSetColor(Entity<SpriteComponent?> sprite, int index, Color value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (!TryGetLayer(sprite, index, out var layer, true))
            return;

        LayerSetColor(layer, value);
        layer.Color = value;
    }

    public void LayerSetColor(Layer layer, Color value)
    {
        //Yes this is trivial, but this is here mainly for future proofing.
        layer.Color = value;
    }

    public void LayerSetColor(Entity<SpriteComponent?> sprite, string key, Color value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetColor(sprite, index, value);
    }

    public void LayerSetColor(Entity<SpriteComponent?> sprite, Enum key, Color value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetColor(sprite, index, value);
    }

    #endregion

    #region DirOffset

    public void LayerSetDirOffset(Entity<SpriteComponent?> sprite, int index, DirectionOffset value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetDirOffset(layer, value);
    }

    public void LayerSetDirOffset(Layer layer, DirectionOffset value)
    {
        //Yes this is trivial, but this is here mainly for future proofing.
        layer.DirOffset = value;
    }

    public void LayerSetDirOffset(Entity<SpriteComponent?> sprite, string key, DirectionOffset value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetDirOffset(sprite, index, value);
    }

    public void LayerSetDirOffset(Entity<SpriteComponent?> sprite, Enum key, DirectionOffset value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetDirOffset(sprite, index, value);
    }

    #endregion

    #region AnimationTime

    public void LayerSetAnimationTime(Entity<SpriteComponent?> sprite, int index, float value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetAnimationTime(layer, value);
    }

    public void LayerSetAnimationTime(Layer layer, float value)
    {
        if (!layer.StateId.IsValid)
            return;

        if (layer.ActualRsi is not {} rsi)
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

    public void LayerSetAnimationTime(Entity<SpriteComponent?> sprite, string key, float value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetAnimationTime(sprite, index, value);
    }

    public void LayerSetAnimationTime(Entity<SpriteComponent?> sprite, Enum key, float value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetAnimationTime(sprite, index, value);
    }

    #endregion

    #region AutoAnimated

    public void LayerSetAutoAnimated(Entity<SpriteComponent?> sprite, int index, bool value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetAutoAnimated(layer, value);
    }

    public void LayerSetAutoAnimated(Layer layer, bool value)
    {
        if (layer._autoAnimated == value)
            return;

        layer._autoAnimated = value;
        QueueUpdateIsInert(layer.Owner);
    }

    public void LayerSetAutoAnimated(Entity<SpriteComponent?> sprite, string key, bool value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetAutoAnimated(sprite, index, value);
    }

    public void LayerSetAutoAnimated(Entity<SpriteComponent?> sprite, Enum key, bool value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetAutoAnimated(sprite, index, value);
    }

    #endregion

    #region LayerSetRenderingStrategy

    public void LayerSetRenderingStrategy(Entity<SpriteComponent?> sprite, int index, LayerRenderingStrategy value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetRenderingStrategy(layer, value);
    }

    public void LayerSetRenderingStrategy(Layer layer, LayerRenderingStrategy value)
    {
        layer.RenderingStrategy = value;
    }

    public void LayerSetRenderingStrategy(Entity<SpriteComponent?> sprite, string key, LayerRenderingStrategy value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetRenderingStrategy(sprite, index, value);
    }

    public void LayerSetRenderingStrategy(Entity<SpriteComponent?> sprite, Enum key, LayerRenderingStrategy value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetRenderingStrategy(sprite, index, value);
    }

    #endregion
}
