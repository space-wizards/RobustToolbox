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
    #region LayerSetData

    public void LayerSetData(Entity<SpriteComponent?> sprite, int index, PrototypeLayerData data)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (TryGetLayer(sprite, index, out var layer, true))
            LayerSetData(sprite!, layer, index, data);
    }

    internal void LayerSetData(Entity<SpriteComponent> sprite, Layer layer, int index, PrototypeLayerData data)
    {
        DebugTools.AssertEqual(sprite.Comp.Layers[index], layer);
        sprite.Comp.LayerSetData(layer, index, data);
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

    #region LayerSetSprite

    public void LayerSetSprite(Entity<SpriteComponent?> sprite, int index, SpriteSpecifier specifier)
    {
        switch (specifier)
        {
            case SpriteSpecifier.Texture tex:
                LayerSetTexture(sprite, index, tex.TexturePath);
                break;
            case SpriteSpecifier.Rsi rsi:
                //LayerSetState(sprite, layer, rsi.RsiState, rsi.RsiPath);
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

    #region LayerSetTexture

    public void LayerSetTexture(Entity<SpriteComponent?> sprite, int index, Texture? texture)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (!TryGetLayer(sprite, index, out var layer, true))
            return;

        layer.State = default;
        layer.Texture = texture;
        QueueUpdateIsInert(sprite!);
        RebuildBounds(sprite!);
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

    #region LayerSetRsiState

    public void LayerSetRsiState(Entity<SpriteComponent?> sprite, int index, StateId state)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (!TryGetLayer(sprite, index, out var layer, true))
            return;

        LayerSetRsiState(sprite!, layer, state);
    }

    public void LayerSetRsiState(Entity<SpriteComponent> sprite, Layer layer, StateId state, bool refresh = false)
    {
        if (layer._parent != sprite.Comp)
            throw new InvalidOperationException($"The given layer does not belong this entity.");

        if (layer.StateId == state && !refresh)
            return;

        layer.StateId = state;

        if (!layer.StateId.IsValid)
        {
            layer._actualState = null;
        }
        else if (layer.ActualRsi is not {} rsi)
        {
            Log.Error($"{ToPrettyString(sprite)} has no RSI to pull new state from! Trace:\n{Environment.StackTrace}");
            layer._actualState = GetFallbackState();
        }
        else if (!rsi.TryGetState(layer.StateId, out layer._actualState))
        {
            layer._actualState = GetFallbackState();
            Log.Error($"{ToPrettyString(sprite)}'s state '{state}' does not exist in RSI {rsi.Path}. Trace:\n{Environment.StackTrace}");
        }

        layer.AnimationFrame = 0;
        layer.AnimationTime = 0;
        layer.AnimationTimeLeft = layer._actualState?.GetDelay(0) ?? 0f;

        RebuildBounds(sprite);
        QueueUpdateIsInert(sprite);
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

    #region LayerSetRsi

    public void LayerSetRsi(Entity<SpriteComponent?> sprite, int index, RSI? rsi, StateId? state = null)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (!TryGetLayer(sprite, index, out var layer, true))
            return;

        layer._rsi = rsi;
        LayerSetRsiState(sprite!, layer, state ?? layer.StateId, refresh: true);
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

    #region Properties

    public void LayerSetScale(Entity<SpriteComponent?> sprite, int index, Vector2 value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (!TryGetLayer(sprite, index, out var layer, true))
            return;

        if (layer._scale.EqualsApprox(value))
            return;

        if (!ValidateScale(sprite.Owner, value))
            return;

        layer._scale = value;
        layer.UpdateLocalMatrix();
        RebuildBounds(sprite!);
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

    public void LayerSetRotation(Entity<SpriteComponent?> sprite, int index, Angle value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (!TryGetLayer(sprite, index, out var layer, true))
            return;

        if (layer._rotation.EqualsApprox(value))
            return;

        layer._rotation = value;
        layer.UpdateLocalMatrix();
        RebuildBounds(sprite!);
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

    public void LayerSetOffset(Entity<SpriteComponent?> sprite, int index, Vector2 value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (!TryGetLayer(sprite, index, out var layer, true))
            return;

        if (layer._offset.EqualsApprox(value))
            return;

        layer._offset = value;
        layer.UpdateLocalMatrix();
        RebuildBounds(sprite!);
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

    public void LayerSetVisible(Entity<SpriteComponent?> sprite, int index, bool value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (!TryGetLayer(sprite, index, out var layer, true))
            return;

        if (layer._visible == value)
            return;

        layer._visible = value;
        QueueUpdateIsInert(sprite!);
        RebuildBounds(sprite!);
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

    public void LayerSetColor(Entity<SpriteComponent?> sprite, int index, Color value)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (!TryGetLayer(sprite, index, out var layer, true))
            return;

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
}
