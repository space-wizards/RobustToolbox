using System;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
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

        layer.SetState(state);
        RebuildBounds(sprite!);
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

        layer.RSI = rsi;
        if (state != null)
            layer.StateId = state.Value;

        layer.AnimationFrame = 0;
        layer.AnimationTime = 0;

        var actualRsi = layer.RSI ?? sprite.Comp.BaseRSI;
        if (actualRsi == null)
        {
            Log.Error($"Entity {ToPrettyString(sprite)} has no RSI to pull new state from! Trace:\n{Environment.StackTrace}");
        }
        else
        {
            if (actualRsi.TryGetState(layer.StateId, out layer._actualState))
                layer.AnimationTimeLeft = layer._actualState.GetDelay(0);
            else
                Log.Error($"Entity {ToPrettyString(sprite)}'s state '{state}' does not exist in RSI {actualRsi.Path}. Trace:\n{Environment.StackTrace}");
        }

        RebuildBounds(sprite!);
        QueueUpdateIsInert(sprite!);
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
}
