using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;
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
        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetData(sprite, index, data);
    }

    public void LayerSetData(Entity<SpriteComponent?> sprite, Enum key, PrototypeLayerData data)
    {
        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetData(sprite, index, data);
    }

    #endregion

    #region LayerSetSprite

    public void LayerSetSprite(Entity<SpriteComponent?> sprite, int index, SpriteSpecifier specifier)
    {
        if (!_query.Resolve(sprite.Owner, ref sprite.Comp))
            return;

        if (!TryGetLayer(sprite, index, out var layer, true))
            return;

        switch (specifier)
        {
            case SpriteSpecifier.Texture tex:
                LayerSetTexture(sprite, layer, tex.TexturePath);
                break;
            case SpriteSpecifier.Rsi rsi:
                LayerSetState(sprite, layer, rsi.RsiState, rsi.RsiPath);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public void LayerSetSprite(Entity<SpriteComponent?> sprite, string key, SpriteSpecifier specifier)
    {
        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetSprite(sprite, index, specifier);
    }

    public void LayerSetSprite(Entity<SpriteComponent?> sprite, Enum key, SpriteSpecifier specifier)
    {
        if (LayerMapTryGet(sprite, key, out var index, true))
            LayerSetSprite(sprite, index, specifier);
    }

    #endregion

    #region LayerSetTexture

    #endregion
}
